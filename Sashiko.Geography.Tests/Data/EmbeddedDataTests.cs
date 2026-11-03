using System.Text.Json;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Model.Enums;
using Sashiko.Geography.Registry;
using Sashiko.Validation;
using Sashiko.Registries;

namespace Sashiko.Geography.Tests.Data
{
	public sealed class EmbeddedDataTests
	{
		[Fact]
		public void EmbeddedData_ShouldLoadAllRecordsAndDeclaredCountryIds()
		{
			var files = GeographyDataFixture.Files();
			var registry = new GeographyRegistry(files);
			Assert.NotEmpty(registry.Countries);
			Assert.Equal(files.Keys.Count(path => path.StartsWith("Countries/", StringComparison.Ordinal)), registry.Documents.Count);
			foreach (var declared in Enum.GetValues<CountryId>())
				Assert.Contains(registry.Countries, country => country.IsoAlpha2.Equals(declared.ToString(), StringComparison.OrdinalIgnoreCase));
		}

		[Fact]
		public void EmbeddedEntities_ShouldBeCanonicallyFormatted()
		{
			var files = GeographyDataFixture.Files();
			foreach (var (path, document) in new GeographyRegistry(files).Documents)
				Assert.Equal(JsonSerializer.Serialize(document, document.GetType(), JsonWriteOptions.Indented), files[path].TrimEnd());
		}

		[Fact]
		public void Loader_ShouldRejectUnsupportedSchemaVersions()
		{
			var files = GeographyDataFixture.Minimal();
			files[GeographyRegistry.ManifestPath] = """{"SchemaVersion":2}""";
			Assert.Contains("SchemaVersion", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData(GeographyRegistry.ManifestPath)]
		[InlineData(GeographyRegistry.SourcesPath)]
		[InlineData(GeographyRegistry.RegionsPath)]
		[InlineData(GeographyRegistry.CoveragePath)]
		public void Loader_ShouldRequireDatasetMetadata(string path)
		{
			var files = GeographyDataFixture.Minimal();
			files.Remove(path);
			Assert.Contains(path, Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData("Name")]
		[InlineData("IsoAlpha2")]
		[InlineData("SourceIds")]
		public void Schema_ShouldRejectMissingRequiredFields(string property)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node => node.AsObject().Remove(property));
			Assert.Contains(property, Assert.Throws<ValidationException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Loader_ShouldRejectMalformedJsonWithSourcePath()
		{
			var files = GeographyDataFixture.Minimal();
			files[GeographyDataFixture.Italy] = "{";
			Assert.Contains(GeographyDataFixture.Italy, Assert.Throws<RegistryLoadException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Loader_ShouldRejectDuplicateProperties()
		{
			var files = GeographyDataFixture.Minimal();
			files[GeographyDataFixture.Italy] = files[GeographyDataFixture.Italy].Replace(
				"\"IsoNumeric\":\"380\"", "\"IsoNumeric\":\"381\",\"IsoNumeric\":\"380\"", StringComparison.Ordinal);
			Assert.Contains("Duplicate property", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData("Countries/IT/legacy.json")]
		[InlineData("Countries/IT/Subdivisions/IT-ROOT/subdivision.json")]
		[InlineData("Countries/IT/../city.json")]
		public void Loader_ShouldRejectUnknownOrUnsafePaths(string path)
		{
			var files = GeographyDataFixture.Minimal();
			files[path] = "{}";
			Assert.Contains(path, Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Schema_ShouldRejectUnknownNestedFields()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node => node["Population"]!["Extra"] = "unexpected");
			Assert.Contains("Extra", Assert.Throws<ValidationException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void CountryCodes_ShouldValidateFormatWithoutExternalAuthorityLookups()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node => node["IsoAlpha3"] = "ZZZ");
			Assert.Equal("ZZZ", Assert.Single(new GeographyRegistry(files).Countries).IsoAlpha3);
		}

		[Fact]
		public void Registry_ShouldAllowCountryOnlyAndUnknownPopulation()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node =>
			{
				node.AsObject().Remove("Population");
				node["OfficialLanguageCodes"] = new System.Text.Json.Nodes.JsonArray();
			});
			var registry = new GeographyRegistry(files);
			Assert.Null(Assert.Single(registry.Countries).Population);
			Assert.Empty(registry.Cities);
			Assert.Null(registry.Coverage["IT"].CityPopulationCoverage);
		}

		[Fact]
		public void Registry_ShouldAllowNationalSubdivisionIdsAndCountryLevelCities()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddSubdivision(files, "IT-NATIONAL-00001");
			GeographyDataFixture.AddCity(files, "IT-TEST-001");
			var registry = new GeographyRegistry(files);
			Assert.Null(Assert.Single(registry.Subdivisions).Iso3166_2);
			Assert.Null(Assert.Single(registry.Cities).SubdivisionId);
		}
	}
}
