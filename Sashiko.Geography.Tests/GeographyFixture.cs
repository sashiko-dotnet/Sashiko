using System.Text.Json.Nodes;
using Sashiko.Geography.Registry;

namespace Sashiko.Geography.Tests
{
	internal static class GeographyFixture
	{
		internal static Dictionary<string, string> Minimal() => new()
		{
			[GeographyDataFiles.WorldRegions] = """[{"Id":"WORLD","Name":"Example world","SourceIds":["SOURCE"]}]""",
			[GeographyDataFiles.Countries] = """[{"Id":"TEST","Name":"Example country","WorldRegionId":"WORLD","CapitalCityIds":[],"SourceIds":["SOURCE"]}]""",
			[GeographyDataFiles.Sources] = """[{"Id":"SOURCE","Title":"Synthetic fixture","Url":"https://example.com/data","Attribution":"Synthetic test evidence.","LicenseUrl":"https://example.com/reuse"}]"""
		};

		internal static void AddCountry(Dictionary<string, string> files)
		{
			Update(files, GeographyDataFiles.Countries, rows =>
			{
				var other = rows[0]!.DeepClone();
				other["Id"] = "ZZ";
				other["Name"] = "Other country";
				other["CapitalCityIds"] = new JsonArray();
				rows.Add(other);
			});
		}
		internal static void AddRegion(Dictionary<string, string> files, string id = "TEST-NORTH", string country = "TEST")
			=> Append(files, $"Countries/{country}/regions.json", new JsonObject
			{
				["Id"] = id, ["Name"] = id, ["CountryId"] = country, ["SourceIds"] = new JsonArray("SOURCE")
			});
		internal static void AddCity(Dictionary<string, string> files, string id = "TEST-ONE",
			string? region = null, string country = "TEST", string name = "Example")
			=> Append(files, $"Countries/{country}/Cities/{region ?? "country-direct"}.json", new JsonObject
			{
				["Id"] = id, ["Name"] = name, ["CountryId"] = country,
				["SourceIds"] = new JsonArray("SOURCE"), ["RegionId"] = region
			});
		internal static void Update(Dictionary<string, string> files, string path, Action<JsonArray> update)
		{
			var rows = JsonNode.Parse(files[path])!.AsArray();
			update(rows);
			files[path] = rows.ToJsonString();
		}
		internal static Dictionary<string, string> Populated()
		{
			var files = Minimal();
			AddRegion(files);
			AddRegion(files, "TEST-SOUTH");
			AddCity(files, region: "TEST-NORTH");
			AddCity(files, "TEST-TWO", name: "Example");
			Update(files, GeographyDataFiles.Countries, rows => rows[0]!["CapitalCityIds"] = new JsonArray("TEST-ONE", "TEST-TWO"));
			return files;
		}
		private static void Append(Dictionary<string, string> files, string path, JsonObject row)
		{
			if (!files.ContainsKey(path)) files[path] = "[]";
			Update(files, path, rows => rows.Add(row));
		}
	}
}
