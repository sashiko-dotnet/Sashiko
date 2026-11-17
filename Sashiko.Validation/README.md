# 🌸 Sashiko.Validation

**Sashiko.Validation** provides lightweight structural validation utilities for JSON data, C# models, and registry-style inputs.

It is built to catch shape problems early, before data is deserialized into a domain model or used by higher-level Sashiko packages.

---

## ✨ Features

- C# schema extraction through reflection
- JSON schema extraction through structural inspection
- Schema comparison with clear missing/extra field reporting
- JSON schema validator included out of the box
- Case-sensitive or case-insensitive matching
- Array element validation
- Context-aware JSON parsing and opt-in duplicate-property rejection
- Format-agnostic `ISchemaValidator` abstraction
- Zero external dependencies

---

## 📦 Installation

```bash
dotnet add package Sashiko.Validation
```

---

## 🚀 Usage

### Validate JSON against a C# model

```csharp
using Sashiko.Validation;
using Sashiko.Validation.Validators.Json;

var validator = new JsonSchemaValidator();

validator.Validate<MyConfig>(
    jsonString,
    new ValidationContext { Source = "config.json" });
```

If the JSON structure does not match the target model, a `ValidationException` is thrown with a readable diff.

```text
Schema mismatch in 'config.json'.
Missing required fields:
  Settings.Mode
Unexpected fields:
  Debug.Verbose
```

---

## 🧠 How It Works

Sashiko.Validation compares two schema trees:

- the expected schema extracted from a C# type
- the actual schema extracted from JSON

The comparer reports:

- missing required fields
- unexpected fields
- object, array, and leaf mismatches
- nested mismatches
- array element mismatches

This keeps validation focused on structure while leaving semantic domain rules to higher-level code.

### JSON parsing policy

`JsonSchemaValidator` reads a `JsonSerializerOptions` instance from
`ValidationContext.Metadata["JsonOptions"]`. Registry loaders supply this context
automatically. Direct callers can configure it explicitly:

```csharp
using System.Text.Json;

var jsonOptions = new JsonSerializerOptions
{
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    MaxDepth = 32,
    PropertyNameCaseInsensitive = true,
    AllowDuplicateProperties = false
};

validator.Validate<MyConfig>(jsonString, new ValidationContext
{
    Source = "config.json",
    IgnoreCase = jsonOptions.PropertyNameCaseInsensitive,
    Metadata = new Dictionary<string, object> { ["JsonOptions"] = jsonOptions }
});
```

For string input, parsing honors `AllowTrailingCommas`, `ReadCommentHandling`,
`MaxDepth` and `AllowDuplicateProperties`. Without a correctly typed `JsonOptions`
entry, the usual `System.Text.Json` parsing defaults apply. Options are not
modified or frozen by validation.

Duplicate properties remain allowed by default. Setting
`AllowDuplicateProperties = false` rejects repeated names before schema
inspection can collapse them. Names are compared with ordinal matching, or
ordinal case-insensitive matching when `ValidationContext.IgnoreCase` is true.
This structural check covers every JSON object, including unknown fields and
all array elements; it does not infer custom converter or dictionary-key rules.
When duplicates are allowed, schema inspection uses the last matching property
in JSON order, including case variants under case-insensitive matching.

Pre-parsed `JsonElement` input is also checked for duplicates under this policy.
Parsing flags and depth limits apply when parsing strings, not retroactively to
an existing element. The caller retains ownership of an element's document;
documents created internally for string input are disposed before returning.

Parsing and duplicate-policy failures throw `JsonException`; schema differences
continue to throw `ValidationException`. This remains a structural validator,
not a full JSON Schema implementation or a replacement for deserialization:
serializer naming policies, custom converters and value constraints do not
redefine the reflected C# schema.

Repeated sibling properties and collection elements may reuse the same C# type.
Recursion protection applies only to types already on the current ancestor path.

---

## 🧪 Testing

The test suite covers:

- C# schema extraction
- JSON schema extraction
- schema comparison
- nullable and required property behavior
- validator integration
- arrays, nested objects, primitives, and recursion safety
- JSON parsing options, duplicate policies, and repeated sibling model types

---

## 🗺️ Roadmap

Future versions may include:

- semantic validation helpers
- structured validation error objects
- error codes and diagnostics
- additional format inspectors
- composite validation pipelines

---

## 🤝 Contributing

Contributions are welcome.  
Please see [CONTRIBUTING.md](../CONTRIBUTING.md) in the repository root.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.  
See [LICENSE](../LICENSE) for the full license text.

Copyright © 2026 Alexandru Luca (alex98luca)
