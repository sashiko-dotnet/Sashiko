# Geography Registry Decisions

## Data To Registry To API

JSON collections define entities and relationships. GeographyDataFiles locates
the embedded collections. GeographyRegistry uses Sashiko.Registries.LoadEmbedded,
sorts immutable snapshots once and builds ID and relationship indexes.
GeographyService delegates queries to one shared Lazy registry.

No runtime schema validation, polishing, external lookups, coverage policy or
population calculations occur. Deserialization and dictionary construction still
fail normally on malformed artifacts; records are never silently skipped.
The loader requires collection routing, not domain validation.

The public package depends on Sashiko.Registries and its existing transitive
Sashiko dependencies. It has no dependency on Sashiko.Languages or Maintenance.
The schema attributes are used by development tooling; trusted loading never
invokes the supplied schema validator.

## A Deliberately Small Model

WorldRegion, Country, Region and City express a curated hierarchy, with one
main-region level. Cities may reference the country directly when regional
detail is not represented. Capital references are stored once on the country;
there is no independently authored capital flag.

IDs are explicit string keys, independently of optional official mappings.
One serializable immutable model per entity serves both runtime loading and
development checks. Constructors copy supplied lists into read-only snapshots;
they do not perform domain validation or normalize records.

Country, region and city identifiers are unique within their own entity kind.
References are typed by their property and query; no universal entity graph or
handwritten country enum is required.

## Validation Lives In Development Tooling

Maintenance owns the validating importer, canonicalization and semantic rules.
It reuses Sashiko.Registries.LoadExternal and Sashiko.Validation for JSON schema
checks, then checks source evidence, identifiers, references, partition ownership,
coordinates and language references. Lists reject null rows, unknown properties,
duplicate JSON properties, duplicate IDs and duplicate references.

The same development importer supports polishing, read-only validation, canonical
checks and source-review checks. Normalization is proposed in memory and the
entire dataset is validated before any writes. Writes are not transactional
against an unexpected filesystem error.

Tests validate embedded content with that importer and load the same content
through the runtime registry. Synthetic fixtures guard failure diagnostics and
the separation between trusted runtime loading and development checks.
No test enumerates a fixed list of seed cities or hard-codes a global seed count.

## Partitioned Collections

One array per small collection is sufficient. Country directories contain main
regions and city partitions. Cities use region collection names, or country-direct,
with optional -part-001 suffixes for large collections.

CountryId and RegionId define membership. Maintenance checks that physical
partitions match those declarations. Moving between valid chunks preserves IDs
and query behavior. There is no separate generated runtime format.

## Release And Future Work

CI checks canonical data and runs tests. Geography releases additionally require
recorded source reuse review. Those checks never modify data after testing.
Source review remains a human responsibility and currently blocks the first release.

Population observations, deeper administration, official-code catalogs, historical
identity changes, geometry, dataset packs and external import APIs are deferred.
Future additions need a concrete consumer, benchmarks, schema decisions and
migration notes. Geography remains independent of Places and evolving world state.
