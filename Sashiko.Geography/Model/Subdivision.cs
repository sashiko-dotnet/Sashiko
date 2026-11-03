using System.Text.Json.Serialization;
using Sashiko.Geography.Model.Data;
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

		internal Subdivision(SubdivisionData data, string countryCode)
		{
			Id = data.Id;
			CountryCode = countryCode;
			Name = data.Name;
			Type = data.Type;
			ParentId = data.ParentId;
			Iso3166_2 = data.Iso3166_2;
			SourceIds = data.SourceIds.ToList().AsReadOnly();
			Population = PopulationSnapshot.FromData(data.Population);
		}
	}
}
