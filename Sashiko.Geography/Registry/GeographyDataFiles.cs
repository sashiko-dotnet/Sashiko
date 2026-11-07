namespace Sashiko.Geography.Registry
{
	internal enum GeographyFileKind { WorldRegions, Countries, Sources, Regions, Cities }

	internal static class GeographyDataFiles
	{
		internal const string WorldRegions = "world-regions.json";
		internal const string Countries = "countries.json";
		internal const string Sources = "sources.json";

		internal static GeographyFileKind Kind(string path)
		{
			if (path == WorldRegions) return GeographyFileKind.WorldRegions;
			if (path == Countries) return GeographyFileKind.Countries;
			if (path == Sources) return GeographyFileKind.Sources;
			var parts = path.Split('/');
			if (parts.Any(part => part.Length == 0 || part is "." or ".."))
				throw new InvalidDataException($"{path}: Unsafe geography collection path.");
			if (parts[0] == "Countries" && parts.Length == 3 && parts[2] == "regions.json")
				return GeographyFileKind.Regions;
			if (parts[0] == "Countries" && parts.Length == 4 && parts[2] == "Cities"
				&& parts[3].EndsWith(".json", StringComparison.Ordinal))
				return GeographyFileKind.Cities;
			throw new InvalidDataException($"{path}: Unexpected geography collection path.");
		}

		internal static IReadOnlyDictionary<string, string> ReadEmbedded()
		{
			var assembly = typeof(GeographyDataFiles).Assembly;
			var files = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var resource in assembly.GetManifestResourceNames())
			{
				var path = resource.Replace('\\', '/');
				if (!path.StartsWith("Data/", StringComparison.Ordinal) || !path.EndsWith(".json", StringComparison.Ordinal))
					continue;
				using var stream = assembly.GetManifestResourceStream(resource)
					?? throw new InvalidOperationException($"Embedded resource '{resource}' was not found.");
				using var reader = new StreamReader(stream);
				files.Add(path["Data/".Length..], reader.ReadToEnd());
			}
			return files;
		}
	}
}
