using System.Text.Json;
using Sashiko.Validation;

namespace Sashiko.Registries.Json
{
	internal static class JsonRegistryInput
	{
		public static void Validate<T>(
			string json,
			string source,
			Type registryType,
			ISchemaValidator schemaValidator,
			JsonSerializerOptions options)
		{
			try
			{
				using var document = JsonDocument.Parse(json, new JsonDocumentOptions
				{
					AllowTrailingCommas = options.AllowTrailingCommas,
					CommentHandling = options.ReadCommentHandling,
					MaxDepth = options.MaxDepth,
					AllowDuplicateProperties = options.AllowDuplicateProperties
				});

				// Keep supplying the original string and context to custom validators.
				schemaValidator.Validate<T>(json, new ValidationContext
				{
					Source = source,
					IgnoreCase = options.PropertyNameCaseInsensitive,
					Metadata = new Dictionary<string, object>
					{
						["IsEmbedded"] = false,
						["JsonOptions"] = options,
						["RegistryType"] = registryType.FullName!
					}
				});
			}
			catch (JsonException error)
			{
				throw new RegistryLoadException(
					$"Invalid JSON format in '{source}'.", source, registryType, error);
			}
		}
	}
}
