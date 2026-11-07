using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Model;
using Sashiko.Geography.Registry;
using Sashiko.Languages.Api;
using Sashiko.Registries.Json;
using Sashiko.Validation;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Maintenance.Libraries.Geography
{
	internal static partial class GeographyDataValidator
	{
		private static readonly JsonSerializerOptions ReadOptions = new(JsonReadOptions.Strict)
		{
			ReadCommentHandling = JsonCommentHandling.Disallow,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
			RespectNullableAnnotations = true
		};

		internal static GeographyDataSet Read(IReadOnlyDictionary<string, string> files, bool polish = false)
		{
			foreach (var required in new[] { GeographyDataFiles.WorldRegions, GeographyDataFiles.Countries, GeographyDataFiles.Sources })
				Require(files.ContainsKey(required), required, "$", "Required collection is missing.");
			var documents = new Dictionary<string, object>(StringComparer.Ordinal);
			var locations = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
			foreach (var (path, json) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
			{
				documents.Add(path, GeographyDataFiles.Kind(path) switch
				{
					GeographyFileKind.WorldRegions => Read<WorldRegion>(json, path, polish, Normalize, item => item.Id, locations),
					GeographyFileKind.Countries => Read<Country>(json, path, polish, Normalize, item => item.Id, locations),
					GeographyFileKind.Regions => Read<Region>(json, path, polish, Normalize, item => item.Id, locations),
					GeographyFileKind.Cities => Read<City>(json, path, polish, Normalize, item => item.Id, locations),
					GeographyFileKind.Sources => Read<GeographySource>(json, path, polish, Normalize, item => item.Id, locations),
					_ => throw new InvalidDataException($"{path}: Unknown collection kind.")
				});
			}
			var data = new GeographyDataSet(documents, locations);
			Validate(data);
			return data;
		}

		private static IReadOnlyList<T> Read<T>(string json, string path, bool polish,
			Func<T, string, bool, T> normalize, Func<T, string> id, Dictionary<object, string> locations) where T : class
		{
			IReadOnlyList<T> values;
			try
			{
				values = new JsonRegistryListLoader<T>(new JsonSchemaValidator(), externalOptions: ReadOptions)
					.LoadExternal(json, path);
			}
			catch (ValidationException error)
			{
				throw new InvalidDataException($"{path}: {error.Message}", error);
			}
			using var document = JsonDocument.Parse(json);
			RejectDuplicateProperties(document.RootElement, path, "$");
			var normalized = values.Select((item, index) =>
			{
				var source = $"{path}[{index}]";
				var record = normalize(item, source, polish);
				locations.Add(record, source);
				return record;
			}).ToArray();
			var sorted = normalized.OrderBy(id, StringComparer.Ordinal).ToArray();
			Require(polish || normalized.Select(id).SequenceEqual(sorted.Select(id)), path, "$", "Records must be sorted by Id.");
			return Array.AsReadOnly(sorted);
		}

		private static WorldRegion Normalize(WorldRegion item, string path, bool polish)
			=> new(Text(item.Id, path, "Id", polish, upper: true), Text(item.Name, path, "Name", polish),
				Values(item.SourceIds, path, "SourceIds", polish, upper: true));
		private static Region Normalize(Region item, string path, bool polish)
			=> new(Text(item.Id, path, "Id", polish, upper: true), Text(item.Name, path, "Name", polish),
				Text(item.CountryId, path, "CountryId", polish, upper: true), Values(item.SourceIds, path, "SourceIds", polish, upper: true));
		private static City Normalize(City item, string path, bool polish)
			=> new(Text(item.Id, path, "Id", polish, upper: true), Text(item.Name, path, "Name", polish),
				Text(item.CountryId, path, "CountryId", polish, upper: true), Values(item.SourceIds, path, "SourceIds", polish, upper: true),
				Optional(item.RegionId, path, "RegionId", polish, upper: true), item.Coordinate);
		private static Country Normalize(Country item, string path, bool polish)
			=> new(Text(item.Id, path, "Id", polish, upper: true), Text(item.Name, path, "Name", polish),
				Text(item.WorldRegionId, path, "WorldRegionId", polish, upper: true),
				Values(item.CapitalCityIds, path, "CapitalCityIds", polish, upper: true),
				Values(item.SourceIds, path, "SourceIds", polish, upper: true),
				Optional(item.IsoAlpha2, path, "IsoAlpha2", polish, upper: true),
				Values(item.OfficialLanguageCodes, path, "OfficialLanguageCodes", polish, lower: true));
		private static GeographySource Normalize(GeographySource item, string path, bool polish)
			=> new(Text(item.Id, path, "Id", polish, upper: true), Text(item.Title, path, "Title", polish),
				Text(item.Url, path, "Url", polish), Text(item.Attribution, path, "Attribution", polish),
				Optional(item.LicenseUrl, path, "LicenseUrl", polish));

		private static void Validate(GeographyDataSet data)
		{
			var worldRegions = Unique(data, data.WorldRegions, item => item.Id);
			var countries = Unique(data, data.Countries, item => item.Id);
			var regions = Unique(data, data.Regions, item => item.Id);
			var cities = Unique(data, data.Cities, item => item.Id);
			var sources = Unique(data, data.Sources, item => item.Id);
			Require(countries.Count > 0, GeographyDataFiles.Countries, "$", "At least one country is required.");
			foreach (var item in data.WorldRegions) Entity(data, item, item.Id, item.SourceIds, sources);
			ValidateCountries(data, worldRegions, cities, sources);
			foreach (var region in data.Regions)
			{
				Entity(data, region, region.Id, region.SourceIds, sources);
				Require(countries.ContainsKey(region.CountryId), data.SourceOf(region), "CountryId", "Unknown country.");
			}
			ValidateCities(data, countries, regions, sources);
			foreach (var source in data.Sources)
			{
				Require(SourceId().IsMatch(source.Id), data.SourceOf(source), "Id", "Expected an uppercase source identifier.");
				Require(WebUrl(source.Url) && (source.LicenseUrl is null || WebUrl(source.LicenseUrl)),
					data.SourceOf(source), "Url", "Expected absolute HTTP(S) source and reuse URLs.");
			}
			ValidatePartitions(data, countries);
		}

		private static void ValidateCountries(GeographyDataSet data, IReadOnlyDictionary<string, WorldRegion> worldRegions,
			IReadOnlyDictionary<string, City> cities, IReadOnlyDictionary<string, GeographySource> sources)
		{
			var languages = new LanguageService();
			var isoCodes = new HashSet<string>(StringComparer.Ordinal);
			foreach (var country in data.Countries)
			{
				var path = data.SourceOf(country);
				Entity(data, country, country.Id, country.SourceIds, sources);
				Require(worldRegions.ContainsKey(country.WorldRegionId), path, "WorldRegionId", "Unknown world region.");
				if (country.IsoAlpha2 is not null)
					Require(Alpha2().IsMatch(country.IsoAlpha2) && isoCodes.Add(country.IsoAlpha2), path, "IsoAlpha2", "Expected a unique two-letter uppercase code.");
				foreach (var capital in country.CapitalCityIds)
					Require(cities.TryGetValue(capital, out var city) && city.CountryId == country.Id, path, "CapitalCityIds", $"Unknown or foreign city '{capital}'.");
				foreach (var code in country.OfficialLanguageCodes)
				{
					Require(languages.TryGet(code, out var language) && language!.Iso639_3 == code,
						path, "OfficialLanguageCodes", $"Unknown or noncanonical language '{code}'.");
				}
			}
		}

		private static void ValidateCities(GeographyDataSet data, IReadOnlyDictionary<string, Country> countries,
			IReadOnlyDictionary<string, Region> regions, IReadOnlyDictionary<string, GeographySource> sources)
		{
			foreach (var city in data.Cities)
			{
				var path = data.SourceOf(city);
				Entity(data, city, city.Id, city.SourceIds, sources);
				Require(countries.ContainsKey(city.CountryId), path, "CountryId", "Unknown country.");
				if (city.RegionId is not null)
					Require(regions.TryGetValue(city.RegionId, out var region) && region.CountryId == city.CountryId,
						path, "RegionId", "Unknown or foreign region.");
				if (city.Coordinate is not null)
					Require(city.Coordinate.Latitude is >= -90 and <= 90 && city.Coordinate.Longitude is >= -180 and <= 180,
						path, "Coordinate", "Latitude or longitude is outside its valid range.");
			}
		}

		private static Dictionary<string, T> Unique<T>(GeographyDataSet data, IEnumerable<T> items, Func<T, string> id) where T : class
		{
			var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in items)
				Require(result.TryAdd(id(item), item), data.SourceOf(item), "Id", $"Duplicate identifier '{id(item)}'.");
			return result;
		}

		private static void Entity(GeographyDataSet data, object item, string id, IReadOnlyList<string> ids,
			IReadOnlyDictionary<string, GeographySource> sources)
		{
			var path = data.SourceOf(item);
			Require(EntityId().IsMatch(id), path, "Id", "Expected an uppercase stable identifier.");
			Require(ids.Count > 0, path, "SourceIds", "At least one documented source is required.");
			foreach (var source in ids) Require(sources.ContainsKey(source), path, "SourceIds", $"Unknown source '{source}'.");
		}

		private static void ValidatePartitions(GeographyDataSet data, IReadOnlyDictionary<string, Country> countries)
		{
			foreach (var (path, document) in data.Documents.Where(pair => pair.Key.StartsWith("Countries/", StringComparison.Ordinal)))
			{
				var parts = path.Split('/');
				Require(countries.ContainsKey(parts[1]), path, "CountryId", "Unknown country partition.");
				if (document is IReadOnlyList<Region> regions)
					foreach (var region in regions) Require(region.CountryId == parts[1], data.SourceOf(region), "CountryId", "Record belongs to another country partition.");
				if (document is not IReadOnlyList<City> cities) continue;
				var stem = parts[3][..^5];
				var candidates = data.Regions.Where(region => region.CountryId == parts[1]).Select(region => region.Id).Append("country-direct");
				var partition = candidates.FirstOrDefault(id => MatchesPart(stem, id));
				Require(partition is not null, path, "$", "Expected a region collection or country-direct collection, optionally suffixed -part-001.");
				foreach (var city in cities)
					Require(city.CountryId == parts[1] && (city.RegionId ?? "country-direct") == partition,
						data.SourceOf(city), "RegionId", "Record belongs to another country or region partition.");
			}
		}

		private static bool MatchesPart(string stem, string id)
		{
			if (stem == id) return true;
			var prefix = id + "-part-";
			return stem.StartsWith(prefix, StringComparison.Ordinal)
				&& stem.Length >= prefix.Length + 3 && stem[prefix.Length..].All(char.IsAsciiDigit);
		}

		private static string? Optional(string? value, string path, string property, bool polish, bool upper = false)
			=> value is null ? null : Text(value, path, property, polish, upper);
		private static string Text(string? value, string path, string property, bool polish, bool upper = false, bool lower = false)
		{
			Require(!string.IsNullOrWhiteSpace(value), path, property, "Expected nonblank text.");
			var normalized = value!.Trim().Normalize();
			if (upper) normalized = normalized.ToUpperInvariant();
			if (lower) normalized = normalized.ToLowerInvariant();
			Require(polish || value == normalized, path, property, "Expected canonical text; run geography polish.");
			return normalized;
		}
		private static IReadOnlyList<string> Values(IReadOnlyList<string> values, string path, string property, bool polish, bool upper = false, bool lower = false)
		{
			var items = values.Select((value, index) => Text(value, path, $"{property}[{index}]", polish, upper, lower)).ToArray();
			Require(items.Distinct(StringComparer.Ordinal).Count() == items.Length, path, property, "Duplicate values must be resolved explicitly.");
			var sorted = items.Order(StringComparer.Ordinal).ToArray();
			Require(polish || items.SequenceEqual(sorted), path, property, "Values must be sorted.");
			return Array.AsReadOnly(sorted);
		}
		private static bool WebUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
		private static void Require(bool condition, string path, string property, string message)
		{
			if (!condition) throw new InvalidDataException($"{path}: {property}: {message}");
		}
		private static void RejectDuplicateProperties(JsonElement element, string path, string property)
		{
			if (element.ValueKind == JsonValueKind.Object)
			{
				var names = new HashSet<string>(StringComparer.Ordinal);
				foreach (var child in element.EnumerateObject())
				{
					Require(names.Add(child.Name), path, property + "." + child.Name, "Duplicate JSON property.");
					RejectDuplicateProperties(child.Value, path, property + "." + child.Name);
				}
			}
			else if (element.ValueKind == JsonValueKind.Array)
			{
				var index = 0;
				foreach (var child in element.EnumerateArray()) RejectDuplicateProperties(child, path, $"{property}[{index++}]");
			}
		}
		[GeneratedRegex("^[A-Z0-9]+(?:-[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
		private static partial Regex EntityId();
		[GeneratedRegex("^[A-Z0-9][A-Z0-9._-]*$", RegexOptions.CultureInvariant)]
		private static partial Regex SourceId();
		[GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant)]
		private static partial Regex Alpha2();
	}
}
