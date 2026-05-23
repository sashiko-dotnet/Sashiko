# Changelog — Sashiko.Core

All notable changes to this project will be documented in this file.  
The format follows [Semantic Versioning](https://semver.org/).

---

## [0.4.1] - 2026-05-23

### Changed
- Refreshed package metadata to use the official Sashiko website and the `sashiko-dotnet/Sashiko` canonical repository.
- Applied centralized shared NuGet metadata used by public Sashiko packages.
- Prepared the release workflow for NuGet Trusted Publishing under the Sashiko NuGet organization.

### Notes
This patch release does not change public APIs or runtime behavior.

---

## [0.4.0] - 2026-05-04

### Added
- Added `OperatingSystemFamily` for shared runtime OS classification.
- Added `RuntimeContext` for cached OS family, OS description, architecture, friendly architecture name, and mobile runtime metadata.
- Extended `RuntimeInfo` with cached `Current` environment data and an `IsMobile` convenience flag.

### Testing
- Added coverage for runtime environment caching, legacy OS flag compatibility, family names, and architecture names.

### Notes
This release promotes reusable runtime detection into **Sashiko.Core**, allowing higher-level packages such as **Sashiko.SystemMonitor** to avoid duplicating operating system checks.

---

## [0.3.1] - 2026-05-03

### Testing
- Refined `StringNormalizer` tests to remove repeated inline expected arrays reported by SonarQube/Roslyn analysis.

### Notes
This patch release does not change the public API or runtime behavior of **Sashiko.Core**. It keeps the package aligned with the repository quality gate before moving toward automated NuGet releases.

---

## [0.3.0] - 2026-04-18

### Added
- **Randomization utilities**:
  - `IRandomPicker` interface for deterministic, injectable random selection
  - `RandomPicker` implementation with seeded `Random` support
  - `ChanceUtility` (internal) for probability-based decisions

- **Text utilities**:
  - `StringNormalizer` for consistent trimming, filtering, and normalization of optional values and string collections

### Testing
- Complete test suite for:
  - `RandomPicker` (lists, sequences, dictionaries, chance delegation)
  - `ChanceUtility` (boundary conditions, deterministic behavior)
  - `StringNormalizer` (optional values, collections, immutability, trimming)

### Notes
This release introduces foundational Core utilities used across the Sashiko ecosystem, enabling deterministic randomization and consistent text normalization for higher-level libraries such as **Sashiko.Names**.

---

## [0.2.0] - 2026-03-18

### Added
- **JSON utilities**:
  - `JsonListLoader<T>` for strongly-typed list deserialization
  - `JsonObjectLoader<T>` for strongly-typed object deserialization
  - `JsonFileReader` with consistent default options and clear error behavior
  - `JsonFileWriter` with neutral default serialization options

- **Registry utilities**:
  - `RegistrySnapshot` for atomic, case-insensitive dictionary replacement
  - `CreateEmpty<T>` helper for consistent registry initialization

### Improved
- Unified JSON error behavior across loaders and file utilities
- Consistent default `JsonSerializerOptions` across all components
- Case-insensitive semantics clarified and enforced in registry tests

### Testing
- Complete test suite for:
  - JSON list loader
  - JSON object loader
  - JSON file reader
  - JSON file writer
  - Registry snapshot utilities
- Refined error expectation tests to match `System.Text.Json` behavior
- Ensured all snapshot tests use case-insensitive dictionaries

### Notes
This release establishes the foundation for future modules (Registries, Validation, Configuration) by providing stable, predictable, and fully tested JSON and registry primitives.

---

## [0.1.0-alpha] - 2026-03-14

- Initial release of **Sashiko.Core**
- `MemoryConverter` with enum-based conversion:
  - Bytes, Kilobytes, Megabytes, Gigabytes, Terabytes
- `BandwidthConverter` with enum-based conversion:
  - Bits, Kilobits, Megabits, Gigabits
- Strongly-typed `MemoryUnit` and `BandwidthUnit` enums
- Full test suite for all conversion paths
- Project metadata and documentation

---

## [Unreleased]

### Planned
- Temperature conversion utilities
- Time and duration helpers
- Human-readable formatting (e.g., “1.5 GB”)
- Additional numeric abstractions
