using System.Text.Json;
using Sashiko.Core.Json;
using Sashiko.Core.Json.Options;
using Sashiko.Geography.Api;
using Sashiko.Maintenance.Shared;

namespace Sashiko.Maintenance.Libraries.Geography
{
	internal static class GeographyDataMaintenance
	{
		internal static Task RunAsync(string command)
		{
			var root = AssemblyProjectLocator.FindProjectRoot("Sashiko.Geography", typeof(GeographyService));
			return RunAsync(Path.Combine(root, "Data"), command);
		}

		internal static async Task RunAsync(string directory, string command)
		{
			if (command is not ("polish" or "validate" or "check" or "validate-release"))
				throw new ArgumentException($"Unknown geography command '{command}'.", nameof(command));
			if (!Directory.Exists(directory))
				throw new DirectoryNotFoundException($"Geography directory was not found: {directory}");
			var files = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
				.Where(path => Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
				.Order(StringComparer.Ordinal))
				files.Add(Path.GetRelativePath(directory, path).Replace('\\', '/'), await File.ReadAllTextAsync(path));
			var data = GeographyDataValidator.Read(files, polish: command is "polish" or "check" or "validate-release");
			var changed = data.Documents.Where(pair => files[pair.Key] != Canonical(pair.Value)).Select(pair => pair.Key).ToArray();
			if (command is "check" or "validate-release" && changed.Length > 0)
				throw new InvalidDataException($"Run geography polish before release: {string.Join(", ", changed)}.");
			if (command == "validate-release")
			{
				var pending = data.Sources.Where(source => source.LicenseUrl is null).Select(source => source.Id).ToArray();
				if (pending.Length > 0)
					throw new InvalidDataException($"Document source reuse review before release: {string.Join(", ", pending)}.");
			}
			// No file is written until all collections and relationships have passed.
			if (command == "polish")
				foreach (var relative in changed)
					JsonFileWriter.Write(Path.Combine(directory, relative), data.Documents[relative], JsonWriteOptions.Indented);
			Console.WriteLine($"Geography {command}: {data.Countries.Count} countries, {data.Regions.Count} regions, {data.Cities.Count} cities; {changed.Length} file(s) needed polishing.");
		}

		private static string Canonical(object document)
			=> JsonSerializer.Serialize(document, document.GetType(), JsonWriteOptions.Indented);
	}
}
