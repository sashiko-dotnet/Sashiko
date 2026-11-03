using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Data
{
	internal sealed record SubdivisionData
	{
		[Required]
		public required string Id { get; init; }

		[Required]
		public required string Name { get; init; }

		[Required]
		public required string Type { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? ParentId { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Iso3166_2 { get; init; }

		[Required]
		public required IReadOnlyList<string> SourceIds { get; init; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationData? Population { get; init; }
	}
}
