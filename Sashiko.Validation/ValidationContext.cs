namespace Sashiko.Validation
{
	public sealed class ValidationContext
	{
		public string? Source { get; init; }
		public bool IgnoreCase { get; init; }
		/// <summary>
		/// Optional validator-specific metadata. JsonSchemaValidator reads "JsonOptions" when its
		/// value is a System.Text.Json.JsonSerializerOptions instance; other values are ignored.
		/// </summary>
		public IDictionary<string, object>? Metadata { get; init; }
	}
}
