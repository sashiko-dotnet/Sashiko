using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Region
	{
		[Required]
		public string Id { get; }

		[Required]
		public string Name { get; }

		[Required]
		public string CountryId { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[JsonConstructor]
		internal Region(string id, string name, string countryId, IReadOnlyList<string> sourceIds)
		{
			Id = id;
			Name = name;
			CountryId = countryId;
			SourceIds = Array.AsReadOnly(sourceIds.ToArray());
		}
	}
}
