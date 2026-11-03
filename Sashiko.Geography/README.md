# Sashiko.Geography

An immutable, offline registry of countries, administrative subdivisions, and
municipalities. Geography reuses Sashiko's registry loaders and schema validators;
it does not contact official registries or add external geography dependencies.

## Usage

```csharp
using Sashiko.Geography.Api;
using Sashiko.Geography.Model.Enums;

var geography = new GeographyService();
var italy = geography.GetCountry(CountryId.It);
var lazio = geography.GetSubdivision("IT-62");
var cities = geography.GetCitiesBySubdivision(lazio.Id);
var directCities = geography.GetDirectCitiesBySubdivision(lazio.Id);
var capitals = geography.GetCapitalCities("IT");
var coverage = geography.GetCoverage("IT");
var languages = geography.GetOfficialLanguages("IT");
```

Country lookup accepts embedded alpha-2, alpha-3, numeric codes, or names.
Lookups trim input and are case-insensitive; city-name lookup is an exact
name match, not fuzzy search. Unknown identifiers throw `KeyNotFoundException`;
individual lookups also have `TryGet` variants. A known parent without recorded
children returns an empty collection. Default services share cached read-only data.

`CountryId` is an optional typed convenience for declared countries, with explicit
numeric codes. String lookups support every embedded country. Adding country data
does not require changing tests or a complete global enum.

## Data Contract

Schema version 1 uses one country directory and one file per entity:

```text
Data/
  manifest.json
  Sources/sources.json
  WorldRegions/world-regions.json
  Coverage/coverage-baseline.json
  Countries/
    IT/
      country.json
      Subdivisions/IT-62.json
      Cities/IT-ISTAT-058091.json
```

Subdivision `ParentId` and city `SubdivisionId` point to another subdivision in
the same country. Omitting them means the country is the nearest represented
parent. The country is derived from its directory, not duplicated in every file.
Hierarchy comes from references, not physical nesting: adding a level does not
move descendant files. Entity IDs are stable country-qualified identifiers;
`Iso3166_2` is optional subdivision metadata, not a requirement for identity.

`GetChildSubdivisions` returns direct children; `GetDescendantSubdivisions`
returns all levels. `GetAncestorSubdivisions` returns nearest parent first.
`GetCitiesBySubdivision` includes all descendants, so inserting an intermediate
level preserves higher-level results. `GetDirectCitiesBySubdivision` deliberately
returns only directly assigned municipalities.

Countries declare `CapitalCityIds`, which may be empty or contain several cities.
`City.IsCapital` is derived from those references. Official language lists may
be empty when none is recorded; this does not imply that nobody speaks a language.

## Population And Coverage

Population is optional on every entity. Missing means unknown, never zero.
Observations include a count, an exact ISO reference date, a definition token,
a boundary-version token, and a documented source ID. Only observations sharing
date, definition, and boundary version are summed or compared. Different snapshots
can coexist without fabricating consistency or requiring a country-wide update.

Administrative coverage uses a non-overlapping frontier: a known parent replaces
its descendants in the sum; an unknown intermediate passes known descendants
upward. Every known ancestor is checked. Municipalities are counted separately
and are never added to overlapping administrative totals.

`GetCoverage` returns record counts and population-coverage lower bounds against
the country's observation. Unknown or zero national population produces null
ratios; incompatible child observations do not contribute. The committed baseline
defines minimum coverage per country, rather than requiring all countries to
be complete. Counts alone do not prove geographic or statistical completeness.

## Validation And Maintenance

```bash
dotnet run --project Sashiko.Maintenance -- geography validate
dotnet run --project Sashiko.Maintenance -- geography polish
dotnet run --project Sashiko.Maintenance -- geography validate-release
dotnet test Sashiko.Geography.Tests
dotnet test Sashiko.Maintenance.Tests
```

The same validated importer is used for embedded records, maintenance, and tests.
It rejects unknown fields, duplicate JSON properties or IDs, unsupported schema
versions, broken or cross-country references, cycles, invalid formats, unknown
sources/languages, impossible comparable totals, and coverage regressions.
Diagnostics identify the file and property. Strict loading never silently repairs
data. Polishing normalizes entity text and arrays only after the whole dataset
has validated; it does not rewrite catalogs or coverage floors. Filesystem writes
are not transactional.

Validation proves local consistency, not official ISO assignment, actual geographic
containment, source accuracy, or legal approval. Source review remains a maintainer
responsibility. Release validation additionally refuses sources without documented
reuse terms; adding a URL is not a substitute for reviewing those terms.

## Initial Scope

The seed contains Italy, Romania, their first-level subdivisions, and Roma and
Bucuresti municipalities. Coverage can grow through data contributions. Existing
population values and identifiers were preserved during the schema migration.
Unattributed coordinates and UN/LOCODE fields are omitted until their sources
are documented. Historical boundaries, migration, demographics simulation,
weather, and economics belong to later work.

The package is unreleased. Romanian census and UN M49 reuse terms still require
documented review in the source catalog before publishing.

See [contributing](Data/CONTRIBUTING.md), [sources](Data/SOURCES.md), and the
[design decisions](ARCHITECTURE.md). Code is Apache-2.0; data retains its source terms.
