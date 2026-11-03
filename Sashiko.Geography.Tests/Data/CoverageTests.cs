using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Tests.Data
{
	public sealed class CoverageTests
	{
		[Fact]
		public void EmbeddedDataset_ShouldMeetItsExplicitCoverageFloors()
		{
			var registry = new GeographyRegistry();
			foreach (var floor in registry.CoverageBaseline.Countries)
			{
				var actual = registry.Coverage[floor.CountryCode];
				Assert.True(actual.SubdivisionCount >= floor.MinimumSubdivisionCount);
				Assert.True(actual.CityCount >= floor.MinimumCityCount);
				if (floor.MinimumCityPopulationCoverage > 0)
					Assert.True(actual.CityPopulationCoverage >= floor.MinimumCityPopulationCoverage);
				if (floor.MinimumSubdivisionPopulationCoverage > 0)
					Assert.True(actual.SubdivisionPopulationCoverage >= floor.MinimumSubdivisionPopulationCoverage);
			}
		}

		[Fact]
		public void PopulationCoverage_ShouldCountANonoverlappingFrontier()
		{
			var registry = new GeographyRegistry(GeographyDataFixture.Nested());
			Assert.Equal(0.8m, registry.Coverage["IT"].SubdivisionPopulationCoverage);
			Assert.Equal(0.1m, registry.Coverage["IT"].CityPopulationCoverage);
		}

		[Fact]
		public void Coverage_ShouldRejectMissingComparableDataWhenFloorIsPositive()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyRegistry.CoveragePath, node =>
				node["Countries"]![0]!["MinimumCityPopulationCoverage"] = 0.1m);
			Assert.Contains("Coverage fell below", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}
	}
}
