using System.Text.Json;
using System.Text.Json.Nodes;
using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Tests.Data
{
	internal static class GeographyDataFixture
	{
		internal const string Italy = "Countries/IT/country.json";
		internal const string Lazio = "Countries/IT/Subdivisions/IT-62.json";
		internal const string Rome = "Countries/IT/Cities/IT-ISTAT-058091.json";

		internal static Dictionary<string, string> Files() =>
			GeographyRegistry.ReadEmbeddedFiles().ToDictionary(pair => pair.Key, pair => pair.Value);

		internal static Dictionary<string, string> Minimal()
		{
			var files = new Dictionary<string, string>
			{
				[GeographyRegistry.ManifestPath] = """{"SchemaVersion":1}""",
				[GeographyRegistry.RegionsPath] = """[{"Name":"Europe","M49Code":"150"}]""",
				[GeographyRegistry.SourcesPath] = """[{"Id":"TEST","Title":"Synthetic fixture","Url":"https://example.com/data","LicenseUrl":"https://creativecommons.org/publicdomain/zero/1.0/","Attribution":"Synthetic test data."}]""",
				[GeographyRegistry.CoveragePath] = """
				{"WorldRegions":[{"M49Code":"150","MinimumSupportedCountryCount":1}],
				"Countries":[{"CountryCode":"IT","MinimumSubdivisionCount":0,"MinimumCityCount":0,
				"MinimumSubdivisionPopulationCoverage":0,"MinimumCityPopulationCoverage":0}]}
				""",
				[Italy] = """
				{"Name":"Italy","IsoAlpha2":"IT","IsoAlpha3":"ITA","IsoNumeric":"380","M49RegionCode":"150",
				"CapitalCityIds":[],"OfficialLanguageCodes":["ita"],"SourceIds":["TEST"]}
				"""
			};
			Update(files, Italy, node => node["Population"] = Population(1000));
			return files;
		}

		internal static JsonObject Population(long value) => new()
		{
			["Value"] = value, ["ReferenceDate"] = "2024-01-01", ["Definition"] = "usual-residents",
			["BoundaryVersion"] = "TEST-2024", ["SourceId"] = "TEST"
		};

		internal static void AddSubdivision(Dictionary<string, string> files, string id, string? parent = null, long? population = null)
		{
			var node = new JsonObject { ["Id"] = id, ["Name"] = id, ["Type"] = "District", ["SourceIds"] = new JsonArray("TEST") };
			if (parent is not null) node["ParentId"] = parent;
			if (population is not null) node["Population"] = Population(population.Value);
			files[$"Countries/IT/Subdivisions/{id}.json"] = node.ToJsonString();
		}

		internal static void AddCity(Dictionary<string, string> files, string id, string? parent = null, long? population = null)
		{
			var node = new JsonObject { ["Id"] = id, ["Name"] = "Example", ["SourceIds"] = new JsonArray("TEST") };
			if (parent is not null) node["SubdivisionId"] = parent;
			if (population is not null) node["Population"] = Population(population.Value);
			files[$"Countries/IT/Cities/{id}.json"] = node.ToJsonString();
		}

		internal static void Update(Dictionary<string, string> files, string path, Action<JsonNode> edit)
		{
			var node = JsonNode.Parse(files[path])!;
			edit(node);
			files[path] = node.ToJsonString();
		}

		internal static Dictionary<string, string> Nested()
		{
			var files = Minimal();
			AddSubdivision(files, "IT-ROOT", population: 800);
			AddSubdivision(files, "IT-MIDDLE", "IT-ROOT");
			AddSubdivision(files, "IT-NATIONAL-001", "IT-MIDDLE", 600);
			AddCity(files, "IT-TEST-001", "IT-NATIONAL-001", 100);
			return files;
		}
	}
}
