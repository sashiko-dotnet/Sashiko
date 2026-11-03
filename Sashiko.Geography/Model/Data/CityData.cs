using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Data
{
	internal sealed record CityData
	{
		[Required]
		public required string Id { get; init; }

		[Required]
		public required string Name { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? SubdivisionId { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Coordinate? Location { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? UnLocode { get; init; }

		[Required]
		public required IReadOnlyList<string> SourceIds { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationData? Population { get; init; }
	}
}
