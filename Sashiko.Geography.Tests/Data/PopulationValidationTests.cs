using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Tests.Data
{
	public sealed class PopulationValidationTests
	{
		private const string Leaf = "Countries/IT/Subdivisions/IT-NATIONAL-001.json";
		private const string City = "Countries/IT/Cities/IT-TEST-001.json";

		[Fact]
		public void UnknownIntermediate_ShouldNotHideAnExcessiveSubdivisionFromItsKnownAncestor()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.Update(files, Leaf, node => node["Population"]!["Value"] = 900);
			var error = Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files));
			Assert.Contains("IT-ROOT.json", error.Message);
			Assert.Contains("exceeds", error.Message);
		}

		[Fact]
		public void NonoverlappingSiblings_ShouldBeCheckedTogetherAtKnownAncestors()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.AddSubdivision(files, "IT-SIBLING", "IT-MIDDLE", 300);
			Assert.Contains("IT-ROOT.json", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Municipalities_ShouldBeCheckedAtAllComparableAncestors()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.Update(files, Leaf, node => node.AsObject().Remove("Population"));
			GeographyDataFixture.Update(files, City, node => node["Population"]!["Value"] = 900);
			Assert.Contains("IT-ROOT.json", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData("ReferenceDate", "2025-01-01")]
		[InlineData("Definition", "registered-residents")]
		[InlineData("BoundaryVersion", "TEST-2025")]
		public void DifferentObservations_ShouldCoexistWithoutBeingSummed(string property, string value)
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.Update(files, City, node =>
			{
				node["Population"]!["Value"] = 2000;
				node["Population"]![property] = value;
			});
			var registry = new GeographyRegistry(files);
			Assert.Equal(2000, Assert.Single(registry.Cities).Population!.Value);
			Assert.Equal(0m, registry.Coverage["IT"].CityPopulationCoverage);
		}

		[Fact]
		public void ANewerSubdivision_ShouldNotPreventCompatibleDescendantsFromContributing()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.Update(files, "Countries/IT/Subdivisions/IT-ROOT.json", node =>
				node["Population"]!["ReferenceDate"] = "2025-01-01");
			Assert.Equal(0.6m, new GeographyRegistry(files).Coverage["IT"].SubdivisionPopulationCoverage);
		}

		[Fact]
		public void ZeroAndUnknownPopulations_ShouldRemainDifferent()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddCity(files, "IT-EMPTY", population: 0);
			GeographyDataFixture.AddCity(files, "IT-UNKNOWN");
			var cities = new GeographyRegistry(files).Cities;
			Assert.Equal(0, cities.Single(city => city.Id == "IT-EMPTY").Population!.Value);
			Assert.Null(cities.Single(city => city.Id == "IT-UNKNOWN").Population);
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node => node["Population"]!["Value"] = 0);
			Assert.Null(new GeographyRegistry(files).Coverage["IT"].CityPopulationCoverage);
		}

		[Fact]
		public void Accumulation_ShouldNotOverflowLongBeforeRejectingInvalidTotals()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddCity(files, "IT-HUGE-A", population: long.MaxValue);
			GeographyDataFixture.AddCity(files, "IT-HUGE-B", population: long.MaxValue);
			Assert.Contains("exceeds", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}
	}
}
