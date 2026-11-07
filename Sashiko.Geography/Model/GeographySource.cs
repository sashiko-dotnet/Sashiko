using System.Text.Json.Serialization;
using Sashiko.Validation.Schema.Attributes;

namespace Sashiko.Geography.Model
{
	public sealed record GeographySource
	{
		[Required]
		public string Id { get; }

		[Required]
		public string Title { get; }

		[Required]
		public string Url { get; }

		[Required]
		public string Attribution { get; }

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? LicenseUrl { get; }

		[JsonConstructor]
		internal GeographySource(string id, string title, string url, string attribution, string? licenseUrl = null)
		{
			Id = id;
			Title = title;
			Url = url;
			Attribution = attribution;
			LicenseUrl = licenseUrl;
		}
	}
}
