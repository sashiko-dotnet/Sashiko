using System.Text.RegularExpressions;
using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Enums;
using Sashiko.Languages.Api;

namespace Sashiko.Geography.Registry
{
	internal static partial class GeographyDataValidator
	{
		internal static IReadOnlyDictionary<string, CountryCoverage> Validate(GeographyRegistry registry)
		{
			UniqueSorted(registry.WorldRegions.Select(region => region.M49Code), GeographyRegistry.RegionsPath, "M49Code");
			foreach (var region in registry.WorldRegions)
			{
				Require(NumericCode().IsMatch(region.M49Code), GeographyRegistry.RegionsPath, "M49Code", "Expected three digits.");
				Text(region.Name, GeographyRegistry.RegionsPath, "Name");
			}
			UniqueSorted(registry.DataSources.Select(source => source.Id), GeographyRegistry.SourcesPath, "Id");
			foreach (var source in registry.DataSources)
			{
				Text(source.Id, GeographyRegistry.SourcesPath, "Id");
				Require(source.Id == source.Id.ToUpperInvariant(), GeographyRegistry.SourcesPath, "Id", "Source IDs must be uppercase.");
				Text(source.Title, GeographyRegistry.SourcesPath, "Title");
				Text(source.Attribution, GeographyRegistry.SourcesPath, "Attribution");
				Require(WebUrl(source.Url) && (source.LicenseUrl is null || WebUrl(source.LicenseUrl)), GeographyRegistry.SourcesPath, "Url", "Expected absolute HTTP(S) source and license URLs.");
			}
			var sources = registry.DataSources.Select(source => source.Id).ToHashSet(StringComparer.Ordinal);
			Require(registry.Countries.Count > 0, "Countries", "$", "At least one country is required.");
			Unique(registry.Countries.Select(country => country.IsoAlpha3), "Countries", "IsoAlpha3");
			Unique(registry.Countries.Select(country => country.IsoNumeric), "Countries", "IsoNumeric");
			Unique(registry.Countries.Select(country => country.Name), "Countries", "Name");
			Unique(registry.Subdivisions.Where(item => item.Iso3166_2 is not null).Select(item => item.Iso3166_2!), "Subdivisions", "Iso3166_2");
			Unique(registry.Cities.Where(city => city.UnLocode is not null).Select(city => city.UnLocode!), "Cities", "UnLocode");
			var countries = registry.Countries.ToDictionary(country => country.IsoAlpha2);
			var subdivisions = registry.Subdivisions.ToDictionary(item => item.Id);
			var cities = registry.Cities.ToDictionary(city => city.Id);
			var languages = new LanguageService();

			foreach (var country in registry.Countries)
			{
				var path = registry.Paths[country.IsoAlpha2];
				Require(Alpha2().IsMatch(country.IsoAlpha2) && Alpha3().IsMatch(country.IsoAlpha3)
					&& NumericCode().IsMatch(country.IsoNumeric), path, "IsoAlpha2", "Invalid ISO code format.");
				if (Enum.TryParse<CountryId>(country.IsoAlpha2, true, out var declared) && Enum.IsDefined(declared))
					Require(((int)declared).ToString("D3", System.Globalization.CultureInfo.InvariantCulture) == country.IsoNumeric,
						path, "IsoNumeric", "Value conflicts with the locally declared CountryId.");
				Require(registry.WorldRegions.Any(region => region.M49Code == country.M49RegionCode), path, "M49RegionCode", "Unknown world region.");
				References(country.SourceIds, sources, path);
				Population(country.Population, sources, path);
				foreach (var language in country.OfficialLanguageCodes)
				{
					try
					{
						Require(languages.GetIso3(language).Iso639_3 == language, path, "OfficialLanguageCodes", $"Noncanonical language '{language}'.");
					}
					catch (KeyNotFoundException)
					{
						throw new InvalidDataException($"{path}: OfficialLanguageCodes: Unknown language '{language}'.");
					}
				}
				foreach (var capital in country.CapitalCityIds)
					Require(cities.TryGetValue(capital, out var city) && city.CountryCode == country.IsoAlpha2,
						path, "CapitalCityIds", $"Unknown or foreign city '{capital}'.");
			}
			foreach (var subdivision in registry.Subdivisions)
			{
				var path = registry.Paths[subdivision.Id];
				Entity(subdivision.Id, subdivision.CountryCode, countries, path);
				if (subdivision.Iso3166_2 is not null)
					Require(SubdivisionCode().IsMatch(subdivision.Iso3166_2) && subdivision.Iso3166_2.StartsWith(subdivision.CountryCode + "-", StringComparison.Ordinal),
						path, "Iso3166_2", "Invalid country-qualified ISO subdivision format.");
				if (subdivision.ParentId is not null)
					Require(subdivisions.TryGetValue(subdivision.ParentId, out var parent) && parent.CountryCode == subdivision.CountryCode,
						path, "ParentId", "Unknown or foreign subdivision parent.");
				References(subdivision.SourceIds, sources, path);
				Population(subdivision.Population, sources, path);
			}
			ValidateHierarchy(subdivisions, registry.Paths);
			foreach (var city in registry.Cities)
			{
				var path = registry.Paths[city.Id];
				Entity(city.Id, city.CountryCode, countries, path);
				if (city.SubdivisionId is not null)
					Require(subdivisions.TryGetValue(city.SubdivisionId, out var parent) && parent.CountryCode == city.CountryCode,
						path, "SubdivisionId", "Unknown or foreign subdivision parent.");
				if (city.UnLocode is not null)
					Require(UnLocode().IsMatch(city.UnLocode) && city.UnLocode.StartsWith(city.CountryCode, StringComparison.Ordinal),
						path, "UnLocode", "Invalid country-qualified UN/LOCODE format.");
				if (city.Coordinate is not null)
					Require(city.Coordinate.Latitude is >= -90 and <= 90 && city.Coordinate.Longitude is >= -180 and <= 180,
						path, "Location", "Coordinates are outside latitude/longitude limits.");
				References(city.SourceIds, sources, path);
				Population(city.Population, sources, path);
			}
			var populations = new GeographyPopulationAnalysis(registry);
			populations.ValidateTotals();
			ValidateCoverage(registry, populations);
			return new System.Collections.ObjectModel.ReadOnlyDictionary<string, CountryCoverage>(
				registry.Countries.ToDictionary(country => country.IsoAlpha2, populations.Coverage));
		}

		private static void ValidateHierarchy(IReadOnlyDictionary<string, Subdivision> subdivisions, IReadOnlyDictionary<string, string> paths)
		{
			var done = new HashSet<string>(StringComparer.Ordinal);
			foreach (var subdivision in subdivisions.Values)
			{
				var chain = new HashSet<string>(StringComparer.Ordinal);
				string? current = subdivision.Id;
				while (current is not null && !done.Contains(current))
				{
					Require(chain.Add(current), paths[subdivision.Id], "ParentId", "Subdivision parent cycle.");
					current = subdivisions[current].ParentId;
				}
				done.UnionWith(chain);
			}
		}

		private static void ValidateCoverage(GeographyRegistry registry, GeographyPopulationAnalysis populations)
		{
			const string path = GeographyRegistry.CoveragePath;
			var baseline = registry.CoverageBaseline;
			UniqueSorted(baseline.Countries.Select(floor => floor.CountryCode), path, "Countries");
			UniqueSorted(baseline.WorldRegions.Select(floor => floor.M49Code), path, "WorldRegions");
			Require(registry.Countries.Select(country => country.IsoAlpha2).SequenceEqual(baseline.Countries.Select(floor => floor.CountryCode))
				&& registry.WorldRegions.Select(region => region.M49Code).SequenceEqual(baseline.WorldRegions.Select(floor => floor.M49Code)),
				path, "$", "Coverage entries must match the embedded countries and world regions.");
			foreach (var floor in baseline.WorldRegions)
				Require(floor.MinimumSupportedCountryCount >= 0 && registry.Countries.Count(country => country.M49RegionCode == floor.M49Code) >= floor.MinimumSupportedCountryCount,
					path, floor.M49Code, "World region coverage fell below its floor.");
			foreach (var floor in baseline.Countries)
			{
				var country = registry.Countries.Single(country => country.IsoAlpha2 == floor.CountryCode);
				Require(floor.MinimumSubdivisionCount >= 0 && floor.MinimumCityCount >= 0
					&& floor.MinimumSubdivisionPopulationCoverage is >= 0 and <= 1 && floor.MinimumCityPopulationCoverage is >= 0 and <= 1,
					path, floor.CountryCode, "Invalid coverage floor.");
				var coverage = populations.Coverage(country);
				Require(coverage.SubdivisionCount >= floor.MinimumSubdivisionCount && coverage.CityCount >= floor.MinimumCityCount
					&& (floor.MinimumSubdivisionPopulationCoverage == 0 || coverage.SubdivisionPopulationCoverage >= floor.MinimumSubdivisionPopulationCoverage)
					&& (floor.MinimumCityPopulationCoverage == 0 || coverage.CityPopulationCoverage >= floor.MinimumCityPopulationCoverage),
					path, floor.CountryCode, "Coverage fell below its committed floor.");
			}
		}

		private static void Entity(string id, string countryCode, IReadOnlyDictionary<string, Country> countries, string path)
			=> Require(countries.ContainsKey(countryCode) && EntityId().IsMatch(id) && id.StartsWith(countryCode + "-", StringComparison.Ordinal),
				path, "Id", "Expected a stable, country-qualified ID and an existing country.");

		private static void References(IReadOnlyList<string> ids, ISet<string> sources, string path)
		{
			Require(ids.Count > 0, path, "SourceIds", "At least one documented source is required.");
			foreach (var id in ids) Require(sources.Contains(id), path, "SourceIds", $"Unknown source '{id}'.");
		}

		private static void Population(PopulationSnapshot? population, ISet<string> sources, string path)
		{
			if (population is null) return;
			Require(population.Value >= 0, path, "Population.Value", "Population cannot be negative.");
			Require(sources.Contains(population.SourceId), path, "Population.SourceId", $"Unknown source '{population.SourceId}'.");
		}

		private static bool WebUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
		private static void Text(string value, string path, string property)
			=> Require(!string.IsNullOrWhiteSpace(value) && value == value.Trim().Normalize(), path, property, "Expected normalized, nonblank text.");

		private static void UniqueSorted(IEnumerable<string> values, string path, string property)
		{
			var array = values.ToArray();
			Unique(array, path, property);
			Require(array.SequenceEqual(array.Order(StringComparer.Ordinal)), path, property, "Values must be sorted.");
		}

		private static void Unique(IEnumerable<string> values, string path, string property)
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var value in values) Require(seen.Add(value), path, property, $"Duplicate value '{value}'.");
		}

		internal static void Require(bool condition, string path, string property, string message)
		{
			if (!condition) throw new InvalidDataException($"{path}: {property}: {message}");
		}

		[GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant)]
		private static partial Regex Alpha2();
		[GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
		private static partial Regex Alpha3();
		[GeneratedRegex("^[0-9]{3}$", RegexOptions.CultureInvariant)]
		private static partial Regex NumericCode();
		[GeneratedRegex("^[A-Z]{2}-[A-Z0-9]{1,3}$", RegexOptions.CultureInvariant)]
		private static partial Regex SubdivisionCode();
		[GeneratedRegex("^[A-Z]{2}-[A-Z0-9]+(?:-[A-Z0-9]+)*$", RegexOptions.CultureInvariant)]
		private static partial Regex EntityId();
		[GeneratedRegex("^[A-Z]{2}[A-Z0-9]{3}$", RegexOptions.CultureInvariant)]
		private static partial Regex UnLocode();
	}
}
