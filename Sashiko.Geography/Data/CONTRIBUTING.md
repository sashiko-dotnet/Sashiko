# Contributing Geography Data

## Add Data

1. Add a source to sources.json with Id, Title, Url, Attribution and, once reviewed,
   LicenseUrl pointing to the relevant reuse terms.
2. Add country records to countries.json, regions to Countries/{CountryId}/regions.json,
   and cities to Countries/{CountryId}/Cities/{RegionId}.json.
3. Use country-direct.json when a city has no represented RegionId. Split a large
   city collection using {RegionId}-part-001.json or country-direct-part-001.json.
4. Give each record a stable Id, Name and nonempty SourceIds. Countries also need
   WorldRegionId and CapitalCityIds; regions and cities need CountryId.
5. Run geography polish, inspect the diff, then run geography check and the tests.

IDs use uppercase letters, digits and hyphen-separated tokens. They are library
keys, not mandatory ISO identifiers. Preserve IDs when correcting a name or moving
a record. Source IDs additionally permit dots and underscores.
A country addition requires no enum entry or new country-specific test.

Country references must resolve. RegionId, when recorded on a city, must resolve
to a region in the same country. CapitalCityIds must identify cities in that
country. Empty capital lists mean unrecorded detail.

OfficialLanguageCodes is optional and uses canonical ISO 639-3 identifiers
validated against the existing offline Sashiko language registry. IsoAlpha2 is
optional two-letter metadata; its assignment is reviewed by contributors, not
verified over the network.

A minimal city collection looks like:

```json
[
  {
    "Id": "EXAMPLE-TOWN",
    "Name": "Example",
    "CountryId": "EXAMPLE",
    "SourceIds": ["YOUR-SOURCE"]
  }
]
```

This template belongs in Countries/EXAMPLE/Cities/country-direct.json and
requires an existing EXAMPLE country and YOUR-SOURCE catalog entry. It is not
factual seed data.

## Optional Coordinates

Coordinate has Latitude and Longitude in degrees, within [-90, 90] and [-180, 180].
Record a representative settlement point using documented source conventions.
Do not fabricate coordinates, population, boundaries or administrative precision
to make a contribution look complete.

## Canonical Rules

Records are sorted by Id using ordinal comparison. Reference lists are sorted
and duplicate-free. Text is trimmed and normalized to Unicode NFC; IDs are
uppercase, language codes lowercase. Formatting uses Sashiko's indented writer.

Polish normalizes and sorts, but never silently removes duplicate records or
references. It validates all collections before writing any file. Check reports
whether polishing would change files, without writing. Validate checks values
and relationships without rewriting formatting.

The source catalog must retain attribution. A missing LicenseUrl means reuse
review is pending. Validate-release requires canonical data and recorded review.
Maintainers must examine the terms and their applicability; adding a URL alone
does not grant permission or prove official authority.

## Commands

```bash
dotnet run --project Sashiko.Maintenance -- geography polish
dotnet run --project Sashiko.Maintenance -- geography validate
dotnet run --project Sashiko.Maintenance -- geography check
dotnet run --project Sashiko.Maintenance -- geography validate-release
dotnet test Sashiko.Geography.Tests
dotnet test Sashiko.Maintenance.Tests
```

Generic data tests cover every collection. New technical tests are needed for
new rules or behaviors, rather than each additional country or city.
