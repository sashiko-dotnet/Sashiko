using System.Text.Json;
using Sashiko.Validation.Schema.Comparison;
using Sashiko.Validation.Schema.Inspectors.CSharp;
using Sashiko.Validation.Schema.Inspectors.Json;
using Sashiko.Validation.Schema.Model;

namespace Sashiko.Validation.Validators.Json
{
	public sealed class JsonSchemaValidator : ISchemaValidator
	{
		public void Validate<T>(object input, ValidationContext? context = null)
		{
			context ??= new ValidationContext();

			var options = GetJsonOptions(context);
			using var document = input is string json
				? JsonDocument.Parse(json, new JsonDocumentOptions
				{
					AllowTrailingCommas = options.AllowTrailingCommas,
					CommentHandling = options.ReadCommentHandling,
					MaxDepth = options.MaxDepth,
					AllowDuplicateProperties = options.AllowDuplicateProperties
				})
				: null;
			var root = ToJsonElement(input, document);
			var comparer = context.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			if (!options.AllowDuplicateProperties)
				EnsureUniqueProperties(root, comparer);
			var expected = CSharpSchemaInspector.GetSchema(typeof(T));

			if (expected.Kind == SchemaNodeKind.Array)
			{
				ValidateArray(expected, root, context, comparer);
				return;
			}

			var actual = JsonSchemaInspector.GetSchema(root, name: null, comparer);
			var diff = SchemaComparer.Compare(expected, actual, context.IgnoreCase);

			if (!diff.IsMatch)
				ThrowSchemaError(context.Source, diff);
		}

		private static void ValidateArray(SchemaNode expected, JsonElement root, ValidationContext context, StringComparer comparer)
		{
			if (root.ValueKind != JsonValueKind.Array)
				throw new ValidationException("Expected JSON array.");

			int index = 0;
			foreach (var item in root.EnumerateArray())
			{
				var actualElement = JsonSchemaInspector.GetSchema(item, name: null, comparer);
				var diff = SchemaComparer.Compare(expected.Element!, actualElement, context.IgnoreCase);

				if (!diff.IsMatch)
					ThrowSchemaErrorForArrayElement(context.Source, diff, index);

				index++;
			}
		}

		private static JsonSerializerOptions GetJsonOptions(ValidationContext context)
		{
			return context.Metadata?.TryGetValue("JsonOptions", out var value) == true && value is JsonSerializerOptions options
				? options
				: JsonSerializerOptions.Default;
		}

		private static void EnsureUniqueProperties(JsonElement element, StringComparer comparer)
		{
			if (element.ValueKind == JsonValueKind.Object)
			{
				var names = new HashSet<string>(comparer);
				foreach (var property in element.EnumerateObject())
				{
					if (!names.Add(property.Name))
					{
						throw new JsonException($"Duplicate JSON property '{property.Name}'.");
					}

					EnsureUniqueProperties(property.Value, comparer);
				}
			}
			else if (element.ValueKind == JsonValueKind.Array)
			{
				foreach (var item in element.EnumerateArray())
					EnsureUniqueProperties(item, comparer);
			}
		}

		private static JsonElement ToJsonElement(object input, JsonDocument? document)
		{
			return input switch
			{
				JsonElement el => el,
				string => document!.RootElement,
				_ => throw new ValidationException(
					$"Unsupported input type '{input.GetType().FullName}' for JsonSchemaValidator. " +
					"Expected string or JsonElement.")
			};
		}

		private static void ThrowSchemaError(string? source, SchemaDiff diff)
		{
			var location = string.IsNullOrEmpty(source) ? "input" : $"'{source}'";

			var message =
				$"Schema mismatch in {location}.\n" +
				(diff.Missing.Count > 0
					? $"Missing required fields:\n  {string.Join("\n  ", diff.Missing)}\n"
					: "") +
				(diff.Extra.Count > 0
					? $"Unexpected fields:\n  {string.Join("\n  ", diff.Extra)}\n"
					: "");

			throw new ValidationException(message);
		}

		private static void ThrowSchemaErrorForArrayElement(string? source, SchemaDiff diff, int index)
		{
			var location = string.IsNullOrEmpty(source) ? "input" : $"'{source}'";

			var message =
				$"Schema mismatch in {location} for array element at index {index}.\n" +
				(diff.Missing.Count > 0
					? $"Missing required fields:\n  {string.Join("\n  ", diff.Missing)}\n"
					: "") +
				(diff.Extra.Count > 0
					? $"Unexpected fields:\n  {string.Join("\n  ", diff.Extra)}\n"
					: "");

			throw new ValidationException(message);
		}
	}
}
