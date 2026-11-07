# Geography Source Reuse Review

Date: 2026-11-07. This is a source-terms and implementation review for Geography,
not a binding legal clearance opinion or an audit of the whole ecosystem.

## Outcome

The previous seed was not supported by a complete redistribution review.
It has been replaced by a fresh acquisition of Wikidata structured data under
CC0-1.0. No geographic identifier, display name or represented membership changed.
The original references remain in Git history as development history; they are
not the provenance of the current distributed seed.

Sashiko's source-code license remains Apache-2.0. We do not use that license
label to claim ownership of third-party material or remove its original terms.

## Earlier Sources

| Source | What the published terms establish | Decision for this seed |
|---|---|---|
| ISO 3166 | ISO expressly permits free use of country codes. That statement does not establish an Apache redistribution grant for every referenced subdivision/catalog field. | Replace the catalog input; preserve application keys and source current facts independently. |
| Eurostat | General statistical-data reuse is permitted with acknowledgement, subject to dataset/third-party exceptions and notices for modifications. Some non-EU/EFTA/candidate-country data have commercial restrictions. | Prefer a source without those conditional reuse requirements for this small seed. |
| UN M49 | The M49 page links to UN terms that restrict general website reuse. No more permissive M49-specific grant was established in this review. | Replace; do not treat public accessibility as permission. |
| Romanian census table 1.22 | A specific redistribution grant for the referenced table was not established. The source page could not be retrieved during this review. | Replace; no conclusion that the data is unlawful, only insufficient evidence for this project's policy. |

Evidence reviewed:

- [ISO country-code policy](https://www.iso.org/iso-3166-country-codes.html)
- [Eurostat reuse policy and exceptions](https://ec.europa.eu/eurostat/help/copyright-notice)
- [UN M49 source page](https://unstats.un.org/unsd/methodology/m49/)
- [UN website terms](https://www.un.org/en/about-us/terms-of-use)
- [Romanian census source](https://www.recensamantromania.ro/rezultate-rpl-2021/rezultate-definitive/)

This review does not assume that individual place names or codes are copyrightable,
or that changing their JSON format removes any rights. It chooses a source with
an explicit grant instead of depending on an unresolved legal interpretation.

## Replacement And Scope

Wikidata's published policy releases structured entity data under CC0. Other
wiki namespaces and media have different terms and are excluded from this import.
CC0 permits copying, modification and redistribution, including commercial use,
within the rights the affirmer can waive or license.

See [Wikidata copyright policy](https://www.wikidata.org/wiki/Wikidata:Copyright),
[licensing scope](https://www.wikidata.org/wiki/Wikidata:Licensing), and
[CC0 terms](https://creativecommons.org/publicdomain/zero/1.0/).

The records were regenerated from freshly retrieved entity labels and statements.
[PROVENANCE.md](PROVENANCE.md) pins entity revisions, fields and transformations.
That establishes an inspectable source trail rather than changing the names of
old provenance entries.

Natural Earth is a candidate for later geometry: its publisher declares its map
data public domain and permits commercial use and modification. No Natural Earth
data is included here, and any future import requires its own review and policy
decision. [Published terms](https://www.naturalearthdata.com/about/terms-of-use/).

## Project Policy And Limits

The initial release check requires both LicenseId and LicenseUrl and accepts
CC0-1.0 or Apache-2.0 sources. Missing, unknown, noncommercial or share-alike
license declarations are blocked under that project policy, even if a URL exists.
This does not make the check a legal-permission engine.

Maintainers must review the actual grant and material before recording it.
CC0 and Apache-2.0 do not supply warranties of factual accuracy or clear every
possible trademark, privacy or third-party claim. Unresolved cases should remain
excluded or receive qualified legal review.

No NuGet package was published as part of this review. The code and API contracts
remain independent of this source choice and require no network access at runtime.
