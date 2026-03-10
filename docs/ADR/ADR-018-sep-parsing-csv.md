# ADR-018 : Sep — Parsing CSV haute performance

- **Statut** : Accepté
- **Date** : 2026-03-01
- **Issue** : [#475](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/475)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.DataExchange.Csv)

## Contexte

Le module `Granit.DataExchange.Csv` nécessite un parser CSV capable de traiter
des fichiers de 100 000+ lignes en streaming, sans charger l'intégralité du
fichier en mémoire. Les cas d'usage incluent : import de données patients,
réimport roundtrip, chargement initial de référentiels.

La bibliothèque doit supporter :

- **Streaming** : `IAsyncEnumerable` natif pour le pipeline DataExchange
- **Performance** : fichiers volumineux sans dégradation
- **Encodages** : UTF-8, UTF-8 BOM, Latin-1, Windows-1252
- **RFC 4180** : champs quotés, séparateurs configurables
- **Licence** : compatible usage commercial sans coût récurrent
- **Target** : .NET 10 explicite

## Décision

**Sep** (nietras) pour le parsing CSV dans `Granit.DataExchange.Csv`.

## Alternatives évaluées

### Option 1 : Sep (retenue)

- **Licence** : MIT
- **Avantage** : zero-allocation après warmup, vectorisation SIMD (AVX-512,
  SSE, ARM NEON), target net10.0 explicite, `IAsyncEnumerable` natif (.NET 9+),
  `Span<T>` / `ISpanParsable<T>`, 9-35x plus rapide que CsvHelper, AOT-compatible
- **Maturité** : releases actives en 2025 (0.9.0 à 0.12.2), tests extensifs
- **Inconvénient** : API plus bas niveau (Span-orientée), numéro de version 0.x
  (versioning conservateur de l'auteur, pas signe d'instabilité)

### Option 2 : CsvHelper

- **Licence** : MS-PL / Apache-2.0
- **Avantage** : standard de facto (508M downloads NuGet), API haut niveau
  (`GetRecordsAsync<T>()`, `ClassMap`, `TypeConverter`), `IAsyncEnumerable`
  natif, documentation excellente, callbacks d'erreur (`BadDataFound`)
- **Inconvénient** : allocation d'une `string` par colonne (significatif à
  100K+ lignes), pas de vectorisation SIMD, pas de target net10.0 explicite
  (via netstandard2.0), 9-35x plus lent que Sep

### Option 3 : Sylvan.Data.Csv

- **Licence** : MIT
- **Avantage** : 2-3x plus rapide que CsvHelper, API `DbDataReader` familière,
  auto-détection du délimiteur, mode Lax pour données malformées
- **Inconvénient** : pas de target net10.0 explicite, pas de SIMD, pas
  d'`IAsyncEnumerable` (uniquement `ReadAsync()`), performance intermédiaire
  sans avantage décisif sur Sep ou CsvHelper

### Option 4 : RecordParser

- **Licence** : MIT
- **Avantage** : near-zero allocation via expression trees, `Span<char>`
- **Inconvénient** : dernière release novembre 2023 (18+ mois), 116K downloads,
  pas de target .NET 8/9/10, pas d'`IAsyncEnumerable`, documentation minimale,
  maintenance stagnante

## Justification

| Critère | Sep | CsvHelper | Sylvan.Data.Csv | RecordParser |
| ------- | --- | --------- | --------------- | ------------ |
| Licence | MIT | MS-PL/Apache-2.0 | MIT | MIT |
| Performance vs CsvHelper | **9-35x** | 1x (baseline) | 2-3x | ~2x |
| Zero-allocation | **Oui** | Non | Low-alloc | Near-zero |
| SIMD (AVX-512/NEON) | **Oui** | Non | Non | Non |
| Target net10.0 | **Oui** | Non (netstandard) | Non (net6.0) | Non |
| IAsyncEnumerable | **Oui** (.NET 9+) | Oui | Non | Non |
| Span/Memory API | **Oui** | Non | Partiel | Oui |
| NuGet downloads | ~1.4M | ~508M | ~3.1M | ~116K |
| Maintenance active | **Oui** (2025) | Oui | Oui | Non (2023) |
| AOT/Trimming | **Oui** | Partiel | Partiel | Inconnu |
| Ergonomie API | Moyen | Excellent | Bon | Faible |

Le critère décisif est la **performance streaming** pour les fichiers volumineux
(100K+ lignes). Sep est 9-35x plus rapide que CsvHelper grâce à la vectorisation
SIMD et l'absence d'allocations. L'API bas niveau n'est pas un inconvénient car
elle est encapsulée derrière l'interface `IFileParser` — les consommateurs ne
voient jamais l'API Sep directement.

## Conséquences

### Positives

- Parsing CSV le plus rapide de l'écosystème .NET (SIMD vectorisé)
- Zero-allocation : pas de pression GC sur les imports volumineux
- Target net10.0 explicite : optimisations du runtime exploitées
- `IAsyncEnumerable` natif : intégration naturelle avec le pipeline DataExchange
- MIT : aucun coût, compatible usage commercial
- AOT-compatible : pas de réflexion à l'exécution

### Négatives

- API Span-orientée plus verbeuse que CsvHelper pour le code interne
- Version 0.x (bien que stable et activement maintenue)
- Communauté plus petite que CsvHelper (1.4M vs 508M downloads)
- Pas de `ClassMap` natif — le mapping est fait par `IDataMapper<T>` (par design)
- Pas de callbacks d'erreur intégrés (`BadDataFound`) — gestion via
  `try/catch` dans l'implémentation `SepCsvFileParser`
