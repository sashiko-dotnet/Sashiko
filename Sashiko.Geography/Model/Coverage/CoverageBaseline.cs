using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Coverage
{
	internal sealed record CoverageBaseline
	{
		[Required]
		public required IReadOnlyList<WorldRegionCoverageFloor> WorldRegions { get; init; }

		[Required]
		public required IReadOnlyList<CountryCoverageFloor> Countries { get; init; }
	}
}
