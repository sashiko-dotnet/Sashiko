# Changelog - Sashiko.Geography

## [0.1.0] - Unreleased

### Added
- Offline country, subdivision, municipality, source, population, and coverage models.
- Flat country-partitioned authoring schema with stable IDs and explicit parent references.
- Schema-version manifest and shared validated loading through Sashiko.Registries and Sashiko.Validation.
- Optional ISO subdivision metadata, population observations, and sourced coordinates.
- Support for partial countries, multiple capitals, and country-level municipalities.
- Cached read-only lookups, ancestor/descendant navigation, and compatible-population coverage.
- Source-reference validation and a pre-publication reuse-documentation gate.
- Synthetic logic tests and automatic embedded-data validation independent of seed counts.

### Notes
- Replaces the unpublished Geography prototype; there is no released schema to migrate.
- Initial identifiers and population values for Italy and Romania are retained.
- Population snapshots include exact dates, definitions, boundary versions, and sources.
- Validation enforces offline consistency and syntax, not official registry assignments.
- Source reuse review must be completed before publication.
