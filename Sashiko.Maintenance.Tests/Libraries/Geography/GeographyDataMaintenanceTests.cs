using System.Text.Json.Nodes;
using Sashiko.Maintenance.Libraries.Geography;

namespace Sashiko.Maintenance.Tests.Libraries.Geography
{
	public sealed class GeographyDataMaintenanceTests
	{
		[Fact]
		public async Task Polish_ShouldNormalizeSortAndBeIdempotent()
		{
			using var fixture = new DataDirectory();
			fixture.Update("countries.json", rows =>
			{
				var other = rows[0]!.DeepClone();
				other["Id"] = "AA";
				other["Name"] = "  Cafe\u0301  ";
				other["SourceIds"] = new JsonArray(" source ");
				rows.Add(other);
			});
			await GeographyDataMaintenance.RunAsync(fixture.Path, "polish");
			var country = JsonNode.Parse(File.ReadAllText(System.IO.Path.Combine(fixture.Path, "countries.json")))![0]!;
			Assert.Equal("AA", country["Id"]!.GetValue<string>());
			Assert.Equal("Café", country["Name"]!.GetValue<string>());
			Assert.Equal("SOURCE", country["SourceIds"]![0]!.GetValue<string>());
			var polished = fixture.Snapshot();
			await GeographyDataMaintenance.RunAsync(fixture.Path, "polish");
			Assert.Equal(polished, fixture.Snapshot());
			await GeographyDataMaintenance.RunAsync(fixture.Path, "check");
		}

		[Fact]
		public async Task Validate_ShouldCheckDataWithoutRewritingFormatting()
		{
			using var fixture = new DataDirectory();
			var before = fixture.Snapshot();
			await GeographyDataMaintenance.RunAsync(fixture.Path, "validate");
			Assert.Equal(before, fixture.Snapshot());
		}

		[Fact]
		public async Task Check_ShouldRejectUnpolishedDataWithoutWriting()
		{
			using var fixture = new DataDirectory();
			var before = fixture.Snapshot();
			Assert.Contains("polish", (await Assert.ThrowsAsync<InvalidDataException>(
				() => GeographyDataMaintenance.RunAsync(fixture.Path, "check"))).Message);
			Assert.Equal(before, fixture.Snapshot());
		}

		[Fact]
		public async Task InvalidProposal_ShouldFailBeforeAnyNormalizedFilesAreWritten()
		{
			using var fixture = new DataDirectory();
			fixture.Update("countries.json", rows => rows[0]!["Name"] = "  Needs polishing  ");
			fixture.Update("world-regions.json", rows => rows[0]!["SourceIds"] = new JsonArray("UNKNOWN"));
			var before = fixture.Snapshot();
			await Assert.ThrowsAsync<InvalidDataException>(() => GeographyDataMaintenance.RunAsync(fixture.Path, "polish"));
			Assert.Equal(before, fixture.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldNotSilentlyRemoveDuplicateEvidence()
		{
			using var fixture = new DataDirectory();
			fixture.Update("countries.json", rows => rows[0]!["SourceIds"] = new JsonArray("SOURCE", "source"));
			var before = fixture.Snapshot();
			Assert.Contains("Duplicate", (await Assert.ThrowsAsync<InvalidDataException>(
				() => GeographyDataMaintenance.RunAsync(fixture.Path, "polish"))).Message);
			Assert.Equal(before, fixture.Snapshot());
		}

		[Fact]
		public async Task ReleaseCheck_ShouldRequireCanonicalDataAndRecordedReuseReview()
		{
			using var fixture = new DataDirectory();
			fixture.Update("sources.json", rows => rows[0]!.AsObject().Remove("LicenseUrl"));
			await GeographyDataMaintenance.RunAsync(fixture.Path, "polish");
			var before = fixture.Snapshot();
			Assert.Contains("SOURCE", (await Assert.ThrowsAsync<InvalidDataException>(
				() => GeographyDataMaintenance.RunAsync(fixture.Path, "validate-release"))).Message);
			Assert.Equal(before, fixture.Snapshot());
			fixture.Update("sources.json", rows => rows[0]!["LicenseUrl"] = "https://example.com/review");
			await GeographyDataMaintenance.RunAsync(fixture.Path, "polish");
			await GeographyDataMaintenance.RunAsync(fixture.Path, "validate-release");
		}

		[Fact]
		public async Task MissingDirectory_ShouldNotBeCreatedByValidation()
		{
			using var fixture = new DataDirectory();
			var missing = System.IO.Path.Combine(fixture.Path, "missing");
			await Assert.ThrowsAsync<DirectoryNotFoundException>(() => GeographyDataMaintenance.RunAsync(missing, "validate"));
			Assert.False(Directory.Exists(missing));
		}

		private sealed class DataDirectory : IDisposable
		{
			internal string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sashiko-geography-{Guid.NewGuid():N}");
			internal DataDirectory()
			{
				Directory.CreateDirectory(Path);
				File.WriteAllText(System.IO.Path.Combine(Path, "world-regions.json"),
					"""[{"Id":"WORLD","Name":"World","SourceIds":["SOURCE"]}]""");
				File.WriteAllText(System.IO.Path.Combine(Path, "countries.json"),
					"""[{"Id":"TEST","Name":"Example","WorldRegionId":"WORLD","CapitalCityIds":[],"SourceIds":["SOURCE"]}]""");
				File.WriteAllText(System.IO.Path.Combine(Path, "sources.json"),
					"""[{"Id":"SOURCE","Title":"Synthetic data","Url":"https://example.com/data","Attribution":"Synthetic test evidence.","LicenseUrl":"https://example.com/review"}]""");
			}
			internal void Update(string relative, Action<JsonArray> action)
			{
				var file = System.IO.Path.Combine(Path, relative);
				var rows = JsonNode.Parse(File.ReadAllText(file))!.AsArray();
				action(rows);
				File.WriteAllText(file, rows.ToJsonString());
			}
			internal string[] Snapshot() => Directory.GetFiles(Path).Order(StringComparer.Ordinal).Select(File.ReadAllText).ToArray();
			public void Dispose() => Directory.Delete(Path, recursive: true);
		}
	}
}
