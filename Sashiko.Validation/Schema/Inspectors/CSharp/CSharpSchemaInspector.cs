using System.Reflection;
using Sashiko.Validation.Schema.Model;

namespace Sashiko.Validation.Schema.Inspectors.CSharp
{
	public static class CSharpSchemaInspector
	{
		public static SchemaNode GetSchema(Type type)
		{
			var visited = new HashSet<Type>();
			return BuildNode(type, name: null, required: true, visited);
		}

		private static SchemaNode BuildNode(
		Type type,
		string? name,
		bool required,
		HashSet<Type> visited)
		{
			// ------------------------------------------------------------
			// Leaf types → never tracked, never recursive
			// ------------------------------------------------------------
			if (LeafTypeDetector.IsLeaf(type))
			{
				return new SchemaNode(
					name ?? type.Name,
					required,
					SchemaNodeKind.Leaf);
			}

			// ------------------------------------------------------------
			// Collections → also never tracked as recursion roots
			// ------------------------------------------------------------
			if (CollectionTypeDetector.TryGetElementType(type, out var elementType))
			{
				var elementNode = BuildNode(
					elementType,
					name: elementType.Name,
					required: true,
					visited);

				return new SchemaNode(
					name ?? type.Name,
					required,
					SchemaNodeKind.Array,
					fields: null,
					element: elementNode);
			}

			// ------------------------------------------------------------
			// Complex objects → recursion guard applies here
			// ------------------------------------------------------------
			if (visited.Contains(type))
			{
				return new SchemaNode(
					name ?? type.Name,
					required,
					SchemaNodeKind.Object,
					fields: new Dictionary<string, SchemaNode>());
			}

			visited.Add(type);

			// Build object fields...
			var fields = new Dictionary<string, SchemaNode>();

			foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (prop.GetIndexParameters().Length > 0)
					continue;

				if (prop.GetMethod == null)
					continue;

				var propType = prop.PropertyType;

				bool isRequired =
					RequiredPropertyDetector.IsRequired(prop) ||
					(propType.IsValueType && Nullable.GetUnderlyingType(propType) == null);

				var childNode = BuildNode(
					propType,
					name: prop.Name,
					required: isRequired,
					visited);

				fields[prop.Name] = childNode;
			}

			// Only ancestors represent recursion; sibling properties may reuse the same type.
			visited.Remove(type);

			return new SchemaNode(
				name ?? type.Name,
				required,
				SchemaNodeKind.Object,
				fields);
		}
	}
}
