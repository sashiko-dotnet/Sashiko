using System.Text.Json;
using Sashiko.Registries.Json;
using Sashiko.Validation;

namespace Sashiko.Registries.Tests.Json
{
	public sealed class JsonRegistryObjectLoaderTests
	{
		// ------------------------------------------------------------
		// Helpers
		// ------------------------------------------------------------

		private sealed class TestModel
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		private sealed class FakeSchemaValidator : ISchemaValidator
		{
			public bool ShouldThrow { get; set; }
			public object? LastInput { get; private set; }
			public ValidationContext? LastContext { get; private set; }

			public void Validate<T>(object input, ValidationContext? context = null)
			{
				LastInput = input;
				LastContext = context;

				if (ShouldThrow)
					throw new ValidationException("Schema mismatch.");
			}
		}

		private static string ValidJson =>
			"""
            { "Id": 1, "Name": "Test" }
            """;

		private static string InvalidJson =>
			"""
            { "Id": 1, "Name": "Test", }
            """; // trailing comma → invalid unless AllowTrailingCommas = true

		private static string WrongTypeJson =>
			"""
            { "Id": "not-a-number", "Name": "Test" }
            """;

		private static string NullJson => "null";

		// ------------------------------------------------------------
		// Embedded loading
		// ------------------------------------------------------------

		[Fact]
		public void LoadEmbedded_WhenJsonIsValid_ReturnsObject()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			var result = loader.LoadEmbedded(ValidJson, "embedded.json");

			Assert.NotNull(result);
			Assert.Equal(1, result.Id);
			Assert.Equal("Test", result.Name);
		}

		[Fact]
		public void LoadEmbedded_WhenJsonIsInvalid_ThrowsJsonException()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			Assert.Throws<JsonException>(() =>
				loader.LoadEmbedded(InvalidJson, "embedded.json"));
		}

		// ------------------------------------------------------------
		// External loading
		// ------------------------------------------------------------

		[Fact]
		public void LoadExternal_WhenJsonIsInvalid_ThrowsFormatError()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			Assert.Throws<RegistryLoadException>(() =>
				loader.LoadExternal(InvalidJson, "external.json"));
		}

		[Fact]
		public void LoadExternal_WhenSchemaValidatorThrows_PropagatesSchemaError()
		{
			var validator = new FakeSchemaValidator { ShouldThrow = true };
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			Assert.Throws<ValidationException>(() =>
				loader.LoadExternal(ValidJson, "external.json"));
		}

		[Fact]
		public void LoadExternal_WhenDeserializationFails_ThrowsDeserializationError()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			Assert.Throws<RegistryLoadException>(() =>
				loader.LoadExternal(WrongTypeJson, "external.json"));
		}

		[Fact]
		public void LoadExternal_WhenDeserializationReturnsNull_ThrowsDeserializationError()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			var exception = Assert.Throws<RegistryLoadException>(() =>
				loader.LoadExternal(NullJson, "external.json"));

			Assert.Contains("Deserialization returned null", exception.Message);
		}

		[Fact]
		public void LoadExternal_WhenJsonIsValid_ReturnsObject()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			var result = loader.LoadExternal(ValidJson, "external.json");

			Assert.NotNull(result);
			Assert.Equal(1, result.Id);
			Assert.Equal("Test", result.Name);
		}

		// ------------------------------------------------------------
		// Options behavior
		// ------------------------------------------------------------

		[Fact]
		public void Constructor_WhenOnlyEmbeddedOptionsProvided_ReusesThemForExternal()
		{
			var validator = new FakeSchemaValidator();

			var embeddedOptions = new JsonSerializerOptions { AllowTrailingCommas = false };

			var loader = new JsonRegistryObjectLoader<TestModel>(
				validator,
				embeddedOptions: embeddedOptions,
				externalOptions: null);

			// External deserialization should use embeddedOptions
			Assert.Throws<RegistryLoadException>(() =>
				loader.LoadExternal(WrongTypeJson, "external.json"));
		}

		[Fact]
		public void Constructor_WhenOnlyExternalOptionsProvided_ReusesThemForEmbedded()
		{
			var validator = new FakeSchemaValidator();

			var externalOptions = new JsonSerializerOptions { AllowTrailingCommas = false };

			var loader = new JsonRegistryObjectLoader<TestModel>(
				validator,
				embeddedOptions: null,
				externalOptions: externalOptions);

			Assert.Throws<JsonException>(() =>
				loader.LoadEmbedded(InvalidJson, "embedded.json"));
		}

		[Fact]
		public void Constructor_WhenBothOptionsProvided_UsesThemIndependently()
		{
			var validator = new FakeSchemaValidator();

			var embeddedOptions = new JsonSerializerOptions { AllowTrailingCommas = false };
			var externalOptions = new JsonSerializerOptions { AllowTrailingCommas = true };

			var loader = new JsonRegistryObjectLoader<TestModel>(
				validator,
				embeddedOptions,
				externalOptions);

			// Embedded loader should reject trailing commas
			Assert.Throws<JsonException>(() =>
				loader.LoadEmbedded(InvalidJson, "embedded.json"));

			// External loader should accept trailing commas
			var result = loader.LoadExternal(InvalidJson, "external.json");
			Assert.NotNull(result);
		}

		// ------------------------------------------------------------
		// Schema validator integration
		// ------------------------------------------------------------

		[Fact]
		public void LoadExternal_PassesCorrectSourceToSchemaValidator()
		{
			var validator = new FakeSchemaValidator();
			var loader = new JsonRegistryObjectLoader<TestModel>(validator);

			loader.LoadExternal(ValidJson, "myfile.json");

			Assert.Equal("myfile.json", validator.LastContext?.Source);
			Assert.Equal(ValidJson, Assert.IsType<string>(validator.LastInput));
		}

		[Fact]
		public void LoadExternal_PopulatesValidationContextWithMetadata()
		{
			var validator = new FakeSchemaValidator();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var loader = new JsonRegistryObjectLoader<TestModel>(validator, null, options);

			loader.LoadExternal(ValidJson, "file.json");

			var context = validator.LastContext ?? throw new InvalidOperationException();
			var metadata = context.Metadata ?? throw new InvalidOperationException();

			Assert.True(context.IgnoreCase);
			Assert.False(Assert.IsType<bool>(metadata["IsEmbedded"]));
			Assert.Equal(typeof(TestModel).FullName, metadata["RegistryType"]);
			Assert.Same(options, metadata["JsonOptions"]);
		}
	}
}
