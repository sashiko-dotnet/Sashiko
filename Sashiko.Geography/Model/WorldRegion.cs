using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record WorldRegion
	{
		[Required]
		public string Id { get; }

		[Required]
		public string Name { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[JsonConstructor]
		internal WorldRegion(string id, string name, IReadOnlyList<string> sourceIds)
		{
			Id = id;
			Name = name;
			SourceIds = Array.AsReadOnly(sourceIds.ToArray());
		}
	}
}
