using Sashiko.Core.Json;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Registry;

namespace Sashiko.Maintenance.Libraries.Geography
{
	internal sealed class GeographyDataPolisher : IGeographyDataPolisher
	{
		public async Task<GeographyPolishResult> PolishAsync(string dataDirectory)
		{
			// Validate the entire proposed dataset before rewriting any entity.
			var registry = await ReadAsync(dataDirectory, polish: true);
			foreach (var (relative, document) in registry.Documents)
				JsonFileWriter.Write(Path.Combine(dataDirectory, relative), document, JsonWriteOptions.Indented);
			return Result(registry);
		}

		internal async Task<GeographyPolishResult> ValidateAsync(string dataDirectory)
			=> Result(await ReadAsync(dataDirectory, polish: false));

		internal async Task<GeographyPolishResult> ValidateReleaseAsync(string dataDirectory)
		{
			var registry = await ReadAsync(dataDirectory, polish: false);
			var unresolved = registry.DataSources.Where(source => source.LicenseUrl is null).Select(source => source.Id).ToArray();
			if (unresolved.Length > 0)
				throw new InvalidDataException($"Document source reuse terms before release: {string.Join(", ", unresolved)}.");
			return Result(registry);
		}

		private static GeographyPolishResult Result(GeographyRegistry registry)
			=> new(registry.Countries.Count, registry.Subdivisions.Count, registry.Cities.Count);

		private static async Task<GeographyRegistry> ReadAsync(string dataDirectory, bool polish)
		{
			if (!Directory.Exists(dataDirectory))
				throw new DirectoryNotFoundException($"Geography data directory does not exist: {dataDirectory}");

			var files = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var path in Directory.EnumerateFiles(dataDirectory, "*", SearchOption.AllDirectories)
				.Where(path => Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
				.Order(StringComparer.Ordinal))
			{
				var relative = Path.GetRelativePath(dataDirectory, path).Replace('\\', '/');
				files.Add(relative, await File.ReadAllTextAsync(path));
			}

			return new GeographyRegistry(files, polish);
		}
	}
}
