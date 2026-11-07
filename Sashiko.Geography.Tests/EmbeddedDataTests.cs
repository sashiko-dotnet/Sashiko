using System.Text.Json;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Api;
using Sashiko.Geography.Registry;
using Sashiko.Maintenance.Libraries.Geography;

namespace Sashiko.Geography.Tests
{
	public sealed class EmbeddedDataTests
	{
		[Fact]
		public void EveryEmbeddedCollection_ShouldValidateAndLoadThroughTheActualRegistry()
		{
			var files = GeographyDataFiles.ReadEmbedded();
			var validated = GeographyDataValidator.Read(files);
			var runtime = new GeographyRegistry();
			Assert.Equal(files.Count, validated.Documents.Count);
			Assert.Equal(validated.WorldRegions.OrderBy(item => item.Id).Select(item => item.Id), runtime.WorldRegions.Select(item => item.Id));
			Assert.Equal(validated.Countries.OrderBy(item => item.Id).Select(item => item.Id), runtime.Countries.Select(item => item.Id));
			Assert.Equal(validated.Regions.OrderBy(item => item.Id).Select(item => item.Id), runtime.Regions.Select(item => item.Id));
			Assert.Equal(validated.Cities.OrderBy(item => item.Id).Select(item => item.Id), runtime.Cities.Select(item => item.Id));
			Assert.Equal(validated.Sources.Select(item => item.Id), runtime.Sources.Select(item => item.Id));
		}

		[Fact]
		public void EveryEmbeddedCollection_ShouldAlreadyBePolished()
		{
			var files = GeographyDataFiles.ReadEmbedded();
			foreach (var (path, value) in GeographyDataValidator.Read(files).Documents)
				Assert.Equal(JsonSerializer.Serialize(value, value.GetType(), JsonWriteOptions.Indented), files[path]);
		}

		[Fact]
		public void Registry_ShouldTrustEmbeddedRecordsWithoutSemanticValidationOrNormalization()
		{
			var files = GeographyFixture.Minimal();
			GeographyFixture.Update(files, GeographyDataFiles.Countries, rows =>
			{
				rows[0]!["Name"] = "  Unpolished  ";
				rows[0]!["WorldRegionId"] = "UNRESOLVED";
			});
			Assert.Throws<InvalidDataException>(() => GeographyDataValidator.Read(files));
			var runtime = new GeographyRegistry(files);
			Assert.Equal("  Unpolished  ", runtime.GetCountry("TEST").Name);
			Assert.Equal("UNRESOLVED", runtime.GetCountry("TEST").WorldRegionId);
		}

		[Fact]
		public void Registry_ShouldAllowCountryOnlyDataWithoutIsoCodesOrEnumChanges()
		{
			var files = GeographyFixture.Minimal();
			GeographyDataValidator.Read(files);
			var service = new GeographyService(new GeographyRegistry(files));
			Assert.Null(service.GetCountry("TEST").IsoAlpha2);
			Assert.Empty(service.GetRegions("TEST"));
			Assert.Empty(service.GetCities("TEST"));
			Assert.Empty(service.GetCapitalCities("TEST"));
		}

		[Fact]
		public void RuntimeAssembly_ShouldNotDependOnLanguagesOrMaintenance()
		{
			var references = typeof(GeographyService).Assembly.GetReferencedAssemblies().Select(item => item.Name);
			Assert.DoesNotContain("Sashiko.Languages", references);
			Assert.DoesNotContain("Sashiko.Maintenance", references);
		}

		[Fact]
		public void Chunking_ShouldNotChangeEntityIdentityOrQueryResults()
		{
			var files = GeographyFixture.Populated();
			const string path = "Countries/TEST/Cities/TEST-NORTH.json";
			var original = new GeographyService(new GeographyRegistry(files));
			files["Countries/TEST/Cities/TEST-NORTH-part-001.json"] = files[path];
			files.Remove(path);
			GeographyDataValidator.Read(files);
			var moved = new GeographyService(new GeographyRegistry(files));
			Assert.Equal(original.GetCities("TEST").Select(item => item.Id), moved.GetCities("TEST").Select(item => item.Id));
		}
	}
}
