# 🌸 Sashiko.Registries

**Sashiko.Registries** provides lightweight helpers for loading structured registry data from JSON.

It is designed for packages and applications that rely on declarative data: languages, names, biomes, items, rules, or any domain where structured data defines behavior.

---

## ✨ Features

- JSON object registry loader
- JSON list registry loader
- Separate loading behavior for trusted embedded data and untrusted external data
- Schema validation integration through `ISchemaValidator`
- Option-aware JSON parsing with `System.Text.Json`
- Predictable `RegistryLoadException` wrapping for external input failures
- Clear source/type diagnostics for easier debugging

---

## 📦 Installation

```bash
dotnet add package Sashiko.Registries
```

---

## 🚀 Usage

### Create a registry loader

```csharp
using System.Text.Json;
using Sashiko.Registries.Json;
using Sashiko.Validation.Validators.Json;

var loader = new JsonRegistryObjectLoader<MyRegistryEntry>(
    schemaValidator: new JsonSchemaValidator(),
    embeddedOptions: new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = false
    },
    externalOptions: new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        MaxDepth = 32,
        AllowDuplicateProperties = false
    });
```

### Load trusted embedded JSON

```csharp
var entry = loader.LoadEmbedded(json, "MyRegistryEntry.json");
```

### Load and validate external JSON

```csharp
try
{
    var entry = loader.LoadExternal(json, "MyRegistryEntry.json");
}
catch (RegistryLoadException ex)
{
    Console.WriteLine($"Failed to load registry data from {ex.SourceName}");
}
```

---

## 🛡️ Error Model

| Scenario | Exception |
|----------|-----------|
| Embedded JSON invalid | `JsonException` |
| External JSON invalid | `RegistryLoadException` |
| External schema mismatch | `ValidationException` |
| External deserialization failure | `RegistryLoadException` |

Embedded data is treated as trusted package data. External data is validated and wrapped so callers can handle registry-loading failures consistently.

### Parsing and duplicate policies

Both registry loaders apply `AllowTrailingCommas`, `ReadCommentHandling`,
`MaxDepth` and `AllowDuplicateProperties` during external parsing. The built-in
`JsonSchemaValidator` receives the same options through the existing
`ValidationContext.Metadata["JsonOptions"]` entry, so permitted comments, trailing
commas and depth are handled consistently during validation.

Duplicate rejection is opt-in through .NET's `AllowDuplicateProperties = false`.
With the built-in validator, it checks every object and every array element
before structural inspection, using ordinal matching or ordinal
case-insensitive matching according to `PropertyNameCaseInsensitive`. The rule
also applies to nested/unknown JSON properties and does not infer a custom
converter's or dictionary's own key comparer. Escaped JSON property names are
compared after decoding.

Defaults are unchanged: duplicate properties are allowed unless disabled.
Case-insensitive schema inspection now retains the last matching property rather
than failing with an internal dictionary collision. Normal deserialization can
still reject an invalid value before a later duplicate is encountered.

Malformed JSON and JSON policy errors, including `JsonException` raised by a
validator, are wrapped in `RegistryLoadException` with the source and registry
type. Schema mismatches remain `ValidationException`. Custom validators still
receive the original JSON string, source, case-sensitivity flag and metadata;
their other exceptions are not intercepted by this parsing wrapper. Custom
validators must enforce any additional structural policies they require.

Embedded loading still bypasses schema validation and uses its own serializer
options. Constructor option inheritance is unchanged, and existing Core JSON
option presets have not been made stricter. See the
[Validation README](https://github.com/sashiko-dotnet/Sashiko/blob/master/Sashiko.Validation/README.md#json-parsing-policy) for direct-validator usage
and structural-validation limits.

---

## 🧪 Testing

The test suite covers:

- embedded vs external loading behavior
- JSON option inheritance
- trailing comma handling
- schema validation integration
- validation context metadata
- exception wrapping semantics
- integration with the built-in validator, duplicate policies, and depth limits

---

## 🗺️ Roadmap

Future versions may include:

- additional structured-data formats
- richer registry composition helpers
- source-mapped diagnostics
- built-in validator integrations

---

## 🤝 Contributing

Contributions are welcome.  
Please see [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository root.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.  
See [LICENSE](../LICENSE) for the full license text.

Copyright © 2026 Alexandru Luca (alex98luca)
