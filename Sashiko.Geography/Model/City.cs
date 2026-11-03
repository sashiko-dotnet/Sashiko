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

		[JsonConstructor]
		internal City(string id, string name, string countryCode, string? subdivisionId, Coordinate? coordinate, bool isCapital, string? unLocode, IReadOnlyList<string> sourceIds, PopulationSnapshot? population)
		{
			Id = id;
			Name = name;
			CountryCode = countryCode;
			SubdivisionId = subdivisionId;
			Coordinate = coordinate;
			IsCapital = isCapital;
			UnLocode = unLocode;
			SourceIds = sourceIds.ToList().AsReadOnly();
			Population = population;
		}
	}
}
