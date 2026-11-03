using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Data
{
	internal sealed record CountryData
	{
		[Required]
		public required string Name { get; init; }

		[Required]
		public required string IsoAlpha2 { get; init; }

		[Required]
		public required string IsoAlpha3 { get; init; }

		[Required]
		public required string IsoNumeric { get; init; }

		[Required]
		public required string M49RegionCode { get; init; }

		[Required]
		public required IReadOnlyList<string> CapitalCityIds { get; init; }

		[Required]
		public required IReadOnlyList<string> OfficialLanguageCodes { get; init; }

		[Required]
		public required IReadOnlyList<string> SourceIds { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationData? Population { get; init; }
	}
}
