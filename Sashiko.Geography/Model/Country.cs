using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Country
	{
		[Required]
		public string Id { get; }

		[Required]
		public string Name { get; }

		[Required]
		public string WorldRegionId { get; }

		[Required]
		public IReadOnlyList<string> CapitalCityIds { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? IsoAlpha2 { get; }

		[Optional]
		public IReadOnlyList<string> OfficialLanguageCodes { get; }

		[JsonConstructor]
		internal Country(string id, string name, string worldRegionId, IReadOnlyList<string> capitalCityIds, IReadOnlyList<string> sourceIds, string? isoAlpha2 = null, IReadOnlyList<string>? officialLanguageCodes = null)
		{
			Id = id;
			Name = name;
			WorldRegionId = worldRegionId;
			CapitalCityIds = Array.AsReadOnly(capitalCityIds.ToArray());
			SourceIds = Array.AsReadOnly(sourceIds.ToArray());
			IsoAlpha2 = isoAlpha2;
			OfficialLanguageCodes = Array.AsReadOnly((officialLanguageCodes ?? []).ToArray());
		}
	}
}
