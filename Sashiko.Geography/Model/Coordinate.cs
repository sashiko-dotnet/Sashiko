using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record Coordinate
	{
		[Required]
		public double Latitude { get; }

		[Required]
		public double Longitude { get; }

		[JsonConstructor]
		internal Coordinate(double latitude, double longitude)
		{
			Latitude = latitude;
			Longitude = longitude;
		}
	}
}
