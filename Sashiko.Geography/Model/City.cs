using System.Text.Json.Serialization;
using Sashiko.Geography.Model.Data;
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
		public string CountryCode { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? SubdivisionId { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public Coordinate? Coordinate { get; }

		[Required]
		public bool IsCapital { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? UnLocode { get; }

		[Required]
		public IReadOnlyList<string> SourceIds { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public PopulationSnapshot? Population { get; }

		internal City(CityData data, string countryCode, bool isCapital)
		{
			Id = data.Id;
			Name = data.Name;
			CountryCode = countryCode;
			SubdivisionId = data.SubdivisionId;
			Coordinate = data.Location;
			IsCapital = isCapital;
			UnLocode = data.UnLocode;
			SourceIds = data.SourceIds.ToList().AsReadOnly();
			Population = PopulationSnapshot.FromData(data.Population);
		}
	}
}
