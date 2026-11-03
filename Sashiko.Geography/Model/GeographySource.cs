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

		[Optional]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? LicenseUrl { get; }

		[Required]
		public string Attribution { get; }

		[JsonConstructor]
		internal GeographySource(string id, string title, string url, string? licenseUrl, string attribution)
		{
			Id = id;
			Title = title;
			Url = url;
			LicenseUrl = licenseUrl;
			Attribution = attribution;
		}
	}
}
