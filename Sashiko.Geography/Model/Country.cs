using System.Text.Json.Serialization;
using Sashiko.Geography.Model.Data;
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

		internal Country(CountryData data)
		{
			Name = data.Name;
			IsoAlpha2 = data.IsoAlpha2;
			IsoAlpha3 = data.IsoAlpha3;
			IsoNumeric = data.IsoNumeric;
			M49RegionCode = data.M49RegionCode;
			CapitalCityIds = data.CapitalCityIds.ToList().AsReadOnly();
			OfficialLanguageCodes = data.OfficialLanguageCodes.ToList().AsReadOnly();
			SourceIds = data.SourceIds.ToList().AsReadOnly();
			Population = PopulationSnapshot.FromData(data.Population);
		}
	}
}
