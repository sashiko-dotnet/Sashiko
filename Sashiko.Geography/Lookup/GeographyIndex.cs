using Sashiko.Geography.Model;
using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Lookup
{
	internal sealed class GeographyIndex
	{
		private readonly IReadOnlyDictionary<string, Country> _countriesByAlpha2;
		private readonly IReadOnlyDictionary<string, Country> _countriesByAlpha3;
		private readonly IReadOnlyDictionary<string, Country> _countriesByNumeric;
		private readonly IReadOnlyDictionary<string, Country> _countriesByName;
		private readonly IReadOnlyDictionary<string, Subdivision> _subdivisionsByCode;
		private readonly IReadOnlyDictionary<string, City> _citiesById;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<City>> _citiesByName;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<Country>> _countriesByRegion;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<Subdivision>> _subdivisionsByCountry;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<Subdivision>> _rootSubdivisionsByCountry;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<Subdivision>> _subdivisionsByParent;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<City>> _citiesByCountry;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<City>> _citiesBySubdivision;

		internal IReadOnlyList<GeographySource> DataSources { get; }
		internal IReadOnlyDictionary<string, CountryCoverage> Coverage { get; }
		internal IReadOnlyList<WorldRegion> WorldRegions { get; }
		internal IReadOnlyList<Country> Countries { get; }
		internal IReadOnlyList<Subdivision> Subdivisions { get; }
		internal IReadOnlyList<City> Cities { get; }

		internal GeographyIndex(IGeographyRegistry registry)
		{
			DataSources = registry.DataSources.ToList().AsReadOnly();
				Coverage = registry.Coverage;
				WorldRegions = registry.WorldRegions.ToList().AsReadOnly();
			Countries = registry.Countries.ToList().AsReadOnly();
			Subdivisions = registry.Subdivisions.ToList().AsReadOnly();
			Cities = registry.Cities.ToList().AsReadOnly();

			_countriesByAlpha2 = Countries.ToDictionary(
				country => country.IsoAlpha2,
				StringComparer.OrdinalIgnoreCase);
			_countriesByAlpha3 = Countries.ToDictionary(
				country => country.IsoAlpha3,
				StringComparer.OrdinalIgnoreCase);
			_countriesByNumeric = Countries.ToDictionary(
				country => country.IsoNumeric,
				StringComparer.OrdinalIgnoreCase);
			_countriesByName = Countries.ToDictionary(
				country => country.Name,
				StringComparer.OrdinalIgnoreCase);
			_subdivisionsByCode = Subdivisions.ToDictionary(
				subdivision => subdivision.Id,
				StringComparer.OrdinalIgnoreCase);
			_citiesById = Cities.ToDictionary(
				city => city.Id,
				StringComparer.OrdinalIgnoreCase);
			_citiesByName = Cities
				.GroupBy(city => city.Name, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					group => group.Key,
					group => (IReadOnlyList<City>)group.ToList().AsReadOnly(),
					StringComparer.OrdinalIgnoreCase);
			_countriesByRegion = Group(Countries, country => country.M49RegionCode);
			_subdivisionsByCountry = Group(Subdivisions, subdivision => subdivision.CountryCode);
			_rootSubdivisionsByCountry = Group(Subdivisions.Where(item => item.ParentId is null), item => item.CountryCode);
			_subdivisionsByParent = Group(Subdivisions.Where(item => item.ParentId is not null), item => item.ParentId!);
			_citiesByCountry = Group(Cities, city => city.CountryCode);
			_citiesBySubdivision = Group(Cities.Where(city => city.SubdivisionId is not null), city => city.SubdivisionId!);
		}

		internal bool TryResolveCountry(string input, out Country? country)
			=> _countriesByAlpha2.TryGetValue(input?.Trim() ?? "", out country)
				|| _countriesByAlpha3.TryGetValue(input?.Trim() ?? "", out country)
				|| _countriesByNumeric.TryGetValue(input?.Trim() ?? "", out country)
				|| _countriesByName.TryGetValue(input?.Trim() ?? "", out country);

		internal bool TryGetSubdivision(string code, out Subdivision? subdivision)
			=> _subdivisionsByCode.TryGetValue(code?.Trim() ?? "", out subdivision);

		internal bool TryGetCity(string id, out City? city)
			=> _citiesById.TryGetValue(id?.Trim() ?? "", out city);

		internal IReadOnlyList<City> FindCitiesByName(string name)
			=> _citiesByName.TryGetValue(name?.Trim() ?? "", out var cities)
				? cities
				: Array.Empty<City>();

		internal IReadOnlyList<Country> GetCountriesByRegion(string code)
			=> _countriesByRegion.GetValueOrDefault(code) ?? Array.Empty<Country>();

		internal IReadOnlyList<Subdivision> GetSubdivisionsByCountry(string code)
			=> _subdivisionsByCountry.GetValueOrDefault(code) ?? Array.Empty<Subdivision>();

		internal IReadOnlyList<Subdivision> GetRootSubdivisions(string code)
			=> _rootSubdivisionsByCountry.GetValueOrDefault(code) ?? Array.Empty<Subdivision>();

		internal IReadOnlyList<Subdivision> GetChildSubdivisions(string code)
			=> _subdivisionsByParent.GetValueOrDefault(code) ?? Array.Empty<Subdivision>();

		internal IReadOnlyList<City> GetCitiesByCountry(string code)
			=> _citiesByCountry.GetValueOrDefault(code) ?? Array.Empty<City>();

		internal IReadOnlyList<City> GetCitiesBySubdivision(string code)
			=> _citiesBySubdivision.GetValueOrDefault(code) ?? Array.Empty<City>();

		private static IReadOnlyDictionary<string, IReadOnlyList<T>> Group<T>(
			IEnumerable<T> items, Func<T, string> key)
			=> items.GroupBy(key, StringComparer.OrdinalIgnoreCase).ToDictionary(
				group => group.Key,
				group => (IReadOnlyList<T>)group.ToList().AsReadOnly(),
				StringComparer.OrdinalIgnoreCase);
	}
}
