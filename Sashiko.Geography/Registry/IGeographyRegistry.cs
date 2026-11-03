using Sashiko.Geography.Model;
using Sashiko.Geography.Model.Coverage;

namespace Sashiko.Geography.Registry
{
	internal interface IGeographyRegistry
	{
		IReadOnlyList<WorldRegion> WorldRegions { get; }
		IReadOnlyList<GeographySource> DataSources { get; }
		IReadOnlyList<Country> Countries { get; }
		IReadOnlyList<Subdivision> Subdivisions { get; }
		IReadOnlyList<City> Cities { get; }
		CoverageBaseline CoverageBaseline { get; }
		IReadOnlyDictionary<string, CountryCoverage> Coverage { get; }
	}
}
