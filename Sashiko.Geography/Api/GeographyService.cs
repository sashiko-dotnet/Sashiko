using Sashiko.Geography.Lookup;
using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Enums;
using Sashiko.Geography.Registry;
using Sashiko.Languages.Api;
using Sashiko.Languages.Model;

namespace Sashiko.Geography.Api
{
	public sealed class GeographyService
	{
		private static readonly Lazy<GeographyIndex> EmbeddedIndex = new(() => new GeographyIndex(new GeographyRegistry()));
		private static readonly Lazy<LanguageService> SharedLanguages = new(() => new LanguageService());
		private readonly GeographyIndex _index;
		private readonly LanguageService _languages;

		public GeographyService()
		{
			_index = EmbeddedIndex.Value;
			_languages = SharedLanguages.Value;
		}

		internal GeographyService(IGeographyRegistry registry, LanguageService languages)
		{
			_index = new GeographyIndex(registry);
			_languages = languages;
		}

		public IReadOnlyList<GeographySource> Sources => _index.DataSources;
		public IReadOnlyList<WorldRegion> WorldRegions => _index.WorldRegions;
		public IReadOnlyList<Country> Countries => _index.Countries;
		public IReadOnlyList<Subdivision> Subdivisions => _index.Subdivisions;
		public IReadOnlyList<City> Cities => _index.Cities;

		public Country GetCountry(string input)
			=> TryGetCountry(input, out var country)
				? country!
				: throw new KeyNotFoundException($"Country '{input}' was not found.");

		public bool TryGetCountry(string input, out Country? country)
			=> _index.TryResolveCountry(input, out country);

		public Country GetCountry(CountryId country)
			=> TryGetCountry(country, out var result) ? result!
				: throw new KeyNotFoundException($"Country '{country}' was not found.");

		public bool TryGetCountry(CountryId country, out Country? result)
		{
			result = null;
			return Enum.IsDefined(country) && TryGetCountry(country.ToString(), out result);
		}

		public Subdivision GetSubdivision(string code)
			=> TryGetSubdivision(code, out var subdivision)
				? subdivision!
				: throw new KeyNotFoundException($"Subdivision '{code}' was not found.");

		public bool TryGetSubdivision(string code, out Subdivision? subdivision)
			=> _index.TryGetSubdivision(code, out subdivision);

		public IReadOnlyList<Subdivision> GetSubdivisions(string countryInput)
			=> _index.GetSubdivisionsByCountry(GetCountry(countryInput).IsoAlpha2);

		public IReadOnlyList<Subdivision> GetSubdivisions(CountryId country)
			=> GetSubdivisions(GetCountry(country).IsoAlpha2);

		/// <summary>Returns the direct administrative children of a country.</summary>
		public IReadOnlyList<Subdivision> GetRootSubdivisions(string countryInput)
			=> _index.GetRootSubdivisions(GetCountry(countryInput).IsoAlpha2);

		public IReadOnlyList<Subdivision> GetRootSubdivisions(CountryId country)
			=> GetRootSubdivisions(GetCountry(country).IsoAlpha2);

		/// <summary>Returns only the direct children of the specified subdivision.</summary>
		public IReadOnlyList<Subdivision> GetChildSubdivisions(string subdivisionId)
			=> _index.GetChildSubdivisions(GetSubdivision(subdivisionId).Id);

		public IReadOnlyList<Country> GetCountriesByWorldRegion(string m49Code)
		{
			if (!WorldRegions.Any(region => region.M49Code == m49Code))
				throw new KeyNotFoundException($"World region '{m49Code}' was not found.");
			return _index.GetCountriesByRegion(m49Code);
		}

		public City GetCity(string id)
			=> TryGetCity(id, out var city)
				? city!
				: throw new KeyNotFoundException($"City '{id}' was not found.");

		public bool TryGetCity(string id, out City? city)
			=> _index.TryGetCity(id, out city);

		public IReadOnlyList<City> GetCities(string countryInput)
			=> _index.GetCitiesByCountry(GetCountry(countryInput).IsoAlpha2);

		public IReadOnlyList<City> GetCities(CountryId country)
			=> GetCities(GetCountry(country).IsoAlpha2);

		/// <summary>Returns municipalities assigned directly to this subdivision.</summary>
		public IReadOnlyList<City> GetDirectCitiesBySubdivision(string subdivisionId)
			=> _index.GetCitiesBySubdivision(GetSubdivision(subdivisionId).Id);

		/// <summary>Returns municipalities anywhere beneath this subdivision.</summary>
		public IReadOnlyList<City> GetCitiesBySubdivision(string subdivisionId)
		{
			var root = GetSubdivision(subdivisionId);
			var ids = GetDescendantSubdivisions(root.Id).Select(item => item.Id).Append(root.Id).ToHashSet(StringComparer.Ordinal);
			return Cities.Where(city => city.SubdivisionId is not null && ids.Contains(city.SubdivisionId)).ToList().AsReadOnly();
		}

		public IReadOnlyList<Subdivision> GetDescendantSubdivisions(string subdivisionId)
		{
			var pending = new Stack<Subdivision>(GetChildSubdivisions(subdivisionId));
			var result = new List<Subdivision>();
			while (pending.TryPop(out var current))
			{
				result.Add(current);
				foreach (var child in GetChildSubdivisions(current.Id)) pending.Push(child);
			}
			return result.OrderBy(item => item.Id, StringComparer.Ordinal).ToList().AsReadOnly();
		}

		/// <summary>Returns subdivision ancestors from the nearest parent outward.</summary>
		public IReadOnlyList<Subdivision> GetAncestorSubdivisions(string subdivisionId)
		{
			var parent = GetSubdivision(subdivisionId).ParentId;
			var result = new List<Subdivision>();
			while (parent is not null)
			{
				var current = GetSubdivision(parent);
				result.Add(current);
				parent = current.ParentId;
			}
			return result.AsReadOnly();
		}

		public CountryCoverage GetCoverage(string countryInput) => _index.Coverage[GetCountry(countryInput).IsoAlpha2];
		public CountryCoverage GetCoverage(CountryId country) => GetCoverage(GetCountry(country).IsoAlpha2);
		public IReadOnlyList<City> GetCapitalCities(string countryInput)
			=> GetCountry(countryInput).CapitalCityIds.Select(GetCity).ToList().AsReadOnly();
		public IReadOnlyList<City> GetCapitalCities(CountryId country) => GetCapitalCities(GetCountry(country).IsoAlpha2);

		public IReadOnlyList<City> FindCities(string name, CountryId country)
			=> FindCities(name, GetCountry(country).IsoAlpha2);

		public IReadOnlyList<City> FindCities(string name, string? countryInput = null)
		{
			var cities = _index.FindCitiesByName(name);
			if (countryInput is null)
				return cities;

			var country = GetCountry(countryInput);
			return cities
				.Where(city => city.CountryCode.Equals(
					country.IsoAlpha2,
					StringComparison.OrdinalIgnoreCase))
				.ToList()
				.AsReadOnly();
		}

		public IReadOnlyList<Language> GetOfficialLanguages(string countryInput)
			=> GetCountry(countryInput)
				.OfficialLanguageCodes
				.Select(_languages.GetIso3)
				.ToList()
				.AsReadOnly();

		public IReadOnlyList<Language> GetOfficialLanguages(CountryId country)
			=> GetOfficialLanguages(GetCountry(country).IsoAlpha2);
	}
}
