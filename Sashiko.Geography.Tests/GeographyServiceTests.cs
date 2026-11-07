using Sashiko.Geography.Api;
using Sashiko.Geography.Model;
using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Tests
{
	public sealed class GeographyServiceTests
	{
		private static GeographyService Service() => new(new GeographyRegistry(GeographyFixture.Populated()));

		[Fact]
		public void LookupsAndQueries_ShouldUseConsistentWhitespaceAndCaseRules()
		{
			var service = Service();
			Assert.Equal("TEST", service.GetCountry(" test ").Id);
			Assert.Equal("WORLD", service.GetWorldRegion(" world ").Id);
			Assert.Equal("TEST-NORTH", service.GetRegion(" test-north ").Id);
			Assert.Equal("TEST-ONE", service.GetCity(" test-one ").Id);
			Assert.Single(service.GetCountriesByWorldRegion(" world "));
			Assert.Equal(2, service.GetRegions(" test ").Count);
			Assert.Equal(2, service.GetCities(" test ").Count);
			Assert.Single(service.GetCitiesByRegion(" test-north "));
		}

		[Fact]
		public void DirectCountryCities_ShouldRemainDistinctFromRegionalCities()
		{
			var service = Service();
			Assert.Null(service.GetCity("TEST-TWO").RegionId);
			Assert.Equal("TEST-ONE", Assert.Single(service.GetCitiesByRegion("TEST-NORTH")).Id);
			Assert.Empty(service.GetCitiesByRegion("TEST-SOUTH"));
		}

		[Fact]
		public void AllCapitalReferences_ShouldResolveToTheSharedCityRecords()
		{
			var service = Service();
			Assert.Equal(service.GetCountry("TEST").CapitalCityIds, service.GetCapitalCities("TEST").Select(item => item.Id));
			foreach (var capital in service.GetCapitalCities("TEST")) Assert.Same(service.GetCity(capital.Id), capital);
		}

		[Fact]
		public void DuplicateCityNames_ShouldBeSearchableWithoutBecomingIdentity()
		{
			var files = GeographyFixture.Populated();
			GeographyFixture.AddCountry(files);
			GeographyFixture.AddCity(files, "ZZ-ONE", country: "ZZ");
			var service = new GeographyService(new GeographyRegistry(files));
			Assert.Equal(3, service.FindCities(" example ").Count);
			Assert.Equal(2, service.FindCities("Example", " test ").Count);
			Assert.Empty(service.FindCities("partial"));
			Assert.Throws<KeyNotFoundException>(() => service.FindCities("unrecorded", "UNKNOWN"));
		}

		[Fact]
		public void UnknownSubjects_ShouldDifferFromKnownSubjectsWithNoChildren()
		{
			var service = Service();
			Assert.Empty(service.GetCitiesByRegion("TEST-SOUTH"));
			Assert.Throws<KeyNotFoundException>(() => service.GetWorldRegion("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCountry("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetRegion("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCity("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCountriesByWorldRegion("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetRegions("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCities("UNKNOWN"));
			Assert.Throws<KeyNotFoundException>(() => service.GetCitiesByRegion("UNKNOWN"));
			Assert.False(service.TryGetWorldRegion("UNKNOWN", out _));
			Assert.False(service.TryGetCountry(null!, out _));
			Assert.False(service.TryGetRegion("UNKNOWN", out _));
			Assert.False(service.TryGetCity("UNKNOWN", out _));
		}

		[Fact]
		public void ReturnedCollectionsAndEntityLists_ShouldBeImmutable()
		{
			var service = Service();
			Assert.Throws<NotSupportedException>(() => ((IList<City>)service.Cities).Clear());
			Assert.Throws<NotSupportedException>(() => ((IList<City>)service.GetCitiesByRegion("TEST-NORTH")).Clear());
			Assert.Throws<NotSupportedException>(() => ((IList<string>)service.GetCountry("TEST").CapitalCityIds).Clear());
			Assert.Throws<NotSupportedException>(() => ((IList<string>)service.GetCity("TEST-ONE").SourceIds).Clear());
		}

		[Fact]
		public void ModelConstruction_ShouldDetachCallerOwnedLists()
		{
			var sources = new[] { "SOURCE" };
			var value = new WorldRegion("WORLD", "Example", sources);
			sources[0] = "CHANGED";
			Assert.Equal("SOURCE", Assert.Single(value.SourceIds));
		}

		[Fact]
		public async Task DefaultServices_ShouldShareOneImmutableRegistryAcrossConcurrentUse()
		{
			var services = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => new GeographyService())));
			foreach (var service in services) Assert.Same(services[0].Countries, service.Countries);
		}
	}
}
