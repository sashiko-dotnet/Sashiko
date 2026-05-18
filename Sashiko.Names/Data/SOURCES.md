# Name Data Sources

The embedded name pools are curated and normalized by the Sashiko maintenance tool. Name data should be maintained from public civil-registration, statistical, national name-list, or culturally reviewed name-pool sources.

The data is embedded directly in `Sashiko.Names`. The library should not depend on third-party fake-data libraries as source material or runtime dependencies.

When a language pool is expanded, the source used for that pool should be noted here or in a language-specific source note.

## Coverage Notes

The `arb`, `ben`, `por`, and `urd` pools were added to bring Sashiko.Names coverage up to the top 10 languages by total speaker count, using the library's ISO 639-3 identifiers. `arb` represents Standard Arabic for generation purposes.

Starter pools are Latin-readable and normalized for embedded generation. They should continue to be refined from public civil-registration, statistical, national name-list, or culturally reviewed sources as the package grows.
