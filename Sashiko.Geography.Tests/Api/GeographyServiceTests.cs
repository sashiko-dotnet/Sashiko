using Sashiko.Geography.Api;
using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Enums;
using Sashiko.Geography.Registry;
using Sashiko.Geography.Tests.Data;
using Sashiko.Languages.Api;

namespace Sashiko.Geography.Tests.Api
{
	public sealed class GeographyServiceTests
	{
		private static GeographyService Service(Dictionary<string, string>? files = null)
			=> new(new GeographyRegistry(files ?? GeographyDataFixture.Nested()), new LanguageService());

		[Theory]
		[InlineData("IT")]
		[InlineData("ita")]
		[InlineData("380")]
		[InlineData(" Italy ")]
		public void CountryLookup_ShouldAcceptLocalCodesAndTrimInput(string input)
			=> Assert.Equal("IT", Service().GetCountry(input).IsoAlpha2);

		[Fact]
		public void HierarchyQueries_ShouldKeepDescendantResultsWhenAnIntermediateLevelExists()
		{
			var service = Service();
			Assert.Equal("IT-ROOT", Assert.Single(service.GetRootSubdivisions(CountryId.It)).Id);
			Assert.Equal("IT-MIDDLE", Assert.Single(service.GetChildSubdivisions("it-root")).Id);
			Assert.Equal(2, service.GetDescendantSubdivisions("IT-ROOT").Count);
			Assert.Equal(new[] { "IT-MIDDLE", "IT-ROOT" }, service.GetAncestorSubdivisions("IT-NATIONAL-001").Select(item => item.Id));
			Assert.Empty(service.GetDirectCitiesBySubdivision("IT-ROOT"));
			Assert.Equal("IT-TEST-001", Assert.Single(service.GetCitiesBySubdivision("IT-ROOT")).Id);
			Assert.Single(service.GetDirectCitiesBySubdivision("IT-NATIONAL-001"));
		}

		[Fact]
		public void MissingParents_ShouldThrowWhileKnownEmptyParentsReturnEmptyCollections()
		{
			var service = Service();
			Assert.Empty(service.GetChildSubdivisions("IT-NATIONAL-001"));
			Assert.Throws<KeyNotFoundException>(() => service.GetChildSubdivisions("IT-MISSING"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCitiesBySubdivision("IT-MISSING"));
			Assert.False(service.TryGetCity("missing", out _));
			Assert.False(service.TryGetSubdivision("missing", out _));
			Assert.False(service.TryGetCountry((CountryId)999, out _));
		}

		[Fact]
		public void CountryApis_ShouldExposeCoverageAndResolvedLanguages()
		{
			var service = Service();
			Assert.Equal(3, service.GetSubdivisions(CountryId.It).Count);
			Assert.Single(service.GetCities(CountryId.It));
			Assert.Equal("ita", Assert.Single(service.GetOfficialLanguages(CountryId.It)).Iso639_3);
			Assert.Equal(0.1m, service.GetCoverage(CountryId.It).CityPopulationCoverage);
			Assert.Single(service.GetCountriesByWorldRegion("150"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCountriesByWorldRegion("999"));
		}

		[Fact]
		public void Capitals_ShouldBeDerivedFromCountryReferencesAndAllowSeveralOrNone()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddCity(files, "IT-CAPITAL-A");
			GeographyDataFixture.AddCity(files, "IT-CAPITAL-B");
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node =>
				node["CapitalCityIds"] = new System.Text.Json.Nodes.JsonArray("IT-CAPITAL-A", "IT-CAPITAL-B"));
			var service = Service(files);
			Assert.Equal(2, service.GetCapitalCities(CountryId.It).Count);
			Assert.All(service.Cities, city => Assert.True(city.IsCapital));
			Assert.Empty(Service(GeographyDataFixture.Minimal()).GetCapitalCities("IT"));
		}

		[Fact]
		public void NameQueries_ShouldReturnAllMatchesWithoutConflatingIdentity()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.AddCity(files, "IT-TEST-002");
			var service = Service(files);
			Assert.Equal(2, service.FindCities(" example ", CountryId.It).Count);
			Assert.Empty(service.FindCities("absent"));
		}

		[Fact]
		public void DefaultServices_ShouldShareImmutableIndexes()
		{
			var first = new GeographyService();
			var second = new GeographyService();
			Assert.Same(first.Countries, second.Countries);
			Assert.Same(first.GetCities(CountryId.It), second.GetCities(CountryId.It));
			Assert.Throws<NotSupportedException>(() => ((IList<Country>)first.Countries).Clear());
			Assert.Throws<NotSupportedException>(() => ((IList<string>)first.GetCountry(CountryId.It).CapitalCityIds).Clear());
		}
	}
}
