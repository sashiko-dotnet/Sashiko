using System.Text.Json;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Validation.Tests.Validators.Json
{
	public sealed class JsonSchemaValidatorOptionsTests
	{
		private readonly JsonSchemaValidator _validator = new();

		private sealed class Entry
		{
			public int Id { get; set; }
		}

		private sealed class Rows
		{
			public List<Entry> Items { get; set; } = [];
		}

		private sealed class CaseSensitiveEntry
		{
			public int Id { get; set; }
			public int ID { get; set; }
		}

		private static ValidationContext Context(JsonSerializerOptions options) => new()
		{
			IgnoreCase = options.PropertyNameCaseInsensitive,
			Metadata = new Dictionary<string, object> { ["JsonOptions"] = options }
		};

		[Fact]
		public void Validate_String_UsesParserOptionsFromContext()
		{
			var options = new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			_validator.Validate<Entry>("""{ /* allowed */ "Id": 1, }""", Context(options));

			Assert.False(options.IsReadOnly);
		}

		[Theory]
		[InlineData("""{ "Id": 1, }""")]
		[InlineData("""{ /* rejected */ "Id": 1 }""")]
		public void Validate_DefaultContext_KeepsDefaultParserRules(string json)
		{
			Assert.ThrowsAny<JsonException>(() => _validator.Validate<Entry>(json));
		}

		[Fact]
		public void Validate_String_EnforcesConfiguredDepth()
		{
			var options = new JsonSerializerOptions { MaxDepth = 1 };

			Assert.ThrowsAny<JsonException>(() =>
				_validator.Validate<Rows>("""{ "Items": [{ "Id": 1 }] }""", Context(options)));
		}

		[Fact]
		public void Validate_String_AllowsConfiguredDepthAboveRuntimeDefaultBeforeSchemaComparison()
		{
			var json = new string('[', 70) + "0" + new string(']', 70);
			var options = new JsonSerializerOptions { MaxDepth = 128 };

			// The syntax is permitted at this depth; the nested arrays then fail the model's shape.
			Assert.Throws<ValidationException>(() => _validator.Validate<List<int>>(json, Context(options)));
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public void Validate_ExplicitDuplicateRejection_AppliesToStringsAndElements(bool elementInput, bool ignoreCase)
		{
			var json = ignoreCase ? """{ "Id": 1, "id": 2 }""" : """{ "Id": 1, "Id": 2 }""";
			using var document = JsonDocument.Parse(json);
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = ignoreCase
			};
			object input = elementInput ? document.RootElement : json;

			Assert.ThrowsAny<JsonException>(() => _validator.Validate<Entry>(input, Context(options)));
			Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
		}

		[Theory]
		[InlineData("""{ "Id": 1, "Extra": { "Name": 1, "name": 2 } }""")]
		[InlineData("""[{ "Id": 1 }, { "Id": 2, "id": 3 }]""")]
		[InlineData("""{ "Items": [{ "Id": 1 }, { "Id": 2, "id": 3 }] }""")]
		public void Validate_PreparsedElement_RejectsDuplicatesThroughoutInputBeforeSchemaInspection(string json)
		{
			using var document = JsonDocument.Parse(json);
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = true
			};

			Assert.ThrowsAny<JsonException>(() => _validator.Validate<Entry>(document.RootElement, Context(options)));
		}

		[Fact]
		public void Validate_RejectDuplicates_DoesNotTreatSeparateObjectsAsDuplicates()
		{
			var options = new JsonSerializerOptions { AllowDuplicateProperties = false };

			_validator.Validate<Rows>("""{ "Items": [{ "Id": 1 }, { "Id": 2 }] }""", Context(options));
		}

		[Fact]
		public void Validate_OrdinalDuplicatePolicy_PermitsDistinctCaseSensitiveNames()
		{
			var options = new JsonSerializerOptions { AllowDuplicateProperties = false };

			_validator.Validate<CaseSensitiveEntry>("""{ "Id": 1, "ID": 2 }""", Context(options));
		}

		[Fact]
		public void Validate_DefaultDuplicatePolicy_KeepsLastExactPropertyShape()
		{
			_validator.Validate<Entry>("""{ "Id": {}, "Id": 1 }""");
		}

		[Fact]
		public void Validate_AllowedCaseInsensitiveDuplicates_KeepLastShapeInInputOrder()
		{
			var context = new ValidationContext { IgnoreCase = true };

			_validator.Validate<Entry>("""{ "Id": 1, "id": {}, "Id": 2 }""", context);
			Assert.Throws<ValidationException>(() =>
				_validator.Validate<Entry>("""{ "Id": 1, "id": 2, "Id": {} }""", context));
		}

		[Fact]
		public void Validate_AllowedCaseInsensitiveDuplicates_AppliesToNestedArrays()
		{
			var context = new ValidationContext { IgnoreCase = true };

			_validator.Validate<Rows>("""{ "Items": [{ "Id": 1, "id": 2 }] }""", context);
		}

		[Fact]
		public void Validate_UnknownMetadata_RemainsIgnored()
		{
			var context = new ValidationContext
			{
				Metadata = new Dictionary<string, object> { ["JsonOptions"] = "not serializer options" }
			};

			_validator.Validate<Entry>("""{ "Id": 1 }""", context);
		}

		[Fact]
		public void Validate_PreparsedElement_RemainsUsableAfterSuccessfulValidation()
		{
			using var document = JsonDocument.Parse("""{ "Id": 1 }""");

			_validator.Validate<Entry>(document.RootElement);

			Assert.Equal(1, document.RootElement.GetProperty("Id").GetInt32());
		}
	}
}
