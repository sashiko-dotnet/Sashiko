using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Coverage
{
	internal sealed record CountryCoverageFloor
	{
		[Required]
		public required string CountryCode { get; init; }

		[Required]
		public required int MinimumSubdivisionCount { get; init; }

		[Required]
		public required int MinimumCityCount { get; init; }

		[Required]
		public required decimal MinimumSubdivisionPopulationCoverage { get; init; }

		[Required]
		public required decimal MinimumCityPopulationCoverage { get; init; }
	}
}
