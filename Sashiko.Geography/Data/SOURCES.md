# Geography Data Sources

The embedded registry contains normalized, derived records from official sources. Source identifiers on population snapshots map to the entries below.

## Codes and hierarchy

- Country codes and first-level subdivision codes follow ISO 3166-1 and ISO 3166-2: https://www.iso.org/iso-3166-country-codes.html
- World-region identifiers follow the United Nations M49 geographic regions: https://unstats.un.org/unsd/methodology/m49/
- Language references use ISO 639-3 identifiers resolved by `Sashiko.Languages`.
- UN/LOCODE values follow the official UNECE publication: https://unlocode.unece.org/publications/

## Population sources

### EUROSTAT-DEMO_GIND-2024

Eurostat `demo_gind`, indicator `JAN` (population on 1 January), reference year 2024:
https://ec.europa.eu/eurostat/databrowser/view/demo_gind/default/table

### EUROSTAT-DEMO_R_PJANAGGR3-2024

Eurostat `demo_r_pjanaggr3`, population on 1 January by NUTS 2 region,
reference year 2024:
https://ec.europa.eu/eurostat/databrowser/view/demo_r_pjanaggr3/default/table

The Italian ISO 3166-2 region `IT-32` is the sum of the NUTS 2 observations
for Provincia Autonoma di Bolzano/Bozen (`ITH1`) and Provincia Autonoma di
Trento (`ITH2`). The 20 normalized region values sum to the national snapshot.

### EUROSTAT-URB_CPOP1-2024

Eurostat `urb_cpop1`, indicator `DE1001V` (population on 1 January, total), city `IT001C` (Roma), reference year 2024:
https://ec.europa.eu/eurostat/databrowser/view/urb_cpop1/default/table

### RO-RPL2021-TABLE-1.22

Romanian National Institute of Statistics, definitive Population and Housing Census 2021, table 1.22, resident population at 1 December 2021:
https://www.recensamantromania.ro/rezultate-rpl-2021/rezultate-definitive/

The table reports `19,053,815` residents for Romania and `1,716,961` residents for Municipiul București, SIRUTA code `179132`. Its 41 county totals plus București sum to the national snapshot.

## Source Catalog And Reuse Review

`Sources/sources.json` is the checked catalog for entity SourceIds and population
SourceId references. The catalog retains source URLs and attribution; reviewed
reuse terms have a LicenseUrl. Romanian census and UN M49 reuse review remains
pending and is intentionally not represented as an invented license.
The release validator blocks publication while those links are absent.

Eurostat records are normalized derivatives. The IT-32 regional observation
aggregates ITH1 and ITH2. Eurostat is not responsible for Sashiko's modifications.
Reuse terms: https://ec.europa.eu/eurostat/help/copyright-notice

Coordinates and UN/LOCODE metadata from the prototype were omitted during this
migration because field-level source evidence was not recorded. This does not
change the seed population values or stable IDs.

## Data Layout And Comparison

Country records live in Countries/{alpha-2}/country.json. Administrative units
and municipalities use Subdivisions/{id}.json and Cities/{id}.json beneath that
country. Explicit parent references define the hierarchy without nested folders.
Population snapshots retain exact reference dates, usual-resident definitions,
and country-specific boundary-version tokens. These tokens describe the retained
snapshots; they do not independently certify source comparability.

See [CONTRIBUTING.md](CONTRIBUTING.md) for authoring and review rules.
