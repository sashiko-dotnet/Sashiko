# Geography Data Sources

Reviewed: 2026-11-07. Scope: the current Sashiko.Geography seed only.

## Current Source

All factual fields in the embedded seed were freshly acquired from Wikidata's
structured entity data, whose published copyright policy applies CC0-1.0.
The source catalog records this as WIKIDATA-2026-11-07.

The software and documentation remain Apache-2.0. The underlying source facts
retain their CC0 status. See [NOTICE](NOTICE.md), [the rights review](REUSE-REVIEW.md)
and [pinned provenance](PROVENANCE.md).

No ISO, UN M49, Eurostat or Romanian census table is used as an input to the
current seed. Their previous provenance entries were removed after re-acquiring
the facts from the source above; they were not merely renamed.

## Fields And Selection

- World-region display names come from English entity labels. Sashiko selects
  five coarse groupings; this is a model choice rather than a complete taxonomy.
- Country names use English labels; P297 supplies country-code metadata.
- Preferred, non-ended P30 statements support the selected Europe association.
- P36 supplies current capital references. Historical capitals with end times
  are excluded, and preferred statements take precedence.
- P37 identifies official-language entities; those entities' P220 values supply
  the recorded ISO 639-3 codes.
- Country P150 statements select the represented main regions. Region P17
  statements confirm country ownership, and P300 values map existing application
  keys to those entities.
- Regions and cities use Italian or Romanian structured labels. The Italian
  part of the bilingual Trentino-Alto Adige/Südtirol label is selected for the
  display name, preserving the existing record.
- Roma's coarse Lazio association follows Q220 -> Q18288160 -> Q1282 through
  current P131 statements. Bucuresti uses Q19660 both as the represented
  first-level area and the selected settlement reference.

The 2 countries, 62 regions, 2 cities and 5 world groupings retain the previous
identifiers and display names. Stable IDs and selected associations are Sashiko
model choices; they do not imply official registry status or geographic extents.

Only structured labels and statements were imported. No Wikipedia prose,
Wikimedia images, maps, logos, population series or unrelated entity claims
are included. Data is Unicode NFC normalized and sorted through Maintenance.

## Future Contributions

Record the source, exact terms, scope of permission and provenance for every
contribution. For this initial package, the release policy accepts CC0-1.0 or
Apache-2.0 data with a recorded LicenseUrl. Other terms require an explicit
project policy decision; this is not a claim that all other licenses are unlawful.

A URL or license identifier does not by itself establish that the contributor
has the necessary rights. Review whether the grant covers the actual material,
including relevant database rights, modifications, redistribution and commercial
use. Runtime loading does not perform that review or make network requests.
