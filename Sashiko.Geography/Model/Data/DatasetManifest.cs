using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model.Data
{
	internal sealed record DatasetManifest
	{
		[Required]
		public required int SchemaVersion { get; init; }
	}
}
