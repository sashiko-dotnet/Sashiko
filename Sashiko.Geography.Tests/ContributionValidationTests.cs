using System.Text.Json.Nodes;
using Sashiko.Geography.Registry;
using Sashiko.Maintenance.Libraries.Geography;
using Sashiko.Registries;
using Sashiko.Validation;

namespace Sashiko.Geography.Tests
{
	public sealed class ContributionValidationTests
	{
		[Theory]
		[InlineData("Name", "\"\"")]
		[InlineData("Id", "\"bad id\"")]
		[InlineData("Name", "null")]
		[InlineData("WorldRegionId", "\"UNKNOWN\"")]
		[InlineData("CapitalCityIds", "[\"MISSING\"]")]
		[InlineData("CapitalCityIds", "[null]")]
		[InlineData("SourceIds", "[]")]
		[InlineData("SourceIds", "null")]
		[InlineData("CapitalCityIds", "null")]
		[InlineData("SourceIds", "[\"UNKNOWN\"]")]
		[InlineData("SourceIds", "[\"SOURCE\",null]")]
		[InlineData("SourceIds", "[\"SOURCE\",\"SOURCE\"]")]
		[InlineData("IsoAlpha2", "\"LONG\"")]
		[InlineData("OfficialLanguageCodes", "[\"zzz\"]")]
		[InlineData("OfficialLanguageCodes", "[\"en\"]")]
		public void InvalidCountryContributions_ShouldFailWithTheirSource(string property, string json)
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows => rows[0]![property] = JsonNode.Parse(json));
			Reject(files, GeographyDataFiles.Countries);
		}

		[Theory]
		[InlineData("Id")]
		[InlineData("Name")]
		[InlineData("CapitalCityIds")]
		[InlineData("SourceIds")]
		public void MissingRequiredFields_ShouldFailDuringDevelopment(string field)
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows => rows[0]!.AsObject().Remove(field));
			Reject(files, GeographyDataFiles.Countries);
		}

		[Theory]
		[InlineData(GeographyDataFiles.WorldRegions)]
		[InlineData(GeographyDataFiles.Countries)]
		[InlineData(GeographyDataFiles.Sources)]
		[InlineData("Countries/TEST/regions.json")]
		[InlineData("Countries/TEST/Cities/TEST-NORTH.json")]
		public void EveryCollection_ShouldRejectNullRowsBeyondItsFirstEntry(string path)
		{
			var files = GeographyFixture.Populated();
			GeographyFixture.Update(files, path, rows => rows.Add((JsonNode?)null));
			Reject(files, path);
		}

		[Theory]
		[InlineData("{")]
		[InlineData("{}")]
		[InlineData("null")]
		public void MalformedOrNonArrayCollections_ShouldFailWithTheirSource(string json)
		{
			var files = GeographyFixture.Minimal();
			files[GeographyDataFiles.Countries] = json;
			Reject(files, GeographyDataFiles.Countries);
		}

		[Fact]
		public void UnknownFieldsAndDuplicateProperties_ShouldNotBeSilentlyAccepted()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.AddCountry(files);
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows => rows[1]!["Typo"] = 1);
			Reject(files, GeographyDataFiles.Countries);
			files = GeographyFixture.Minimal();
			files[GeographyDataFiles.Countries] = files[GeographyDataFiles.Countries].Replace(
				"\"Name\":\"Example country\"", "\"Name\":\"Wrong\",\"Name\":\"Example country\"", StringComparison.Ordinal);
			Assert.Contains("Duplicate JSON property", Reject(files, GeographyDataFiles.Countries).Message);
		}

		[Theory]
		[InlineData(GeographyDataFiles.WorldRegions)]
		[InlineData(GeographyDataFiles.Countries)]
		[InlineData(GeographyDataFiles.Sources)]
		public void MissingCatalogs_ShouldFailWithTheirSource(string path)
		{
			var files = GeographyFixture.Minimal();
			files.Remove(path);
			Reject(files, path);
		}

		[Fact]
		public void DuplicateIdentities_ShouldBeRejectedAcrossCityParts()
		{
			var files = GeographyFixture.Populated();
			files["Countries/TEST/Cities/TEST-NORTH-part-001.json"] = files["Countries/TEST/Cities/TEST-NORTH.json"];
			Assert.Contains("Duplicate identifier", Reject(files, "Cities/").Message);
		}

		[Fact]
		public void References_ShouldRejectUnknownCountriesAndForeignRegions()
		{
			var files = GeographyFixture.Populated();
			GeographyFixture.Update(files, "Countries/TEST/regions.json", rows => rows[0]!["CountryId"] = "UNKNOWN");
			Assert.Contains("CountryId", Reject(files, "regions.json").Message);
			files = GeographyFixture.Populated();
			GeographyFixture.AddCountry(files);
			GeographyFixture.AddRegion(files, "ZZ-NORTH", "ZZ");
			GeographyFixture.Update(files, "Countries/TEST/Cities/TEST-NORTH.json", rows => rows[0]!["RegionId"] = "ZZ-NORTH");
			Assert.Contains("foreign region", Reject(files, "TEST-NORTH.json").Message);
		}

		[Fact]
		public void ForeignCapitals_ShouldBeRejected()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.AddCountry(files);
			GeographyFixture.AddCity(files, "ZZ-ONE", country: "ZZ");
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows => rows[0]!["CapitalCityIds"] = new JsonArray("ZZ-ONE"));
			Assert.Contains("foreign city", Reject(files, GeographyDataFiles.Countries).Message);
		}

		[Theory]
		[InlineData("Countries/TEST/Cities/UNKNOWN.json")]
		[InlineData("Countries/TEST/Cities/TEST-SOUTH.json")]
		[InlineData("Countries/ZZ/Cities/TEST-NORTH.json")]
		[InlineData("Countries/TEST/Cities/../escape.json")]
		[InlineData("Countries/../regions.json")]
		[InlineData("unsupported.json")]
		public void IncorrectOrUnsafePartitions_ShouldBeRejected(string destination)
		{
			var files = GeographyFixture.Populated();
			const string original = "Countries/TEST/Cities/TEST-NORTH.json";
			files[destination] = files[original];
			files.Remove(original);
			Reject(files, destination);
		}

		[Theory]
		[InlineData(-91, 0)]
		[InlineData(91, 0)]
		[InlineData(0, -181)]
		[InlineData(0, 181)]
		public void Coordinates_ShouldRejectOutOfRangeValues(int latitude, int longitude)
		{
			var files = GeographyFixture.Populated();
			GeographyFixture.Update(files, "Countries/TEST/Cities/TEST-NORTH.json", rows =>
				rows[0]!["Coordinate"] = new JsonObject { ["Latitude"] = latitude, ["Longitude"] = longitude });
			Assert.Contains("Coordinate", Reject(files, "TEST-NORTH.json").Message);
		}

		[Fact]
		public void SourceMetadata_ShouldRequireUsableUrlsAndAttribution()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Sources, rows => rows[0]!["Url"] = "file:///local/data");
			Reject(files, GeographyDataFiles.Sources);
			files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Sources, rows => rows[0]!["Attribution"] = "");
			Reject(files, GeographyDataFiles.Sources);
		}

		[Fact]
		public void IsoMetadata_ShouldCheckLocalUniquenessAndFormatWithoutExternalLookups()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows => rows[0]!["IsoAlpha2"] = "AA");
			GeographyDataValidator.Read(files); // Synthetic code: format is checked, assignment is reviewed offline.
			GeographyFixture.AddCountry(files);
			Assert.Contains("IsoAlpha2", Reject(files, GeographyDataFiles.Countries).Message);
		}

		[Theory]
		[InlineData("{\"Longitude\":0}")]
		[InlineData("{\"Latitude\":0,\"Longitude\":0,\"Typo\":1}")]
		public void Coordinates_ShouldRequireTheirCompleteDeclaredShape(string coordinate)
		{
			var files = GeographyFixture.Populated();
			GeographyFixture.Update(files, "Countries/TEST/Cities/TEST-NORTH.json", rows => rows[0]!["Coordinate"] = JsonNode.Parse(coordinate));
			Reject(files, "TEST-NORTH.json");
		}

		[Fact]
		public void PolishingDiagnostics_ShouldIdentifyTheOriginalRowBeforeSorting()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.AddCountry(files);
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows =>
			{
				var last = rows[1]!.DeepClone();
				last["WorldRegionId"] = "MISSING";
				rows.RemoveAt(1);
				rows.Insert(0, last);
			});
			var error = Assert.Throws<InvalidDataException>(() => GeographyDataValidator.Read(files, polish: true));
			Assert.Contains("countries.json[0]", error.Message);
		}

		private static Exception Reject(Dictionary<string, string> files, string source)
		{
			var error = Record.Exception(() => GeographyDataValidator.Read(files));
			Assert.True(error is InvalidDataException or RegistryLoadException or ValidationException,
				$"Expected a data diagnostic, received {error?.GetType().Name ?? "no exception"}.");
			Assert.Contains(source, error!.Message);
			return error;
		}
	}
}
