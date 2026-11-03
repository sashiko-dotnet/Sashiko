using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Coordinate
	{
		[Required]
		public decimal Latitude { get; }

		[Required]
		public decimal Longitude { get; }

		[JsonConstructor]
		internal Coordinate(decimal latitude, decimal longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}
	}
}
