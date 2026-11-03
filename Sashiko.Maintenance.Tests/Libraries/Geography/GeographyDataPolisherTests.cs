using System.Text.Json.Nodes;
using Sashiko.Geography.Api;
using Sashiko.Maintenance.Libraries.Geography;
using Sashiko.Validation;

namespace Sashiko.Maintenance.Tests.Libraries.Geography
{
	public sealed class GeographyDataPolisherTests
	{
		private const string Italy = "Countries/IT/country.json";
		private const string Rome = "Countries/IT/Cities/IT-ISTAT-058091.json";

		[Fact]
		public async Task Polish_ShouldNormalizeRecordsAndBeIdempotent()
		{
			using var data = new DataDirectory();
			data.Edit(Italy, node =>
			{
				node["Name"] = " Italy ";
				node["IsoAlpha2"] = " it ";
				node["OfficialLanguageCodes"]![0] = " ITA ";
			});
			var polisher = new GeographyDataPolisher();
			var result = await polisher.PolishAsync(data.Root);
			Assert.Equal(data.Expected, result);
			var country = JsonNode.Parse(File.ReadAllText(Path.Combine(data.Root, Italy)))!;
			Assert.Equal("Italy", country["Name"]!.GetValue<string>());
			Assert.Equal("IT", country["IsoAlpha2"]!.GetValue<string>());
			Assert.Equal("ita", country["OfficialLanguageCodes"]![0]!.GetValue<string>());
			var first = data.Snapshot();
			await polisher.PolishAsync(data.Root);
			Assert.Equal(first, data.Snapshot());
			Assert.Equal("unchanged", File.ReadAllText(Path.Combine(data.Root, "SOURCES.md")));
		}

		[Fact]
		public async Task Validate_ShouldReadAllRecordsWithoutWriting()
		{
			using var data = new DataDirectory();
			var before = data.Snapshot();
			var result = await new GeographyDataPolisher().ValidateAsync(data.Root);
			Assert.Equal(data.Expected, result);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Validate_ShouldRejectUnpolishedValuesWithoutChangingThem()
		{
			using var data = new DataDirectory();
			data.Edit(Italy, node => node["Name"] = " Italy ");
			var before = data.Snapshot();
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => new GeographyDataPolisher().ValidateAsync(data.Root));
			Assert.Contains("Name", error.Message);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldValidateEveryCountryBeforeWritingAnyFile()
		{
			using var data = new DataDirectory();
			data.Edit(Italy, node => node["Name"] = " Italy ");
			data.Edit("Countries/RO/country.json", node => node["Population"]!["Value"] = -1);
			var before = data.Snapshot();
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => new GeographyDataPolisher().PolishAsync(data.Root));
			Assert.Contains("Countries/RO/country.json", error.Message);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldRejectUnknownFieldsWithoutDiscardingThem()
		{
			using var data = new DataDirectory();
			data.Edit(Rome, node => node["Unrecognized"] = "must not be lost");
			var before = data.Snapshot();
			var error = await Assert.ThrowsAsync<ValidationException>(() => new GeographyDataPolisher().PolishAsync(data.Root));
			Assert.Contains("Unrecognized", error.Message);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldRejectMisplacedCityWithSourcePath()
		{
			using var data = new DataDirectory();
			data.Edit(Rome, node => node["SubdivisionId"] = "IT-MISSING");
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => new GeographyDataPolisher().PolishAsync(data.Root));
			Assert.Contains(Rome, error.Message);
			Assert.Contains("SubdivisionId", error.Message);
		}

		[Fact]
		public async Task Polish_ShouldNotReduceCoverageFloors()
		{
			using var data = new DataDirectory();
			data.Edit(Rome, node => node["Population"]!["Value"] = 1);
			var before = data.Snapshot();
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => new GeographyDataPolisher().PolishAsync(data.Root));
			Assert.Contains("Coverage fell below", error.Message);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldRejectMissingDirectory()
		{
			var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			await Assert.ThrowsAsync<DirectoryNotFoundException>(() => new GeographyDataPolisher().PolishAsync(missing));
		}

		[Fact]
		public async Task ReleaseValidation_ShouldBlockUnreviewedSourcesWithoutWriting()
		{
			using var data = new DataDirectory();
			var sourcePath = Path.Combine(data.Root, "Sources/sources.json");
			var sources = JsonNode.Parse(File.ReadAllText(sourcePath))!.AsArray();
			foreach (var source in sources)
				source!["LicenseUrl"] = "https://example.com/reviewed-reuse-terms";
			var unresolved = sources[0]!["Id"]!.GetValue<string>();
			sources[0]!.AsObject().Remove("LicenseUrl");
			File.WriteAllText(sourcePath, sources.ToJsonString());
			var before = data.Snapshot();
			var polisher = new GeographyDataPolisher();
			await polisher.ValidateAsync(data.Root);
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => polisher.ValidateReleaseAsync(data.Root));
			Assert.Contains(unresolved, error.Message);
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task ReleaseValidation_ShouldAcceptRecordedReuseTermsWithoutWriting()
		{
			using var data = new DataDirectory();
			var sourcePath = Path.Combine(data.Root, "Sources/sources.json");
			var sources = JsonNode.Parse(File.ReadAllText(sourcePath))!.AsArray();
			foreach (var source in sources)
				source!["LicenseUrl"] = "https://example.com/reviewed-reuse-terms";
			File.WriteAllText(sourcePath, sources.ToJsonString());
			var before = data.Snapshot();
			Assert.Equal(data.Expected, await new GeographyDataPolisher().ValidateReleaseAsync(data.Root));
			Assert.Equal(before, data.Snapshot());
		}

		[Fact]
		public async Task Polish_ShouldRejectDuplicatesInsteadOfSilentlyDroppingEvidence()
		{
			using var data = new DataDirectory();
			data.Edit(Italy, node => node["SourceIds"]!.AsArray().Add(node["SourceIds"]![0]!.DeepClone()));
			var before = data.Snapshot();
			Assert.Contains("Duplicate", (await Assert.ThrowsAsync<InvalidDataException>(
				() => new GeographyDataPolisher().PolishAsync(data.Root))).Message);
			Assert.Equal(before, data.Snapshot());
		}

		private sealed class DataDirectory : IDisposable
		{
			internal GeographyPolishResult Expected => new(new GeographyService().Countries.Count, new GeographyService().Subdivisions.Count, new GeographyService().Cities.Count);

				internal string Root { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

			internal DataDirectory()
			{
				var assembly = typeof(GeographyService).Assembly;
				foreach (var resource in assembly.GetManifestResourceNames())
				{
					var normalized = resource.Replace('\\', '/');
					if (!normalized.StartsWith("Data/", StringComparison.Ordinal)) continue;
					var path = Path.Combine(Root, normalized["Data/".Length..]);
					Directory.CreateDirectory(Path.GetDirectoryName(path)!);
					using var stream = assembly.GetManifestResourceStream(resource)!;
					using var reader = new StreamReader(stream);
					File.WriteAllText(path, reader.ReadToEnd());
				}
				File.WriteAllText(Path.Combine(Root, "SOURCES.md"), "unchanged");
			}

			internal void Edit(string relative, Action<JsonNode> edit)
			{
				var path = Path.Combine(Root, relative);
				var node = JsonNode.Parse(File.ReadAllText(path))!;
				edit(node);
				File.WriteAllText(path, node.ToJsonString());
			}

			internal string[] Snapshot()
				=> Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
					.Select(path => Path.GetRelativePath(Root, path) + "\n" + File.ReadAllText(path)).ToArray();

			public void Dispose() => Directory.Delete(Root, recursive: true);
		}
	}
}
