using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Subdivision
	{
		[Required]
		public string Id { get; }

		[Required]
		public string CountryCode { get; }

		[Required]
		public string Name { get; }

		[Required]
		public string Type { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? ParentId { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Iso3166_2 { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationSnapshot? Population { get; }

		[JsonConstructor]
		internal Subdivision(string id, string countryCode, string name, string type, string? parentId, string? iso3166_2, IReadOnlyList<string> sourceIds, PopulationSnapshot? population)
		{
			Id = id;
			CountryCode = countryCode;
			Name = name;
			Type = type;
			ParentId = parentId;
			Iso3166_2 = iso3166_2;
			SourceIds = sourceIds.ToList().AsReadOnly();
			Population = population;
		}
	}
}
