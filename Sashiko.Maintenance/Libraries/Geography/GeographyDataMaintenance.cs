using Sashiko.Geography.Api;
using Sashiko.Maintenance.Shared;

namespace Sashiko.Maintenance.Libraries.Geography
{
	internal static class GeographyDataMaintenance
	{
		internal static async Task ValidateReleaseAsync()
		{
			var root = AssemblyProjectLocator.FindProjectRoot("Sashiko.Geography", typeof(GeographyService));
			await new GeographyDataPolisher().ValidateReleaseAsync(Path.Combine(root, "Data"));
			Console.WriteLine("Geography release data and source documentation validated.");
		}

		internal static async Task ValidateEmbeddedGeographyAsync()
		{
			var root = AssemblyProjectLocator.FindProjectRoot("Sashiko.Geography", typeof(GeographyService));
			var result = await new GeographyDataPolisher().ValidateAsync(Path.Combine(root, "Data"));
			Console.WriteLine(
				$"Validated {result.CountriesProcessed} countries, {result.SubdivisionsProcessed} subdivisions, " +
				$"and {result.CitiesProcessed} municipalities.");
		}

		internal static async Task PolishEmbeddedGeographyAsync()
		{
			var result = await PolishEmbeddedGeographyAsync(
				new GeographyDataPolisher(),
				AssemblyProjectLocator.FindProjectRoot);

			Console.WriteLine(
				$"Polished {result.CountriesProcessed} country bundle(s), " +
				$"{result.SubdivisionsProcessed} subdivision(s), and " +
				$"{result.CitiesProcessed} city record(s).");
		}

		internal static Task<GeographyPolishResult> PolishEmbeddedGeographyAsync(
			IGeographyDataPolisher polisher,
			Func<string, Type, string> projectLocator)
		{
			var geographyRoot = projectLocator(
				"Sashiko.Geography",
				typeof(GeographyService));

			return polisher.PolishAsync(Path.Combine(geographyRoot, "Data"));
		}
	}
}
