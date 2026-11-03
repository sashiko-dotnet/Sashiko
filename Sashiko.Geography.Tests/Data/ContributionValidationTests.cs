using System.Text.Json.Nodes;
using Sashiko.Geography.Registry;
using Sashiko.Registries;
using Sashiko.Validation;

namespace Sashiko.Geography.Tests.Data
{
	public sealed class ContributionValidationTests
	{
		[Theory]
		[InlineData("Name", "\"\"")]
		[InlineData("IsoAlpha3", "\"IT\"")]
		[InlineData("IsoNumeric", "\"38\"")]
		[InlineData("M49RegionCode", "\"999\"")]
		[InlineData("CapitalCityIds", "[\"IT-MISSING\"]")]
		[InlineData("OfficialLanguageCodes", "[\"zzz\"]")]
		[InlineData("SourceIds", "[\"MISSING\"]")]
		[InlineData("SourceIds", "[\"TEST\",null]")]
		[InlineData("Population.Value", "-1")]
		[InlineData("Population.ReferenceDate", "\"2024-02-30\"")]
		[InlineData("Population.Definition", "\"\"")]
		[InlineData("Population.BoundaryVersion", "\"\"")]
		[InlineData("Population.SourceId", "\"MISSING\"")]
		public void CountryContributions_ShouldRejectInvalidValuesWithTheirSource(string property, string json)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node =>
			{
				var parts = property.Split('.');
				var target = parts.Length == 1 ? node : node[parts[0]]!;
				target[parts[^1]] = JsonNode.Parse(json);
			});
			var error = Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files));
			Assert.Contains(GeographyDataFixture.Italy, error.Message);
		}

		[Theory]
		[InlineData("Name")]
		[InlineData("IsoAlpha3")]
		[InlineData("CapitalCityIds")]
		[InlineData("SourceIds")]
		public void RequiredValues_ShouldRejectExplicitNull(string property)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node => node[property] = null);
			var error = Record.Exception(() => new GeographyRegistry(files));
			Assert.True(error is ValidationException or RegistryLoadException);
			Assert.Contains(GeographyDataFixture.Italy, error!.Message);
		}

		[Theory]
		[InlineData("IT-MISSING")]
		[InlineData("RO-ROOT")]
		public void Parents_ShouldRejectMissingAndCrossCountryReferences(string parent)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddRomania(files);
			GeographyDataFixture.AddSubdivision(files, "IT-CHILD", parent);
			files["Countries/RO/Subdivisions/RO-ROOT.json"] =
				"""{"Id":"RO-ROOT","Name":"Root","Type":"County","SourceIds":["TEST"]}""";
			Assert.Contains("ParentId", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Capitals_ShouldRejectForeignMunicipalities()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddRomania(files);
			files["Countries/RO/Cities/RO-TEST-001.json"] =
				"""{"Id":"RO-TEST-001","Name":"Capital","SourceIds":["TEST"]}""";
			GeographyDataFixture.Update(files, GeographyDataFixture.Italy, node =>
				node["CapitalCityIds"] = new JsonArray("RO-TEST-001"));
			Assert.Contains("CapitalCityIds", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void MunicipalityParents_ShouldRejectForeignAdministrativeUnits()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddRomania(files);
			files["Countries/RO/Subdivisions/RO-ROOT.json"] =
				"""{"Id":"RO-ROOT","Name":"Root","Type":"County","SourceIds":["TEST"]}""";
			GeographyDataFixture.AddCity(files, "IT-TEST-001", "RO-ROOT");
			Assert.Contains("SubdivisionId", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Hierarchy_ShouldRejectCyclesIncludingSelfParents()
		{
			var files = GeographyDataFixture.Nested();
			GeographyDataFixture.Update(files, "Countries/IT/Subdivisions/IT-ROOT.json", node => node["ParentId"] = "IT-NATIONAL-001");
			Assert.Contains("cycle", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
			files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddSubdivision(files, "IT-SELF", "IT-SELF");
			Assert.Contains("cycle", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void RecordIdentity_ShouldMatchItsFileAndBeUniqueAcrossEntityKinds()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddSubdivision(files, "IT-ROOT");
			GeographyDataFixture.AddCity(files, "IT-ROOT");
			Assert.Contains("Duplicate identifier", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
			files.Remove("Countries/IT/Cities/IT-ROOT.json");
			GeographyDataFixture.Update(files, "Countries/IT/Subdivisions/IT-ROOT.json", node => node["Id"] = "IT-OTHER");
			Assert.Contains("does not match", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData("RO-XY")]
		[InlineData("IT-TOOLONG")]
		public void IsoMetadata_ShouldEnforceFormatAndCountryOwnership(string code)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddSubdivision(files, "IT-LOCAL-001");
			GeographyDataFixture.Update(files, "Countries/IT/Subdivisions/IT-LOCAL-001.json", node => node["Iso3166_2"] = code);
			Assert.Contains("Iso3166_2", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Theory]
		[InlineData(91, 0)]
		[InlineData(0, -181)]
		public void Coordinates_ShouldRejectInvalidRanges(int latitude, int longitude)
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddCity(files, "IT-TEST-001");
			GeographyDataFixture.Update(files, "Countries/IT/Cities/IT-TEST-001.json", node =>
				node["Location"] = new JsonObject { ["Latitude"] = latitude, ["Longitude"] = longitude });
			Assert.Contains("Location", Assert.Throws<InvalidDataException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void Catalogs_ShouldValidateEveryArrayElementNotJustTheFirst()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.Update(files, GeographyRegistry.SourcesPath, node =>
			{
				var second = node[0]!.DeepClone();
				second["Id"] = "TEST2";
				second["Unexpected"] = "must not be ignored";
				node.AsArray().Add(second);
			});
			Assert.Contains("Unexpected", Assert.Throws<ValidationException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void NestedPolicyArrays_ShouldRejectUnknownFieldsBeyondTheirFirstElement()
		{
			var files = GeographyDataFixture.Minimal();
			GeographyDataFixture.AddRomania(files);
			GeographyDataFixture.Update(files, GeographyRegistry.CoveragePath, node => node["Countries"]![1]!["Unexpected"] = 1);
			Assert.Contains(GeographyRegistry.CoveragePath, Assert.Throws<RegistryLoadException>(() => new GeographyRegistry(files)).Message);
		}

		[Fact]
		public void DeepHierarchies_ShouldNotRequireRecursiveCallStacks()
		{
			var files = GeographyDataFixture.Minimal();
			string? parent = null;
			for (var index = 0; index < 256; index++)
			{
				var id = $"IT-LEVEL-{index:D3}";
				GeographyDataFixture.AddSubdivision(files, id, parent);
				parent = id;
			}
			Assert.Equal(256, new GeographyRegistry(files).Subdivisions.Count);
		}
	}
}
