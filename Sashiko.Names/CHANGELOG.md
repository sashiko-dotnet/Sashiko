# Changelog — Sashiko.Names

All notable changes to this project will be documented in this file.  
The format follows [Semantic Versioning](https://semver.org/).

---

## [0.2.0] - 2026-05-18

### Added
- Added embedded name data and generation rules for Standard Arabic (`arb`), Bengali (`ben`), Portuguese (`por`), and Urdu (`urd`).
- Expanded `LanguageId` so Sashiko.Names includes the top 10 languages by total speaker count.
- Prepared the package for release with the refined Sashiko NuGet icon asset.

### Documentation
- Updated supported-language documentation and name data source notes for the expanded language set.

---

## [0.1.1] - 2026-05-04

### Improved
- Refactored internal name generation wiring to reduce constructor and method arity reported by SonarQube while preserving the public `NameService` API.
- Simplified embedded name and rules data models with initializer-based JSON deserialization.
- Updated internal Sashiko dependencies to the latest stable Core and Registries packages.

### Testing
- Converted untyped xUnit member data to strongly typed `TheoryData`.
- Replaced repeated inline expected arrays with shared test fixtures.
- Cleaned enum assertions and analyzer-reported test helpers.

### Notes
This patch release does not change the public API or embedded name dataset of **Sashiko.Names**. It keeps the package aligned with the repository quality gate and the latest stable Sashiko building blocks.

---

## [0.1.0] - 2026-05-02

### Added
- Embedded, culturally aware person name registry and generator
- Public `NameService` API for generating structured `PersonName` values
- Strongly typed options for:
  - Supported language selection
  - Random language selection
  - Sex-specific name generation
- Structured `PersonName` model exposing:
  - Given names
  - Last names
  - Patronymics
  - Matronymics
  - Prefixes and suffixes
  - Computed `FullName` and `DisplayName`
- Initial support for:
  - Mandarin Chinese (`cmn`)
  - English (`eng`)
  - French (`fra`)
  - Hindi (`hin`)
  - Icelandic (`isl`)
  - Italian (`ita`)
  - Romanian (`ron`)
  - Russian (`rus`)
  - Spanish (`spa`)
- Language-specific generation rules for:
  - Name ordering
  - Given-name count
  - Double surnames
  - Gendered surnames
  - Patronymic and matronymic patterns
- Embedded `names.json` and `rules.json` data for each supported language
- Internal maintenance command for polishing embedded name pools
- Documentation covering usage, supported languages, data guarantees, maintenance, and source policy

### Data Quality
- Release-floor validation requiring each supported language to provide:
  - At least 150 male given names
  - At least 150 female given names
  - At least 200 active surname values
  - At least 500 unique core name values
- Validation that embedded name pools are:
  - Sorted alphabetically
  - Duplicate-free
  - Trimmed and non-empty
  - Latin-readable using plain Latin or accented Latin characters
- Source policy for maintaining name data from public civil-registration,
  statistical, national name-list, or culturally reviewed sources

### Testing
- Test coverage for:
  - Public name generation through `NameService`
  - Registry loading and supported-language discovery
  - Given-name generation
  - Last-name generation
  - Patronymic generation
  - Matronymic generation
  - Prefix and suffix generation
  - Name assembly
  - Embedded JSON validity and schema validation
  - Embedded data quality guarantees

### Notes
This is the first official release of **Sashiko.Names**.  
It establishes a strongly typed, embedded, dependency-light foundation for
realistic person name generation in the Sashiko ecosystem.
