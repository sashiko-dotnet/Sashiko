using Sashiko.Geography.Model;
using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Api
{
	public sealed class GeographyService
	{
		private static readonly Lazy<GeographyRegistry> Embedded = new(() => new GeographyRegistry());
		private readonly GeographyRegistry _registry;

		public GeographyService() : this(Embedded.Value) { }
		internal GeographyService(GeographyRegistry registry) => _registry = registry;

		public IReadOnlyList<WorldRegion> WorldRegions => _registry.WorldRegions;
		public IReadOnlyList<Country> Countries => _registry.Countries;
		public IReadOnlyList<Region> Regions => _registry.Regions;
		public IReadOnlyList<City> Cities => _registry.Cities;
		public IReadOnlyList<GeographySource> Sources => _registry.Sources;
		public WorldRegion GetWorldRegion(string id) => _registry.GetWorldRegion(id);
		public Country GetCountry(string id) => _registry.GetCountry(id);
		public Region GetRegion(string id) => _registry.GetRegion(id);
		public City GetCity(string id) => _registry.GetCity(id);
		public bool TryGetWorldRegion(string id, out WorldRegion? value) => _registry.TryGetWorldRegion(id, out value);
		public bool TryGetCountry(string id, out Country? value) => _registry.TryGetCountry(id, out value);
		public bool TryGetRegion(string id, out Region? value) => _registry.TryGetRegion(id, out value);
		public bool TryGetCity(string id, out City? value) => _registry.TryGetCity(id, out value);
		public IReadOnlyList<Country> GetCountriesByWorldRegion(string id) => _registry.GetCountries(id);
		public IReadOnlyList<Region> GetRegions(string countryId) => _registry.GetRegions(countryId);
		public IReadOnlyList<City> GetCities(string countryId) => _registry.GetCities(countryId);
		public IReadOnlyList<City> GetCitiesByRegion(string regionId) => _registry.GetCitiesByRegion(regionId);
		public IReadOnlyList<City> GetCapitalCities(string countryId) => _registry.GetCapitalCities(countryId);
		public IReadOnlyList<City> FindCities(string name, string? countryId = null) => _registry.FindCities(name, countryId);
	}
}
