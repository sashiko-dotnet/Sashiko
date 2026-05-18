# 🌸 Sashiko.Names

**Sashiko.Names** provides an embedded, culturally aware person name generator
for .NET applications.  
It is designed for applications that need realistic, structured person names
without depending on external services or fake-data libraries at runtime.

The library ships with curated `names.json` and `rules.json` files for each
supported language. Name pools are embedded in the package and validated as part
of the test suite before release.

---

## ✨ Features

- Embedded name pools and generation rules
- Strongly typed language and sex options
- Structured `PersonName` model with:
  - given names
  - last names
  - patronymics and matronymics when supported
  - prefixes and suffixes when supported
  - computed `FullName` and `DisplayName`
- Culture-aware name ordering, including last-name-first languages
- Language-specific surname behavior, including gendered surnames
- Latin-readable output using plain Latin or accented Latin characters
- Zero runtime dependency on external data sources or fake-data libraries

---

## 📦 Installation

```bash
dotnet add package Sashiko.Names
```

---

## 🚀 Usage

### Generate a random supported name

```csharp
using Sashiko.Names.Api;
using Sashiko.Names.Model.Enums;

var service = new NameService();

var name = service.Generate(Sex.Female);

Console.WriteLine(name.FullName);
```

### Generate a name for a specific language

```csharp
using Sashiko.Names.Api;
using Sashiko.Names.Model.Enums;

var service = new NameService();

var name = service.Generate(Sex.Male, LanguageId.Ita);

Console.WriteLine(name.FullName);
```

### Use generated name parts

```csharp
var name = service.Generate(Sex.Female, LanguageId.Fra);

Console.WriteLine(string.Join(" ", name.GivenNames));
Console.WriteLine(string.Join(" ", name.LastNames));
Console.WriteLine(name.DisplayName);
```

### Generate patronymic or matronymic names

```csharp
var russian = service.Generate(
    Sex.Male,
    LanguageId.Rus,
    fatherName: "Ivan");

var icelandic = service.Generate(
    Sex.Female,
    LanguageId.Isl,
    motherName: "Anna");
```

---

## 🌍 Supported Languages

| Language | ISO 639-3 | `LanguageId` |
|----------|-----------|--------------|
| Standard Arabic | `arb` | `Arb` |
| Bengali | `ben` | `Ben` |
| Mandarin Chinese | `cmn` | `Cmn` |
| English | `eng` | `Eng` |
| French | `fra` | `Fra` |
| Hindi | `hin` | `Hin` |
| Icelandic | `isl` | `Isl` |
| Italian | `ita` | `Ita` |
| Portuguese | `por` | `Por` |
| Romanian | `ron` | `Ron` |
| Russian | `rus` | `Rus` |
| Spanish | `spa` | `Spa` |
| Urdu | `urd` | `Urd` |

`LanguageId.Random` selects one supported language at generation time.

The supported set includes the top 10 languages by total speaker count, represented by their ISO 639-3 identifiers.

---

## 📚 Data Model

Generated names are returned as a `PersonName`.

Each generated name may include:

- **GivenNames** — one or more given names
- **LastNames** — one or more surnames
- **Patronymic** — father-derived name component when supported
- **Matronymic** — mother-derived name component when supported
- **Prefix** — optional prefix when supported
- **Suffix** — optional suffix when supported
- **FullName** — generated name rendered in language-specific order
- **DisplayName** — nickname-aware display value, currently equal to `FullName`

The model keeps individual name parts available so callers can store, inspect,
or render names in their own UI.

---

## ✅ Data Quality Guarantees

Embedded name data is validated before release.

Each supported language must provide:

- At least 150 male given names
- At least 150 female given names
- At least 200 active surname values
- At least 500 unique core name values
- Sorted and duplicate-free arrays
- Trimmed, non-empty values
- Latin-readable text using plain Latin or accented Latin characters

Optional pools such as unisex names, prefixes, and suffixes are allowed to be
empty when they are not natural for the language.

---

## 🔄 Maintaining Embedded Name Data

The embedded `names.json` files are polished using the
**Sashiko Maintenance Tool**:

```bash
dotnet run --project Sashiko.Maintenance -- names polish
```

This process:

1. Reads every `names.json` file under `Sashiko.Names/Data`
2. Trims values
3. Removes blank or duplicate entries
4. Sorts each array alphabetically
5. Writes clean, indented JSON back to disk

The maintenance tool is internal and not required at runtime.

---

## 📄 Source Policy

Name pools should be maintained from public civil-registration, statistical, national name-list, or culturally reviewed name-pool sources.

Sashiko.Names does **not** depend on third-party fake-data libraries as source material or runtime dependencies. See [`Data/SOURCES.md`](Data/SOURCES.md) for the current source policy.

---

## 🤝 Contributing

Contributions are welcome!  
If you would like to improve the names library or propose new language data, please see the [CONTRIBUTING.md](../CONTRIBUTING.md) file in the repository root.

Please keep contributed name data sorted, duplicate-free, Latin-readable, and backed by a clear source.

---

## 📄 License

This project is licensed under the **Apache License 2.0**.  
See the [LICENSE](../LICENSE) file for the full license text.

Copyright © 2026 Alexandru Luca (alex98luca)
