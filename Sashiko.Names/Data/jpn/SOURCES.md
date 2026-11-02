# Japanese Name Sources

## Scope

This starter pool contains 274 male given names, 264 female given names, and
213 surnames, representing 749 distinct romanized strings across the core pools.
It is a curated set of attested names, not a list of the most popular names or
a model of Japanese population frequencies. Historical and modern names coexist.
Gender classifications are source classifications, not claims that a spelling
can only be used by one sex. Kanji spellings, meanings, and name readings are
outside this pool's scope.

## Structured Name Records

Names were checked against [Wikidata](https://www.wikidata.org/), retrieved on
2026-11-02 using the [Wikidata Query Service](https://query.wikidata.org/).
Wikidata's [copyright policy](https://www.wikidata.org/wiki/Wikidata:Copyright)
releases structured item data, including the labels and classification statements
used here, under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
CC0 permits copying, modification, and redistribution, including commercially,
without attribution or share-alike requirements. This source designation applies
to the structured name records; Wikimedia article prose has separate terms.
The pool copies no dictionary prose, biographies, fictional character lists, or
third-party fake-data pools. The provenance notes are retained for traceability.

Given-name candidates were retained only when an English label matched a record
with Japanese (`Q5287`) as its language (`P407`) and an explicit male-given-name
(`Q12308941`) or female-given-name (`Q11879590`) type (`P31`) for the corresponding
pool. Surname candidates were retained only when matching a Japanese-family-name
(`Q19914123`) record. Unclassified candidates were omitted rather than guessed.

Given-name source query:

```sparql
SELECT ?item ?name ?type WHERE {
  ?item wdt:P31/wdt:P279* wd:Q202444;
        wdt:P407 wd:Q5287;
        rdfs:label ?name;
        wdt:P31 ?type.
  FILTER(LANG(?name) = "en")
}
```

Surname source query:

```sparql
SELECT ?item ?name WHERE {
  ?item wdt:P31 wd:Q19914123;
        rdfs:label ?name.
  FILTER(LANG(?name) = "en")
}
```

The source query responses had these SHA-256 hashes. Live query results can
change as Wikidata is edited.

- Given names: `e174f59bfe6aafb4e832260615afb58d906efa6a71bcbbcd0575342112450589`
- Surnames: `0f73eb47eaf93058e7c3f1d5dc913096b489b91715d53f28e5eeb99e29132a28`

## Normalization and Generation

The curated candidates use conventional ASCII romanizations. Source labels were
Unicode-decomposed and combining marks removed before exact candidate matching,
so long-vowel macrons are omitted (for example, `Taro` and `Sato`). Vowel lengths
and distinct kanji readings may therefore collapse to the same string. Names
were not constructed by combining syllables. The maintenance tool trims,
deduplicates, and sorts the final arrays with .NET's invariant comparer.

Generation uses one given name and one shared surname in family-name-first
order. This follows the customary order described in the Japan Tourism Agency's
[Names of People and Entities](https://www.mlit.go.jp/kankocho/content/001996797.pdf)
guidance. The rules do not invent patronymics, double surnames, honorifics, or
gendered surname transformations. No separate unisex pool is sampled.
