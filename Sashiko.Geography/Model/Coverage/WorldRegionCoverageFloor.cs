using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Coverage
{
	internal sealed record WorldRegionCoverageFloor
	{
		[Required]
		public required string M49Code { get; init; }

		[Required]
		public required int MinimumSupportedCountryCount { get; init; }
	}
}
