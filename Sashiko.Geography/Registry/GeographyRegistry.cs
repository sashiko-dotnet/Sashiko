using Sashiko.Core.Json.Options;
using Sashiko.Geography.Model;
using Sashiko.Registries.Json;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Geography.Registry
{
	internal sealed class GeographyRegistry
	{
		private readonly Dictionary<string, WorldRegion> _worldRegions;
		private readonly Dictionary<string, Country> _countries;
		private readonly Dictionary<string, Region> _regions;
		private readonly Dictionary<string, City> _cities;
		private readonly Dictionary<string, IReadOnlyList<Country>> _countriesByWorldRegion;
		private readonly Dictionary<string, IReadOnlyList<Region>> _regionsByCountry;
		private readonly Dictionary<string, IReadOnlyList<City>> _citiesByCountry;
		private readonly Dictionary<string, IReadOnlyList<City>> _citiesByRegion;
		private readonly Dictionary<string, IReadOnlyList<City>> _citiesByName;
		private readonly Dictionary<string, IReadOnlyList<City>> _capitals;

		internal IReadOnlyList<WorldRegion> WorldRegions { get; }
		internal IReadOnlyList<Country> Countries { get; }
		internal IReadOnlyList<Region> Regions { get; }
		internal IReadOnlyList<City> Cities { get; }
		internal IReadOnlyList<GeographySource> Sources { get; }

		internal GeographyRegistry() : this(GeographyDataFiles.ReadEmbedded()) { }

		// This path trusts records already checked by maintenance and tests.
		internal GeographyRegistry(IReadOnlyDictionary<string, string> files)
		{
			WorldRegions = Sorted(Load<WorldRegion>(files, GeographyDataFiles.WorldRegions), item => item.Id);
			Countries = Sorted(Load<Country>(files, GeographyDataFiles.Countries), item => item.Id);
			Sources = Sorted(Load<GeographySource>(files, GeographyDataFiles.Sources), item => item.Id);
			var parts = files.Keys.Order(StringComparer.Ordinal).Select(path => (Path: path, Kind: GeographyDataFiles.Kind(path))).ToArray();
			Regions = Sorted(parts.Where(part => part.Kind == GeographyFileKind.Regions)
				.SelectMany(part => Load<Region>(files, part.Path)), item => item.Id);
			Cities = Sorted(parts.Where(part => part.Kind == GeographyFileKind.Cities)
				.SelectMany(part => Load<City>(files, part.Path)), item => item.Id);
			_worldRegions = ById(WorldRegions, item => item.Id);
			_countries = ById(Countries, item => item.Id);
			_regions = ById(Regions, item => item.Id);
			_cities = ById(Cities, item => item.Id);
			_countriesByWorldRegion = Group(Countries, item => item.WorldRegionId);
			_regionsByCountry = Group(Regions, item => item.CountryId);
			_citiesByCountry = Group(Cities, item => item.CountryId);
			_citiesByRegion = Group(Cities.Where(item => item.RegionId is not null), item => item.RegionId!);
			_citiesByName = Group(Cities, item => item.Name);
			_capitals = Countries.ToDictionary(country => country.Id,
				country => (IReadOnlyList<City>)Array.AsReadOnly(country.CapitalCityIds.Select(id => _cities[id]).ToArray()),
				StringComparer.OrdinalIgnoreCase);
		}

		internal bool TryGetWorldRegion(string id, out WorldRegion? value) => _worldRegions.TryGetValue(Key(id), out value);
		internal bool TryGetCountry(string id, out Country? value) => _countries.TryGetValue(Key(id), out value);
		internal bool TryGetRegion(string id, out Region? value) => _regions.TryGetValue(Key(id), out value);
		internal bool TryGetCity(string id, out City? value) => _cities.TryGetValue(Key(id), out value);
		internal WorldRegion GetWorldRegion(string id) => Get(_worldRegions, id, "World region");
		internal Country GetCountry(string id) => Get(_countries, id, "Country");
		internal Region GetRegion(string id) => Get(_regions, id, "Region");
		internal City GetCity(string id) => Get(_cities, id, "City");
		internal IReadOnlyList<Country> GetCountries(string worldRegionId) => Values(_countriesByWorldRegion, GetWorldRegion(worldRegionId).Id);
		internal IReadOnlyList<Region> GetRegions(string countryId) => Values(_regionsByCountry, GetCountry(countryId).Id);
		internal IReadOnlyList<City> GetCities(string countryId) => Values(_citiesByCountry, GetCountry(countryId).Id);
		internal IReadOnlyList<City> GetCitiesByRegion(string regionId) => Values(_citiesByRegion, GetRegion(regionId).Id);
		internal IReadOnlyList<City> GetCapitalCities(string countryId) => _capitals[GetCountry(countryId).Id];

		internal IReadOnlyList<City> FindCities(string name, string? countryId)
		{
			var country = countryId is null ? null : GetCountry(countryId);
			var matches = Values(_citiesByName, Key(name));
			return country is null ? matches
				: Array.AsReadOnly(matches.Where(city => city.CountryId == country.Id).ToArray());
		}

		private static string Key(string? value) => value?.Trim() ?? "";
		private static T Get<T>(Dictionary<string, T> values, string id, string kind)
			=> values.TryGetValue(Key(id), out var value) ? value : throw new KeyNotFoundException($"{kind} '{id}' was not found.");
		private static IReadOnlyList<T> Values<T>(Dictionary<string, IReadOnlyList<T>> values, string id)
			=> values.GetValueOrDefault(id) ?? Array.Empty<T>();
		private static IReadOnlyList<T> Load<T>(IReadOnlyDictionary<string, string> files, string path)
			=> new JsonRegistryListLoader<T>(new JsonSchemaValidator(), embeddedOptions: JsonReadOptions.Strict)
				.LoadEmbedded(files[path], path);
		private static IReadOnlyList<T> Sorted<T>(IEnumerable<T> values, Func<T, string> id)
			=> Array.AsReadOnly(values.OrderBy(id, StringComparer.Ordinal).ToArray());
		private static Dictionary<string, T> ById<T>(IEnumerable<T> values, Func<T, string> id)
			=> values.ToDictionary(id, StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, IReadOnlyList<T>> Group<T>(IEnumerable<T> values, Func<T, string> key)
			=> values.GroupBy(key, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key,
				group => (IReadOnlyList<T>)Array.AsReadOnly(group.ToArray()), StringComparer.OrdinalIgnoreCase);
	}
}
