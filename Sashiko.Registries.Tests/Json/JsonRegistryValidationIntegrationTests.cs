using System.Text.Json;
using Sashiko.Registries.Json;
using Sashiko.Validation;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Registries.Tests.Json
{
	public sealed class JsonRegistryValidationIntegrationTests
	{
		private const string Source = "registry.json";

		private sealed class Entry
		{
			public int Id { get; set; }
			public Detail? Details { get; set; }
			public List<Detail>? Children { get; set; }
		}

		private sealed class Detail
		{
			public int Value { get; set; }
		}

		private sealed class ThrowingValidator(Exception error) : ISchemaValidator
		{
			public void Validate<T>(object input, ValidationContext? context = null) => throw error;
		}

		private static Entry Load(string json, bool list, JsonSerializerOptions? options = null, ISchemaValidator? validator = null)
		{
			validator ??= new JsonSchemaValidator();
			return list
				? new JsonRegistryListLoader<Entry>(validator, externalOptions: options)
					.LoadExternal($"[{json}]", Source).Single()
				: new JsonRegistryObjectLoader<Entry>(validator, externalOptions: options)
					.LoadExternal(json, Source);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_CustomValidatorJsonFailure_IsWrappedWithOriginalCause(bool list)
		{
			var failure = new JsonException("Synthetic JSON policy failure.");

			var error = Assert.Throws<RegistryLoadException>(() =>
				Load("""{ "Id": 1 }""", list, validator: new ThrowingValidator(failure)));

			Assert.Same(failure, error.InnerException);
			Assert.Equal(Source, error.SourceName);
			Assert.Equal(typeof(Entry), error.RegistryType);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_CustomValidatorNonJsonFailure_IsNotReclassified(bool list)
		{
			var failure = new InvalidOperationException("Synthetic validator failure.");

			var error = Assert.Throws<InvalidOperationException>(() =>
				Load("""{ "Id": 1 }""", list, validator: new ThrowingValidator(failure)));

			Assert.Same(failure, error);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_WithRealValidator_HonorsTrailingCommasAndComments(bool list)
		{
			var options = new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};

			var entry = Load("""{ /* allowed */ "Id": 7, }""", list, options);

			Assert.Equal(7, entry.Id);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_WithRealValidator_HonorsMaxDepth(bool list)
		{
			var options = new JsonSerializerOptions { MaxDepth = list ? 2 : 1 };

			var error = Assert.Throws<RegistryLoadException>(() =>
				Load("""{ "Id": 1, "Details": { "Value": 2 } }""", list, options));

			Assert.IsAssignableFrom<JsonException>(error.InnerException);
			Assert.Equal(Source, error.SourceName);
			Assert.Equal(typeof(Entry), error.RegistryType);
		}

		[Theory]
		[InlineData(false, false)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(true, true)]
		public void LoadExternal_ExplicitDuplicateRejection_PreservesExternalErrorContract(bool list, bool ignoreCase)
		{
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = ignoreCase
			};
			var json = ignoreCase ? """{ "Id": 1, "id": 2 }""" : """{ "Id": 1, "Id": 2 }""";

			var error = Assert.Throws<RegistryLoadException>(() => Load(json, list, options));

			Assert.IsAssignableFrom<JsonException>(error.InnerException);
			Assert.Equal(Source, error.SourceName);
			Assert.Equal(typeof(Entry), error.RegistryType);
		}

		[Theory]
		[InlineData(false, """{ "Id": 1, "Details": { "Value": 2, "value": 3 } }""")]
		[InlineData(true, """{ "Id": 1, "Details": { "Value": 2, "value": 3 } }""")]
		[InlineData(false, """{ "Id": 1, "Children": [{ "Value": 2 }, { "Value": 3, "value": 4 }] }""")]
		[InlineData(true, """{ "Id": 1, "Children": [{ "Value": 2 }, { "Value": 3, "value": 4 }] }""")]
		public void LoadExternal_ExplicitDuplicateRejection_VisitsNestedObjectsAndEveryArrayElement(bool list, string json)
		{
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = true
			};

			var error = Assert.Throws<RegistryLoadException>(() => Load(json, list, options));

			Assert.IsAssignableFrom<JsonException>(error.InnerException);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_DuplicateRejection_IsCaseSensitiveWhenConfigured(bool list)
		{
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = false
			};

			// "id" is an unexpected field, not a duplicate of "Id" under ordinal matching.
			Assert.Throws<ValidationException>(() => Load("""{ "Id": 1, "id": 2 }""", list, options));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_DefaultOptions_PreserveLastValueForRepeatedProperty(bool list)
		{
			Assert.Equal(2, Load("""{ "Id": 1, "Id": 2 }""", list).Id);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_AllowedCaseInsensitiveDuplicates_UseLastValueInJsonOrder(bool list)
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var entry = Load("""{ "Id": 1, "id": 2, "Id": 3 }""", list, options);

			Assert.Equal(3, entry.Id);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_WithRealValidator_StillRejectsUnknownMembersAndMissingRequiredFields(bool list)
		{
			Assert.Throws<ValidationException>(() => Load("""{ "Id": 1, "Unexpected": 2 }""", list));
			Assert.Throws<ValidationException>(() => Load("{}", list));
		}

		[Fact]
		public void LoadExternal_List_WithRealValidator_HonorsRootTrailingComma()
		{
			var options = new JsonSerializerOptions { AllowTrailingCommas = true };
			var loader = new JsonRegistryListLoader<Entry>(new JsonSchemaValidator(), externalOptions: options);

			Assert.Equal(7, Assert.Single(loader.LoadExternal("""[{ "Id": 7, },]""", Source)).Id);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_ModelReusesNestedType_ValidatesPropertiesAndCollectionElements(bool list)
		{
			var entry = Load("""{ "Id": 1, "Details": { "Value": 2 }, "Children": [{ "Value": 3 }] }""", list);

			Assert.Equal(2, entry.Details!.Value);
			Assert.Equal(3, Assert.Single(entry.Children!).Value);
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_ConfiguredDepthAboveDefault_ReachesSchemaComparison(bool list)
		{
			var nestedArrays = new string('[', 70) + "0" + new string(']', 70);
			var json = "{\"Id\":" + nestedArrays + "}";
			var options = new JsonSerializerOptions { MaxDepth = 128 };

			Assert.Throws<ValidationException>(() => Load(json, list, options));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadExternal_ExplicitDuplicateRejection_RecognizesEscapedPropertyNames(bool list)
		{
			var options = new JsonSerializerOptions { AllowDuplicateProperties = false };

			Assert.Throws<RegistryLoadException>(() => Load("""{ "Id": 1, "\u0049d": 2 }""", list, options));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void LoadEmbedded_RemainsIndependentOfExternalDuplicatePolicyAndSchemaValidation(bool list)
		{
			var embedded = new JsonSerializerOptions();
			var external = new JsonSerializerOptions { AllowDuplicateProperties = false };
			const string json = """{ "Id": 1, "Id": 2, "Extra": 3 }""";
			var validator = new JsonSchemaValidator();

			var entry = list
				? new JsonRegistryListLoader<Entry>(validator, embedded, external).LoadEmbedded($"[{json}]", Source).Single()
				: new JsonRegistryObjectLoader<Entry>(validator, embedded, external).LoadEmbedded(json, Source);

			Assert.Equal(2, entry.Id);
			Assert.Throws<RegistryLoadException>(() => Load(json, list, external));
		}

		[Fact]
		public void LoadExternal_List_DuplicatePolicyAlsoAppliesToLaterEntries()
		{
			var options = new JsonSerializerOptions
			{
				AllowDuplicateProperties = false,
				PropertyNameCaseInsensitive = true
			};
			var loader = new JsonRegistryListLoader<Entry>(new JsonSchemaValidator(), externalOptions: options);

			var error = Assert.Throws<RegistryLoadException>(() =>
				loader.LoadExternal("""[{ "Id": 1 }, { "Id": 2, "id": 3 }]""", Source));

			Assert.IsAssignableFrom<JsonException>(error.InnerException);
		}
	}
}
