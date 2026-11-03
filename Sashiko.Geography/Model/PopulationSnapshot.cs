using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record PopulationSnapshot
	{
		[Required]
		public long Value { get; }

		[Required]
		public DateOnly ReferenceDate { get; }

		[Required]
		public string Definition { get; }

		[Required]
		public string BoundaryVersion { get; }

		[Required]
		public string SourceId { get; }

		[JsonConstructor]
		internal PopulationSnapshot(long value, DateOnly referenceDate, string definition, string boundaryVersion, string sourceId)
		{
			Value = value;
			ReferenceDate = referenceDate;
			Definition = definition;
			BoundaryVersion = boundaryVersion;
			SourceId = sourceId;
		}
	}
}
