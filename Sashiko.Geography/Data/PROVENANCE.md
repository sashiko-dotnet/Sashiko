# Geography Provenance Snapshot

Acquired: 2026-11-07. Source: Wikidata structured entity data, CC0-1.0.
The table records 73 distinct entity revisions used for 74 record/support mappings.

## Extraction Rules

Structured labels and the listed statement properties were selected from the
MediaWiki wbgetentities API using props=info|labels|claims and languages=en|it|ro.
Deprecated statements and statements with P582 end times were excluded.
Preferred statements take precedence where available.

Country P150 memberships and region P17 statements were checked before projecting
the selected regions. Countries use English labels; regions/cities use their
selected Italian or Romanian label. The Italian portion of Q1237's bilingual
label was retained before the slash. Sashiko's existing IDs, display names and
coarse memberships were compared with the fresh extraction and preserved.

Country-capital references use P36; official-language references use P37 and
supporting language entities' P220 codes. Country code metadata uses P297 and
existing region keys map to P300 values. The preferred Italy P30 association
selects Europe for this simplified grouping.

Roma's region reference is projected through Q220 P131 Q18288160 P131 Q1282.
Bucuresti's selected settlement and first-level area use the same entity Q19660
in different model roles. No population or boundary equivalence is inferred.

Each link below identifies the exact entity revision inspected. Retrieval and
review are development activities, not runtime or build dependencies.

| Kind | Sashiko key | Entity revision | Fields or support |
|---|---|---|---|
| WorldRegion | AFRICA | [Q15 @ 2562953310](https://www.wikidata.org/w/index.php?title=Q15&oldid=2562953310) | English label; selected world grouping |
| WorldRegion | AMERICAS | [Q828 @ 2562602982](https://www.wikidata.org/w/index.php?title=Q828&oldid=2562602982) | English label; selected world grouping |
| WorldRegion | ASIA | [Q48 @ 2565116170](https://www.wikidata.org/w/index.php?title=Q48&oldid=2565116170) | English label; selected world grouping |
| WorldRegion | EUROPE | [Q46 @ 2564314527](https://www.wikidata.org/w/index.php?title=Q46&oldid=2564314527) | English label; selected world grouping |
| WorldRegion | OCEANIA | [Q55643 @ 2565433177](https://www.wikidata.org/w/index.php?title=Q55643&oldid=2565433177) | English label; selected world grouping |
| Country | IT | [Q38 @ 2564285091](https://www.wikidata.org/w/index.php?title=Q38&oldid=2564285091) | English label; P297; P30; P36; P37; P150 |
| Country | RO | [Q218 @ 2564008844](https://www.wikidata.org/w/index.php?title=Q218&oldid=2564008844) | English label; P297; P30; P36; P37; P150 |
| Region | IT-21 | [Q1216 @ 2564284158](https://www.wikidata.org/w/index.php?title=Q1216&oldid=2564284158) | it label; P300; P17; country P150 |
| Region | IT-23 | [Q1222 @ 2564284164](https://www.wikidata.org/w/index.php?title=Q1222&oldid=2564284164) | it label; P300; P17; country P150 |
| Region | IT-25 | [Q1210 @ 2564284151](https://www.wikidata.org/w/index.php?title=Q1210&oldid=2564284151) | it label; P300; P17; country P150 |
| Region | IT-32 | [Q1237 @ 2564284168](https://www.wikidata.org/w/index.php?title=Q1237&oldid=2564284168) | it label; P300; P17; country P150 |
| Region | IT-34 | [Q1243 @ 2564284179](https://www.wikidata.org/w/index.php?title=Q1243&oldid=2564284179) | it label; P300; P17; country P150 |
| Region | IT-36 | [Q1250 @ 2564330031](https://www.wikidata.org/w/index.php?title=Q1250&oldid=2564330031) | it label; P300; P17; country P150 |
| Region | IT-42 | [Q1256 @ 2564284198](https://www.wikidata.org/w/index.php?title=Q1256&oldid=2564284198) | it label; P300; P17; country P150 |
| Region | IT-45 | [Q1263 @ 2564284205](https://www.wikidata.org/w/index.php?title=Q1263&oldid=2564284205) | it label; P300; P17; country P150 |
| Region | IT-52 | [Q1273 @ 2564284597](https://www.wikidata.org/w/index.php?title=Q1273&oldid=2564284597) | it label; P300; P17; country P150 |
| Region | IT-55 | [Q1280 @ 2564356771](https://www.wikidata.org/w/index.php?title=Q1280&oldid=2564356771) | it label; P300; P17; country P150 |
| Region | IT-57 | [Q1279 @ 2564284207](https://www.wikidata.org/w/index.php?title=Q1279&oldid=2564284207) | it label; P300; P17; country P150 |
| Region | IT-62 | [Q1282 @ 2564284602](https://www.wikidata.org/w/index.php?title=Q1282&oldid=2564284602) | it label; P300; P17; country P150 |
| Region | IT-65 | [Q1284 @ 2565781297](https://www.wikidata.org/w/index.php?title=Q1284&oldid=2565781297) | it label; P300; P17; country P150 |
| Region | IT-67 | [Q1443 @ 2564284224](https://www.wikidata.org/w/index.php?title=Q1443&oldid=2564284224) | it label; P300; P17; country P150 |
| Region | IT-72 | [Q1438 @ 2563595224](https://www.wikidata.org/w/index.php?title=Q1438&oldid=2563595224) | it label; P300; P17; country P150 |
| Region | IT-75 | [Q1447 @ 2564284231](https://www.wikidata.org/w/index.php?title=Q1447&oldid=2564284231) | it label; P300; P17; country P150 |
| Region | IT-77 | [Q1452 @ 2564285337](https://www.wikidata.org/w/index.php?title=Q1452&oldid=2564285337) | it label; P300; P17; country P150 |
| Region | IT-78 | [Q1458 @ 2564284240](https://www.wikidata.org/w/index.php?title=Q1458&oldid=2564284240) | it label; P300; P17; country P150 |
| Region | IT-82 | [Q1460 @ 2564284453](https://www.wikidata.org/w/index.php?title=Q1460&oldid=2564284453) | it label; P300; P17; country P150 |
| Region | IT-88 | [Q1462 @ 2564284463](https://www.wikidata.org/w/index.php?title=Q1462&oldid=2564284463) | it label; P300; P17; country P150 |
| Region | RO-AB | [Q45868 @ 2550001638](https://www.wikidata.org/w/index.php?title=Q45868&oldid=2550001638) | ro label; P300; P17; country P150 |
| Region | RO-AG | [Q179567 @ 2563307336](https://www.wikidata.org/w/index.php?title=Q179567&oldid=2563307336) | ro label; P300; P17; country P150 |
| Region | RO-AR | [Q179559 @ 2561650288](https://www.wikidata.org/w/index.php?title=Q179559&oldid=2561650288) | ro label; P300; P17; country P150 |
| Region | RO-B | [Q19660 @ 2563972624](https://www.wikidata.org/w/index.php?title=Q19660&oldid=2563972624) | ro label; P300; P17; country P150 |
| Region | RO-BC | [Q179779 @ 2549959444](https://www.wikidata.org/w/index.php?title=Q179779&oldid=2549959444) | ro label; P300; P17; country P150 |
| Region | RO-BH | [Q179588 @ 2563307611](https://www.wikidata.org/w/index.php?title=Q179588&oldid=2563307611) | ro label; P300; P17; country P150 |
| Region | RO-BN | [Q181513 @ 2552212315](https://www.wikidata.org/w/index.php?title=Q181513&oldid=2552212315) | ro label; P300; P17; country P150 |
| Region | RO-BR | [Q188503 @ 2549822555](https://www.wikidata.org/w/index.php?title=Q188503&oldid=2549822555) | ro label; P300; P17; country P150 |
| Region | RO-BT | [Q178861 @ 2540228281](https://www.wikidata.org/w/index.php?title=Q178861&oldid=2540228281) | ro label; P300; P17; country P150 |
| Region | RO-BV | [Q182614 @ 2522573462](https://www.wikidata.org/w/index.php?title=Q182614&oldid=2522573462) | ro label; P300; P17; country P150 |
| Region | RO-BZ | [Q185309 @ 2540225922](https://www.wikidata.org/w/index.php?title=Q185309&oldid=2540225922) | ro label; P300; P17; country P150 |
| Region | RO-CJ | [Q182624 @ 2540226187](https://www.wikidata.org/w/index.php?title=Q182624&oldid=2540226187) | ro label; P300; P17; country P150 |
| Region | RO-CL | [Q188948 @ 2540227613](https://www.wikidata.org/w/index.php?title=Q188948&oldid=2540227613) | ro label; P300; P17; country P150 |
| Region | RO-CS | [Q187925 @ 2563308000](https://www.wikidata.org/w/index.php?title=Q187925&oldid=2563308000) | ro label; P300; P17; country P150 |
| Region | RO-CT | [Q188505 @ 2549822556](https://www.wikidata.org/w/index.php?title=Q188505&oldid=2549822556) | ro label; P300; P17; country P150 |
| Region | RO-CV | [Q188959 @ 2540227589](https://www.wikidata.org/w/index.php?title=Q188959&oldid=2540227589) | ro label; P300; P17; country P150 |
| Region | RO-DB | [Q188797 @ 2540227752](https://www.wikidata.org/w/index.php?title=Q188797&oldid=2540227752) | ro label; P300; P17; country P150 |
| Region | RO-DJ | [Q191065 @ 2540227471](https://www.wikidata.org/w/index.php?title=Q191065&oldid=2540227471) | ro label; P300; P17; country P150 |
| Region | RO-GJ | [Q190406 @ 2540227568](https://www.wikidata.org/w/index.php?title=Q190406&oldid=2540227568) | ro label; P300; P17; country P150 |
| Region | RO-GL | [Q188796 @ 2544563958](https://www.wikidata.org/w/index.php?title=Q188796&oldid=2544563958) | ro label; P300; P17; country P150 |
| Region | RO-GR | [Q193055 @ 2563308354](https://www.wikidata.org/w/index.php?title=Q193055&oldid=2563308354) | ro label; P300; P17; country P150 |
| Region | RO-HD | [Q191071 @ 2540227425](https://www.wikidata.org/w/index.php?title=Q191071&oldid=2540227425) | ro label; P300; P17; country P150 |
| Region | RO-HR | [Q190702 @ 2540227518](https://www.wikidata.org/w/index.php?title=Q190702&oldid=2540227518) | ro label; P300; P17; country P150 |
| Region | RO-IF | [Q191670 @ 2551708454](https://www.wikidata.org/w/index.php?title=Q191670&oldid=2551708454) | ro label; P300; P17; country P150 |
| Region | RO-IL | [Q193044 @ 2540227071](https://www.wikidata.org/w/index.php?title=Q193044&oldid=2540227071) | ro label; P300; P17; country P150 |
| Region | RO-IS | [Q182888 @ 2540228034](https://www.wikidata.org/w/index.php?title=Q182888&oldid=2540228034) | ro label; P300; P17; country P150 |
| Region | RO-MH | [Q191717 @ 2540227115](https://www.wikidata.org/w/index.php?title=Q191717&oldid=2540227115) | ro label; P300; P17; country P150 |
| Region | RO-MM | [Q188813 @ 2549822572](https://www.wikidata.org/w/index.php?title=Q188813&oldid=2549822572) | ro label; P300; P17; country P150 |
| Region | RO-MS | [Q190711 @ 2561194923](https://www.wikidata.org/w/index.php?title=Q190711&oldid=2561194923) | ro label; P300; P17; country P150 |
| Region | RO-NT | [Q188661 @ 2549822560](https://www.wikidata.org/w/index.php?title=Q188661&oldid=2549822560) | ro label; P300; P17; country P150 |
| Region | RO-OT | [Q188945 @ 2540227681](https://www.wikidata.org/w/index.php?title=Q188945&oldid=2540227681) | ro label; P300; P17; country P150 |
| Region | RO-PH | [Q188665 @ 2540227950](https://www.wikidata.org/w/index.php?title=Q188665&oldid=2540227950) | ro label; P300; P17; country P150 |
| Region | RO-SB | [Q184797 @ 2540225887](https://www.wikidata.org/w/index.php?title=Q184797&oldid=2540225887) | ro label; P300; P17; country P150 |
| Region | RO-SJ | [Q190690 @ 2549963926](https://www.wikidata.org/w/index.php?title=Q190690&oldid=2549963926) | ro label; P300; P17; country P150 |
| Region | RO-SM | [Q165459 @ 2563308672](https://www.wikidata.org/w/index.php?title=Q165459&oldid=2563308672) | ro label; P300; P17; country P150 |
| Region | RO-SV | [Q181209 @ 2551929815](https://www.wikidata.org/w/index.php?title=Q181209&oldid=2551929815) | ro label; P300; P17; country P150 |
| Region | RO-TL | [Q191078 @ 2540227280](https://www.wikidata.org/w/index.php?title=Q191078&oldid=2540227280) | ro label; P300; P17; country P150 |
| Region | RO-TM | [Q185586 @ 2561365051](https://www.wikidata.org/w/index.php?title=Q185586&oldid=2561365051) | ro label; P300; P17; country P150 |
| Region | RO-TR | [Q191653 @ 2540227250](https://www.wikidata.org/w/index.php?title=Q191653&oldid=2540227250) | ro label; P300; P17; country P150 |
| Region | RO-VL | [Q186838 @ 2555873756](https://www.wikidata.org/w/index.php?title=Q186838&oldid=2555873756) | ro label; P300; P17; country P150 |
| Region | RO-VN | [Q182778 @ 2563307265](https://www.wikidata.org/w/index.php?title=Q182778&oldid=2563307265) | ro label; P300; P17; country P150 |
| Region | RO-VS | [Q167199 @ 2540228542](https://www.wikidata.org/w/index.php?title=Q167199&oldid=2540228542) | ro label; P300; P17; country P150 |
| City | IT-ROMA | [Q220 @ 2560816914](https://www.wikidata.org/w/index.php?title=Q220&oldid=2560816914) | it label; P17; country P36; P131 mapping |
| City | RO-BUCURESTI | [Q19660 @ 2563972624](https://www.wikidata.org/w/index.php?title=Q19660&oldid=2563972624) | ro label; P17; country P36/P150; shared entity for settlement and main-area roles |
| Supporting entity | Q18288160 | [Q18288160 @ 2563343641](https://www.wikidata.org/w/index.php?title=Q18288160&oldid=2563343641) | P131 Rome to Lazio relationship |
| Supporting entity | Q652 | [Q652 @ 2564827807](https://www.wikidata.org/w/index.php?title=Q652&oldid=2564827807) | P220 language code |
| Supporting entity | Q7913 | [Q7913 @ 2558338710](https://www.wikidata.org/w/index.php?title=Q7913&oldid=2558338710) | P220 language code |

## Acquisition Integrity

The following SHA-256 values identify the three raw API responses used during
review. Those full responses include unrelated statements and are not shipped;
the pinned entity revisions above support independent retrieval of the selected
facts. These hashes record acquisition integrity, not a copyright guarantee.

- sashiko-geography-wikidata-initial.json: de34823b6d5c03b7026ba830e4bfbec8e9d147ff14a10e61cfe7b8b033e9b885
- entities-1.json: 513c8859ea37c73008a6d052da730410bf66abd817a32cb7b3941f8af557fd99
- entities-2.json: 70cc476ac6b812008abcbf2e74da215a70dda3cfed583c8d5b49c51267fdeb17
