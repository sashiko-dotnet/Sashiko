# Changelog — Sashiko.Registries

All notable changes to this project will be documented in this file.
The format follows [Semantic Versioning](https://semver.org/).

---

## [Unreleased]

### Changed
- Require the published `Sashiko.Validation` 0.1.3 package for consistent JSON parsing and repeated-model inspection.

### Fixed
- Apply external parsing options consistently across syntax checks and the built-in schema validator, including maximum depth and explicit duplicate-property rejection.
- Wrap JSON parsing/policy failures from validation as `RegistryLoadException`, preserving source/type diagnostics and schema `ValidationException` behaviour.
- Dispose parsed documents used by external syntax checks and share the object/list parsing pipeline.

### Testing
- Exercise object and list loaders with the built-in validator, including comments, trailing commas, depth limits, duplicate names, embedded options and repeated model types.
- Preserve the original string/context contract for custom validators and keep duplicate rejection opt-in.

### Planned
- Additional structured-data formats
- Registry composition helpers
- Source-mapped diagnostics
- Built-in validator integrations

---

## [0.1.2] - 2026-08-14

### Changed
- Refreshed package metadata through the centralized public Sashiko package settings.
- Aligned the package with the official lightweight NuGet icon asset already used by **Sashiko.Core**.
- Updated internal Sashiko dependencies to **Sashiko.Core** `0.4.1` and **Sashiko.Validation** `0.1.2`.

### Notes
This patch release does not change public APIs or runtime behavior.

---

## [0.1.1] - 2026-05-03

### Improved
- Refactored JSON registry loaders to remove unused embedded-options state while preserving option inheritance behavior.
- Replaced generic null checks in external deserialization guards with default-value comparison compatible with unconstrained generic models.
- Updated internal Sashiko dependencies to the latest stable package versions.

### Testing
- Added regression coverage for external JSON payloads that deserialize to `null`.
- Added focused coverage for `RegistryLoadException` registry context and inner-exception behavior.
- Cleaned nullable test assertions reported by SonarQube/Roslyn analysis.

### Notes
This patch release does not change the public API or runtime contract of **Sashiko.Registries**. It keeps the package aligned with the repository quality gate and the latest stable Sashiko building blocks.

---

## [0.1.0] - 2026-03-18

### Added

- **Registry loaders**

  - `JsonRegistryObjectLoader<T>` for strongly-typed object registries
  - `JsonRegistryListLoader<T>` for strongly-typed list registries
  - Clear separation between **embedded** (trusted) and **external** (untrusted) loading paths

- **Validation pipeline**

  - Integration with `ISchemaValidator` for external JSON

  - Rich `ValidationContext` including:

    - `Source`
    - `IgnoreCase`
    - `JsonOptions`
    - `RegistryType`
    - `IsEmbedded`

  - Consistent validation semantics across object and list loaders

- **Error model**

  - Introduced `RegistryLoadException` for predictable, domain‑specific error handling
  - External JSON failures (format or deserialization) are wrapped
  - Schema validation errors (`ValidationException`) are preserved
  - Embedded JSON surfaces raw exceptions for clarity

- **Option‑aware JSON loading**

  - Full support for custom `JsonSerializerOptions`
  - External JSON parsing respects:
    - `AllowTrailingCommas`
    - `ReadCommentHandling`
    - `PropertyNameCaseInsensitive`
  - Embedded and external options can be reused or defined independently

### Testing

- Complete test suite for:
  - Object registry loader
  - List registry loader
  - ValidationContext propagation
  - Option inheritance and independence
  - Trailing comma and comment handling
  - RegistryLoadException wrapping behavior
- Behavior‑driven tests documenting the intended API and error model

### Notes

This release establishes the foundation for future registry modules (e.g., Languages, Names) by providing a stable, expressive, and fully tested registry loading pipeline.
