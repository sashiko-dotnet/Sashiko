using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record City
	{
		[Required]
		public string Id { get; }

		[Required]
		public string Name { get; }

		[Required]
		public string CountryId { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? RegionId { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Coordinate? Coordinate { get; }

		[JsonConstructor]
		internal City(string id, string name, string countryId, IReadOnlyList<string> sourceIds, string? regionId = null, Coordinate? coordinate = null)
		{
			Id = id;
			Name = name;
			CountryId = countryId;
			SourceIds = Array.AsReadOnly(sourceIds.ToArray());
			RegionId = regionId;
			Coordinate = coordinate;
		}
	}
}
