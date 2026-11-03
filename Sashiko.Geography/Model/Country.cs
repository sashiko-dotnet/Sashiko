using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Country
	{
		[Required]
		public string Name { get; }

		[Required]
		public string IsoAlpha2 { get; }

		[Required]
		public string IsoAlpha3 { get; }

		[Required]
		public string IsoNumeric { get; }

		[Required]
		public string M49RegionCode { get; }

		[Required]
		public IReadOnlyList<string> CapitalCityIds { get; }

		[Required]
		public IReadOnlyList<string> OfficialLanguageCodes { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationSnapshot? Population { get; }

		[JsonConstructor]
		internal Country(string name, string isoAlpha2, string isoAlpha3, string isoNumeric, string m49RegionCode, IReadOnlyList<string> capitalCityIds, IReadOnlyList<string> officialLanguageCodes, IReadOnlyList<string> sourceIds, PopulationSnapshot? population)
		{
			Name = name;
			IsoAlpha2 = isoAlpha2;
			IsoAlpha3 = isoAlpha3;
			IsoNumeric = isoNumeric;
			M49RegionCode = m49RegionCode;
			CapitalCityIds = capitalCityIds.ToList().AsReadOnly();
			OfficialLanguageCodes = officialLanguageCodes.ToList().AsReadOnly();
			SourceIds = sourceIds.ToList().AsReadOnly();
			Population = population;
		}
	}
}
