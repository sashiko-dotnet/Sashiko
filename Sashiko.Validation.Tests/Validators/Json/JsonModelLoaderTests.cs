using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Sashiko.Validation.Validators.Json;

namespace Sashiko.Validation.Tests.Validators.Json
{
	public sealed class JsonModelLoaderTests
	{
		[Fact]
		public void Load_UsesSerializerNamesAndAcceptsEmptyCollectionsAndOptionalObjects()
		{
			var utf8 = """
				{"items":[{"record_id":1,"children":[],"details":null},{"record_id":2,"children":[]}]}
				"""u8;

			var model = JsonModelLoader.Load(utf8, CreateContract<Catalog>(), utf8.Length);

			Assert.Collection(model.Items,
				first =>
				{
					Assert.Equal(1, first.Id);
					Assert.Empty(first.Children);
					Assert.Null(first.Details);
				},
				second =>
				{
					Assert.Equal(2, second.Id);
					Assert.Null(second.Details);
				});
		}

		[Theory]
		[InlineData("""{"record_id":2,"children":[{"id":3},{}]}""", "$.items[1].children[1]")]
		[InlineData("""{"record_id":2,"children":[{"id":3},{"bad":4}]}""", "$.items[1].children[1].bad")]
		[InlineData("""{"record_id":2,"children":null}""", "$.items[1]")]
		[InlineData("""{"record_id":2}""", "$.items[1]")]
		public void Load_EnforcesTheContractOnLaterNestedItems(string laterItem, string pathPrefix)
		{
			var utf8 = Encoding.UTF8.GetBytes(
				$$"""{"items":[{"record_id":1,"children":[{"id":2}]},{{laterItem}}]}""");

			var error = Assert.Throws<JsonException>(() =>
				JsonModelLoader.Load(utf8, CreateContract<Catalog>(), utf8.Length));

			Assert.StartsWith(pathPrefix, error.Path);
		}

		[Theory]
		[InlineData(false, "id")]
		[InlineData(true, "ID")]
		[InlineData(true, "I\\u0044")]
		public void Load_DuplicatePolicyChecksSkippedObjectsAndDecodedNames(bool ignoreCase, string repeatedName)
		{
			var contract = CreateContract<Details>(options =>
			{
				options.AllowDuplicateProperties = false;
				options.PropertyNameCaseInsensitive = ignoreCase;
				options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;
			});
			var input = Encoding.UTF8.GetBytes($$"""{"id":1,"ignored":[{"id":2,"{{repeatedName}}":3}]}""");

			var error = Assert.Throws<JsonException>(() => JsonModelLoader.Load(input, contract, input.Length));

			Assert.Equal("Duplicate JSON property name.", error.Message);
		}

		[Fact]
		public void Load_DuplicateChecksKeepObjectScopesSeparate()
		{
			var contract = CreateContract<Catalog>(options =>
			{
				options.AllowDuplicateProperties = false;
				options.PropertyNameCaseInsensitive = true;
			});
			var utf8 = """
				{"items":[{"record_id":1,"children":[{"ID":2},{"Id":3}],"details":{"id":4}}]}
				"""u8;

			var item = Assert.Single(JsonModelLoader.Load(utf8, contract, utf8.Length).Items);

			Assert.Collection(item.Children,
				first => Assert.Equal(2, first.Id),
				second => Assert.Equal(3, second.Id));
			Assert.Equal(4, item.Details?.Id);
		}

		[Fact]
		public void Load_AllowsDuplicatesWhenTheContractAllowsThem()
		{
			var contract = CreateContract<Details>(options => options.PropertyNameCaseInsensitive = true);

			Assert.Equal(2, JsonModelLoader.Load("""{"id":1,"ID":2}"""u8, contract, 100).Id);
		}

		[Fact]
		public void Load_EnforcesTheByteLimitBeforeCallingTheConverter()
		{
			var converter = new DetailsConverter();
			var contract = CreateContract<Details>(options =>
			{
				options.AllowDuplicateProperties = false;
				options.Converters.Add(converter);
			});
			var utf8 = Encoding.UTF8.GetBytes("\"é\"");

			Assert.Throws<JsonException>(() => JsonModelLoader.Load(utf8, contract, utf8.Length - 1));
			Assert.Equal(0, converter.ReadCount);
			Assert.Equal(1, JsonModelLoader.Load(utf8, contract, utf8.Length).Id);
			Assert.Equal(1, converter.ReadCount);
		}

		[Fact]
		public void Load_AppliesParserOptionsToDuplicateChecks()
		{
			var contract = CreateContract<Catalog>(options =>
			{
				options.AllowDuplicateProperties = false;
				options.ReadCommentHandling = JsonCommentHandling.Skip;
				options.AllowTrailingCommas = true;
				options.MaxDepth = 2;
			});

			Assert.Empty(JsonModelLoader.Load("""{/* allowed */"items":[],}"""u8, contract, 100).Items);
			Assert.ThrowsAny<JsonException>(() => JsonModelLoader.Load(
				"""{"items":[{"record_id":1,"children":[]}]}"""u8, contract, 100));
		}

		[Theory]
		[InlineData("")]
		[InlineData("null")]
		[InlineData("""{"id":1} {"id":2}""")]
		public void Load_RequiresOneCompleteNonNullModel(string json)
		{
			var utf8 = Encoding.UTF8.GetBytes(json);

			Assert.ThrowsAny<JsonException>(() => JsonModelLoader.Load(utf8, CreateContract<Details>(), utf8.Length));
		}

		private static JsonTypeInfo<T> CreateContract<T>(Action<JsonSerializerOptions>? configure = null)
		{
			var options = new JsonSerializerOptions
			{
				TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				RespectRequiredConstructorParameters = true,
				RespectNullableAnnotations = true,
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
			};
			configure?.Invoke(options);
			return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
		}

		private sealed record Catalog(List<Item> Items);
		private sealed record Item([property: JsonPropertyName("record_id")] int Id, List<Details> Children, Details? Details = null);
		private sealed record Details(int Id);

		private sealed class DetailsConverter : JsonConverter<Details>
		{
			public int ReadCount { get; private set; }

			public override Details Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				ReadCount++;
				return new Details(reader.GetString()!.Length);
			}

			public override void Write(Utf8JsonWriter writer, Details value, JsonSerializerOptions options) =>
				throw new NotSupportedException();
		}
	}
}
