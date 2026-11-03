using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Data
{
	internal sealed record PopulationData
	{
		[Required]
		public required long Value { get; init; }

		[Required]
		public required string ReferenceDate { get; init; }

		[Required]
		public required string Definition { get; init; }

		[Required]
		public required string BoundaryVersion { get; init; }

		[Required]
		public required string SourceId { get; init; }
	}
}
