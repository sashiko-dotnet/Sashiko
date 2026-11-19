using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Sashiko.Validation.Validators.Json
{
	/// <summary>Loads a bounded UTF-8 JSON value using a System.Text.Json contract.</summary>
	public static class JsonModelLoader
	{
		/// <summary>Deserializes the entire input using the supplied contract and rejects a null result.</summary>
		/// <param name="utf8Json">One UTF-8 JSON value without a byte order mark.</param>
		/// <param name="contract">The serializer metadata and its input policies.</param>
		/// <param name="maxInputBytes">The inclusive byte limit for the supplied input.</param>
		/// <exception cref="ArgumentNullException"><paramref name="contract"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxInputBytes"/> is negative.</exception>
		/// <exception cref="JsonException">The input exceeds the limit, violates the JSON contract, or produces a null result.</exception>
		public static T Load<T>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<T> contract, int maxInputBytes)
		{
			ArgumentNullException.ThrowIfNull(contract);
			ArgumentOutOfRangeException.ThrowIfNegative(maxInputBytes);
			if (utf8Json.Length > maxInputBytes)
				throw new JsonException("JSON input exceeds the configured byte limit.");

			if (!contract.Options.AllowDuplicateProperties)
				EnsureUniquePropertyNames(utf8Json, contract.Options);

			return JsonSerializer.Deserialize(utf8Json, contract)
				?? throw new JsonException("The JSON model must not be null.");
		}

		private static void EnsureUniquePropertyNames(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options)
		{
			var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
			{
				AllowTrailingCommas = options.AllowTrailingCommas,
				CommentHandling = options.ReadCommentHandling,
				MaxDepth = options.MaxDepth
			});
			var comparer = options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			var objectScopes = new Stack<HashSet<string>>();

			while (reader.Read())
			{
				switch (reader.TokenType)
				{
					case JsonTokenType.StartObject:
						objectScopes.Push(new HashSet<string>(comparer));
						break;
					case JsonTokenType.EndObject:
						objectScopes.Pop();
						break;
					case JsonTokenType.PropertyName:
						if (!objectScopes.Peek().Add(ReadPropertyName(ref reader)))
							throw new JsonException("Duplicate JSON property name.");
						break;
				}
			}
		}

		private static string ReadPropertyName(ref Utf8JsonReader reader)
		{
			try
			{
				return reader.GetString()!;
			}
			catch (InvalidOperationException error)
			{
				throw new JsonException("Invalid JSON property name.", error);
			}
		}
	}
}
