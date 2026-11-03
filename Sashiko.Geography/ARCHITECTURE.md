# Geography Foundation Decisions

Status: pre-release schema 1. This replaces the unpublished hierarchical prototype.

## Offline Consistency, Not External Authority

Use Sashiko.Registries and Sashiko.Validation for JSON shape and deserialization,
then enforce geography relationships locally. No network access, external
geography library, or full duplicate ISO catalog is required at runtime.
Code syntax and declared local mappings are checked; official assignments and
source claims are reviewed by contributors. All records use the same importer.

## Stable Identity And Flat Country Partitions

One record per entity keeps reviews small. Each country owns flat Subdivisions
and Cities collections. References express the hierarchy; directory nesting
does not. Moving an administrative boundary changes a parent reference instead
of moving an entire subtree. The filename must match the record ID.

Country-qualified stable IDs can use local or national identifiers. Optional
ISO subdivision codes remain metadata. A source revision does not silently
rename existing IDs; mergers and splits need an explicit migration decision.
The dataset manifest versions the authoring schema independently of NuGet versions.

## Incomplete Knowledge Is Explicit

Population and coordinates are optional. Countries may have zero or several
recorded capitals; municipalities may belong directly to their country.
Country-level and subdivision-level language or cultural detail can grow
without requiring complete demographic coverage first. The initial language
association is national official-language metadata, resolved offline through
Sashiko.Languages. Cultural behavior remains outside Geography.

## Comparable Observations

Public snapshots expose DateOnly. Authoring DTOs store validated ISO date strings
so the published Sashiko schema inspector can be reused unchanged.
A comparison requires identical reference date, population definition, and
boundary version. These tokens are reviewed declarations, not automatic proofs.
Different snapshots remain valid records but are excluded from incompatible sums.

For each comparable snapshot, administrative totals use a bottom-up frontier.
Known parent totals replace their descendants; missing intermediates pass known
descendants to the next ancestor. City totals use a separate aggregation.
Neither algorithm adds overlapping administrative and municipal populations.
Traversal is iterative to avoid stack overflow for deeply nested contributions.

## Data Tests And Policy

Synthetic fixtures test invalid references, hierarchy, optional data, snapshots,
and failure diagnostics. Embedded-data tests enumerate every record and compare
coverage with explicit per-country policy. They contain no global seed counts.
New data normally needs a record, source evidence, and a reviewed coverage floor,
not a new test per city or country.

Source catalogs retain provenance and optional links to reviewed reuse terms.
Structural validation permits unreviewed sources during development; a separate
release gate blocks them from package publication. No license is invented for a
source merely because it is publicly accessible.

## Deferred Work

Aliases, regional language associations, time-zone identifiers, boundary geometry,
historical entity lifetimes, external dataset loading, seeded selection, and
compact runtime bundles can be added when concrete consumers require them.
The first version provides deterministic lookup and navigation, not a simulation
engine, universal spatial database, or population generator.
