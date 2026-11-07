# Changelog — Sashiko Maintenance Tool

This changelog tracks meaningful changes to the internal maintenance tool used
across the Sashiko ecosystem.  
It is intended for contributors.  
The maintenance tool is not versioned or published as a NuGet package.

---

## Unreleased

- Add geography polish, validate, check and validate-release commands.
- Validate JSON shape, identifiers, sources, relationships and collection partitions
  during development, keeping the published registry's loading path lightweight.
- Normalize and sort complete proposed datasets before writing; reject duplicates.
- Add regression tests for validation, read-only checks, release review and idempotence.

## 2026-08-14
### Changed
- Updated the internal Sashiko.Core dependency to `0.4.1` so solution restore stays aligned with the latest registry packages.

---

## 2026-05-04
### Improved
- Resolved SonarQube/Roslyn findings in the maintenance entry point, language updater, name pool polisher, and command tests.
- Made required ISO 639-3 normalization explicit when mapping SIL language rows.
- Reworked the ISO 639-3 download endpoint construction to avoid hardcoded absolute URI literals.
- Tightened name pool polishing helpers and test fixtures for clearer analyzer behavior.

### Testing
- Added assertions to no-hook command handler tests.
- Replaced repeated inline test arrays with shared fixtures.
- Kept command-dispatch and name-polishing tests deterministic and isolated.

### Documentation
- Documented the names maintenance workflow that was introduced after the original maintenance tool changelog entry.

---

## 2026-05-02
### Added
- `names polish` command for maintaining embedded `Sashiko.Names` data.
- `NamePoolPolisher` for trimming, sorting, and deduplicating every embedded `names.json` file.
- `NamePoolMaintenance` entry point for locating and polishing the `Sashiko.Names/Data` folder.
- Tests covering names command dispatch and name pool polishing behavior.

### Notes
- Names maintenance keeps embedded data contributor-friendly without adding runtime dependencies to `Sashiko.Names`.

---

## 2026-04-08
### Added
- Initial implementation of the handler-based command architecture
- `CommandDispatcher` with pluggable handler registry
- `LanguageCommandHandler` with instance-based update hook
- `languages update` command for regenerating the embedded language registry
- Full test suite for dispatcher routing and handler behavior
- Console output isolation and deterministic test setup
- README documenting usage, architecture, and extension model

### Notes
- This is the first official version of the maintenance tool.
- Changes are tracked to support contributors maintaining registry-based libraries.
