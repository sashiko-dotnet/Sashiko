# Sashiko.Geography

A small, curated geographic registry for applications and simulations. Embedded
JSON defines the world regions, countries, main regions and selected settlements.
The registry loads those trusted records once and builds immutable lookup indexes.
Maintenance and tests validate data before publication.

## Usage

```csharp
using Sashiko.Geography.Api;

var geography = new GeographyService();
var italy = geography.GetCountry("IT");
var regions = geography.GetRegions(italy.Id);
var cities = geography.GetCitiesByRegion("IT-62");
var roma = geography.GetCity("IT-ROMA");
var capitals = geography.GetCapitalCities("IT");
var matches = geography.FindCities("Roma", countryId: "IT");
```

The service also exposes WorldRegions, Countries, Regions, Cities and Sources.
Individual Get methods have TryGet variants. IDs are trimmed and matched without
case sensitivity. They are registry keys; display names and optional ISO metadata
are not additional lookup keys. FindCities performs an exact, trimmed,
case-insensitive name search and permits duplicate names.

Queries for known parents with no recorded children return empty collections.
Unknown lookup subjects throw KeyNotFoundException, or return false through
TryGet. Results have deterministic ID ordering. Service instances share the same
read-only registry and records, including during concurrent construction.

## Small World Model

- WorldRegion: a curated high-level grouping.
- Country: a named country, a world-region reference and zero or more capital references.
- Region: one represented main area within a country.
- City: a selected settlement belonging to a country and optionally a region.

Every factual record has SourceIds. Countries can additionally record IsoAlpha2
and OfficialLanguageCodes. Cities can record a representative Coordinate.
Omitted detail means unknown or unrepresented, not zero or nonexistent.

Regions have one level in this version. A City does not claim to describe a
complete administrative municipality or its territory. The country notes in
[the source documentation](Data/SOURCES.md) explain the represented seed.
Coordinates, if added, are latitude/longitude settlement points in degrees,
not facility addresses or boundaries.

IDs are stable Sashiko keys. A code-like spelling does not imply official
assignment. Optional ISO metadata is checked for format and local consistency
during development, without online authority lookups. Country additions do not
require enum edits. Language codes can be resolved separately using
Sashiko.Languages; constructing Geography does not initialize that registry.

## Authoring Data

```text
Data/
  world-regions.json
  countries.json
  sources.json
  Countries/
    IT/
      regions.json
      Cities/IT-62.json
    RO/
      regions.json
      Cities/RO-B.json
```

Files contain arrays. Cities are grouped by represented region, with
country-direct.json for records without regional detail. Large collections can
use stable names such as IT-62-part-001.json. Relationships are explicit record
fields, checked against partition scope during development.

## Maintenance And Tests

From the repository root:

```bash
dotnet run --project Sashiko.Maintenance -- geography polish
dotnet run --project Sashiko.Maintenance -- geography validate
dotnet run --project Sashiko.Maintenance -- geography check
dotnet test Sashiko.Geography.Tests
dotnet test Sashiko.Maintenance.Tests
```

Polish normalizes text and sorts records and reference lists. It validates the
complete proposed dataset before writing and rejects semantic errors and
duplicates. Check performs the same proposed normalization in memory and fails
if any file would change. Validate checks canonical values and relationships
without rewriting formatting.

Tests enumerate all embedded collections, validate their content through the
maintenance importer, and independently exercise the actual trusted runtime
loader and APIs. Routine data additions need no country-specific technical test.

CI runs geography check before testing. Geography publication additionally runs
geography validate-release to require canonical data and recorded source reuse
review. A review URL is evidence recorded by a maintainer, not automatic proof
of permission or factual correctness.

## Status And Scope

This is an unreleased 0.1.0 foundation. The seed includes five world groupings,
Italy and Romania, their 62 represented main regions, and Roma and Bucuresti.
It is intentionally incomplete. No population analysis, boundary geometry,
nested administrative graph, weather or demographic evolution runs here.

Romanian census and UN M49 reuse review remains pending in the source catalog.
The release check blocks publication until that review is documented. A local
package can still be built for testing; it is not an official release.

This replaces an unpublished prototype. City IDs are now IT-ROMA and
RO-BUCURESTI; no compatibility with experimental population or subdivision APIs
is promised. Earlier branches retain that work for future scoped capabilities.

See [contributing](Data/CONTRIBUTING.md), [sources](Data/SOURCES.md), and
[architecture](ARCHITECTURE.md). Source code is Apache-2.0; source data retains
its applicable terms.
