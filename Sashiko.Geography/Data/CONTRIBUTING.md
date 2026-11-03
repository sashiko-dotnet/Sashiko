# Contributing Geography Data

## Add An Entity

1. Open Data/Countries/{alpha-2}. Use a country.json record for the country,
   Subdivisions/{id}.json for an administrative unit, or Cities/{id}.json for a municipality.
2. Use a stable uppercase country-qualified ID and keep it equal to the filename.
   Preserve IDs when inserting a parent or correcting a source.
3. Record ParentId on a subdivision, or SubdivisionId on a city. Omit it when
   the country is the nearest represented parent. Do not move files into nested folders.
4. Add documented SourceIds from Sources/sources.json. Cite sources for every
   factual field you introduce. Optional population has its own SourceId.
5. Run geography validate, geography polish, and the Geography and Maintenance tests.
   Review the complete diff. Do not add country-specific technical tests or edit global counts.

A subdivision needs Id, Name, Type, and SourceIds. Iso3166_2, ParentId,
and Population are optional. Type is a descriptive administrative category,
not a fixed global enumeration.

A minimal city record is:

```json
{
  "Id": "IT-LOCAL-EXAMPLE",
  "Name": "Example",
  "SubdivisionId": "IT-62",
  "SourceIds": ["YOUR-DOCUMENTED-SOURCE"]
}
```

This example is a template, not seed geography. SourceIds must name existing
catalog entries. Location, UnLocode, and Population are optional and should
be omitted unless supported by evidence. Country membership comes from the path.

## Add A Country

Add country.json with Name, IsoAlpha2, IsoAlpha3, IsoNumeric, M49RegionCode,
CapitalCityIds, OfficialLanguageCodes, and SourceIds. Capital and language lists
may be empty for incomplete records. Add a coverage-baseline entry, including
explicit zero floors for incomplete coverage. CountryId is a convenience; string
lookups work without a new enum member. No online ISO checker is used, so review
code assignments carefully. Existing declared enum mappings must remain consistent.

## Population

Use Value, ReferenceDate (yyyy-MM-dd), Definition, BoundaryVersion, and SourceId.
The date must be a real calendar date. Use the same definition and boundary token
only when their underlying observations are comparable. A year alone does not
establish matching dates or territories. Never change tokens just to make a test pass.

Unknown population is omitted, not zero. Different dates and definitions are
allowed; they do not contribute to incompatible national coverage. Do not sum
a subdivision and its descendants, or a municipality and its overlapping region.
The validator checks non-overlapping known totals at every comparable ancestor.

## Sources And Release Review

Source catalog entries contain Id, Title, Url, Attribution, and an optional
LicenseUrl linking to reviewed reuse terms. Missing LicenseUrl explicitly means
review is pending; geography validate-release blocks publication in that state.
A maintainer must check commercial reuse, attribution, modifications, and any
source-specific restrictions before adding that link. Do not invent a license.

## Shared Rules

IDs and code metadata use uppercase; language codes and population definitions
use lowercase. Lists are sorted and duplicate-free; text is trimmed and NFC
normalized. Optional fields should be omitted rather than fabricated.
Unknown fields, duplicate properties, path/ID mismatches, missing parents,
cycles, source mistakes, and invalid compatible totals fail validation.

Update coverage floors when verified coverage grows. Lowering a floor requires
an explanation of the source or boundary change. The tool does not lower floors,
and tests do not compare policy with historical Git maxima.

Commands from the repository root:

```bash
dotnet run --project Sashiko.Maintenance -- geography validate
dotnet run --project Sashiko.Maintenance -- geography polish
dotnet run --project Sashiko.Maintenance -- geography validate-release
dotnet test Sashiko.Geography.Tests
dotnet test Sashiko.Maintenance.Tests
```
