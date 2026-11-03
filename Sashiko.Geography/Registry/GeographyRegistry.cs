using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Coverage;
using Sashiko.Geography.Model.Data;
using Sashiko.Registries.Json;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Geography.Registry
{
	internal sealed class GeographyRegistry : IGeographyRegistry
	{
		internal const string ManifestPath = "manifest.json";
		internal const string RegionsPath = "WorldRegions/world-regions.json";
		internal const string CoveragePath = "Coverage/coverage-baseline.json";
		internal const string SourcesPath = "Sources/sources.json";
		private static readonly JsonSerializerOptions ReadOptions = new(JsonReadOptions.Strict)
		{
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
			RespectNullableAnnotations = true,
			ReadCommentHandling = JsonCommentHandling.Disallow
		};

		public IReadOnlyList<WorldRegion> WorldRegions { get; }
		public IReadOnlyList<GeographySource> DataSources { get; }
		public IReadOnlyList<Country> Countries { get; }
		public IReadOnlyList<Subdivision> Subdivisions { get; }
		public IReadOnlyList<City> Cities { get; }
		public CoverageBaseline CoverageBaseline { get; }
		public IReadOnlyDictionary<string, CountryCoverage> Coverage { get; }
		internal IReadOnlyDictionary<string, object> Documents { get; }
		internal IReadOnlyDictionary<string, string> Paths { get; }

		internal GeographyRegistry() : this(ReadEmbeddedFiles()) { }

		internal GeographyRegistry(IReadOnlyDictionary<string, string> files, bool polish = false)
		{
			if (Read<DatasetManifest>(files, ManifestPath).SchemaVersion != 1)
				throw new InvalidDataException($"{ManifestPath}: Unsupported SchemaVersion; expected 1.");
			WorldRegions = ReadList<WorldRegion>(files, RegionsPath);
			DataSources = ReadList<GeographySource>(files, SourcesPath);
			CoverageBaseline = Read<CoverageBaseline>(files, CoveragePath);
			var countries = new List<Country>();
			var subdivisions = new List<Subdivision>();
			var cityRecords = new List<(string Country, CityData Data)>();
			var documents = new Dictionary<string, object>(StringComparer.Ordinal);
			var paths = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (var path in files.Keys.Order(StringComparer.Ordinal))
			{
				if (path is ManifestPath or RegionsPath or CoveragePath or SourcesPath) continue;
				var parts = path.Split('/');
				if (parts.Any(part => part.Length == 0 || part is "." or "..") || parts[0] != "Countries")
					throw new InvalidDataException($"{path}: Unexpected geography path.");
				var normalizer = new GeographyDataNormalizer(path, polish);
				if (parts.Length == 3 && parts[2] == "country.json")
				{
					var data = normalizer.Normalize(Read<CountryData>(files, path));
					Add(path, parts[1], data.IsoAlpha2, data, documents, paths);
					countries.Add(new Country(data));
				}
				else if (parts.Length == 4 && parts[3].EndsWith(".json", StringComparison.Ordinal))
				{
					var fileId = parts[3][..^5];
					if (parts[2] == "Subdivisions")
					{
						var data = normalizer.Normalize(Read<SubdivisionData>(files, path));
						Add(path, fileId, data.Id, data, documents, paths);
						subdivisions.Add(new Subdivision(data, parts[1]));
					}
					else if (parts[2] == "Cities")
					{
						var data = normalizer.Normalize(Read<CityData>(files, path));
						Add(path, fileId, data.Id, data, documents, paths);
						cityRecords.Add((parts[1], data));
					}
					else throw new InvalidDataException($"{path}: Expected Subdivisions or Cities.");
				}
				else throw new InvalidDataException($"{path}: Expected country.json or a flat entity file.");
			}
			Countries = countries.OrderBy(country => country.IsoAlpha2, StringComparer.Ordinal).ToList().AsReadOnly();
			Subdivisions = subdivisions.OrderBy(subdivision => subdivision.Id, StringComparer.Ordinal).ToList().AsReadOnly();
			var capitals = countries.ToDictionary(country => country.IsoAlpha2, country => country.CapitalCityIds.ToHashSet(StringComparer.Ordinal));
			Cities = cityRecords.Select(record => new City(record.Data, record.Country,
				capitals.TryGetValue(record.Country, out var ids) && ids.Contains(record.Data.Id)))
				.OrderBy(city => city.Id, StringComparer.Ordinal).ToList().AsReadOnly();
			Documents = new ReadOnlyDictionary<string, object>(documents);
			Paths = new ReadOnlyDictionary<string, string>(paths);
			Coverage = GeographyDataValidator.Validate(this);
		}

		internal static IReadOnlyDictionary<string, string> ReadEmbeddedFiles()
		{
			var assembly = typeof(GeographyRegistry).Assembly;
			var files = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var resource in assembly.GetManifestResourceNames())
			{
				var path = resource.Replace('\\', '/');
				if (!path.StartsWith("Data/", StringComparison.Ordinal)) continue;
				using var stream = assembly.GetManifestResourceStream(resource)
					?? throw new InvalidOperationException($"Embedded resource '{resource}' not found.");
				using var reader = new StreamReader(stream);
				files.Add(path["Data/".Length..], reader.ReadToEnd());
			}
			return files;
		}

		private static T Read<T>(IReadOnlyDictionary<string, string> files, string path)
		{
			var json = RequiredFile(files, path);
			var value = new JsonRegistryObjectLoader<T>(new JsonSchemaValidator(), externalOptions: ReadOptions).LoadExternal(json, path);
			using var document = JsonDocument.Parse(json);
			RejectDuplicateProperties(document.RootElement, path, "$");
			return value;
		}

		private static IReadOnlyList<T> ReadList<T>(IReadOnlyDictionary<string, string> files, string path)
		{
			var json = RequiredFile(files, path);
			var values = new JsonRegistryListLoader<T>(new JsonSchemaValidator(), externalOptions: ReadOptions).LoadExternal(json, path);
			using var document = JsonDocument.Parse(json);
			RejectDuplicateProperties(document.RootElement, path, "$");
			return values.ToList().AsReadOnly();
		}

		private static void RejectDuplicateProperties(JsonElement element, string path, string propertyPath)
		{
			if (element.ValueKind == JsonValueKind.Object)
			{
				var names = new HashSet<string>(StringComparer.Ordinal);
				foreach (var property in element.EnumerateObject())
				{
					if (!names.Add(property.Name)) throw new InvalidDataException($"{path}: {propertyPath}.{property.Name}: Duplicate property.");
					RejectDuplicateProperties(property.Value, path, $"{propertyPath}.{property.Name}");
				}
			}
			else if (element.ValueKind == JsonValueKind.Array)
			{
				var index = 0;
				foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item, path, $"{propertyPath}[{index++}]");
			}
		}

		private static string RequiredFile(IReadOnlyDictionary<string, string> files, string path)
			=> files.TryGetValue(path, out var json) ? json : throw new InvalidDataException($"{path}: Required geography file is missing.");

		private static void Add(string path, string fileId, string id, object document,
			Dictionary<string, object> documents, Dictionary<string, string> paths)
		{
			if (fileId != id) throw new InvalidDataException($"{path}: Id '{id}' does not match its file or country directory.");
			if (!paths.TryAdd(id, path)) throw new InvalidDataException($"{path}: Duplicate identifier '{id}', already in '{paths[id]}'.");
			documents.Add(path, document);
		}
	}
}
