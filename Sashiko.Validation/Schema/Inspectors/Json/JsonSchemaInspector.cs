using System.Text.Json;
using Sashiko.Validation.Schema.Model;

namespace Sashiko.Validation.Schema.Inspectors.Json
{
	public static class JsonSchemaInspector
	{
		public static SchemaNode GetSchema(JsonElement element, string? name = null)
			=> GetSchema(element, name, StringComparer.Ordinal);

		internal static SchemaNode GetSchema(JsonElement element, string? name, StringComparer comparer)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.Object:
					return BuildObjectNode(element, name, comparer);

				case JsonValueKind.Array:
					return BuildArrayNode(element, name, comparer);

				default:
					return new SchemaNode(
						name ?? "",
						required: true,
						kind: SchemaNodeKind.Leaf
					);
			}
		}

		private static SchemaNode BuildObjectNode(JsonElement element, string? name, StringComparer comparer)
		{
			var fields = new Dictionary<string, SchemaNode>(comparer);

			foreach (var prop in element.EnumerateObject())
			{
				fields[prop.Name] = GetSchema(prop.Value, prop.Name, comparer);
			}

			return new SchemaNode(
				name ?? "",
				required: true,
				kind: SchemaNodeKind.Object,
				fields: fields
			);
		}

		private static SchemaNode BuildArrayNode(JsonElement element, string? name, StringComparer comparer)
		{
			// Empty array → element schema unknown → treat as leaf
			if (!element.EnumerateArray().Any())
			{
				return new SchemaNode(
					name ?? "",
					required: true,
					kind: SchemaNodeKind.Array,
					element: new SchemaNode("[]", required: true, kind: SchemaNodeKind.Leaf)
				);
			}

			// Infer element schema from first element
			var first = element.EnumerateArray().First();
			var elementSchema = GetSchema(first, name: null, comparer);

			return new SchemaNode(
				name ?? "",
				required: true,
				kind: SchemaNodeKind.Array,
				element: elementSchema
			);
		}
	}
}
