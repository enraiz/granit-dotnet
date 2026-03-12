# ADR-016 : Sylvan.Data.Excel — Lecture streaming de fichiers Excel

- **Statut** : Accepté
- **Date** : 2026-03-01
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.DataExchange.Excel)

## Contexte

Le module `Granit.DataExchange.Excel` nécessite un parser Excel capable de lire
des fichiers `.xlsx`, `.xlsb` et `.xls` en streaming, sans charger l'intégralité
du classeur en mémoire (modèle DOM). Les cas d'usage incluent : import de données
patients, réimport roundtrip, chargement initial depuis des fichiers legacy `.xls`.

Le framework utilise déjà **ClosedXML** pour la **génération** Excel
(`Granit.DocumentGeneration.Excel`). Pour la **lecture** (import), ClosedXML
est inadapté car il charge le DOM complet en mémoire (centaines de MB pour
100K+ lignes).

La bibliothèque doit supporter :

- **Streaming** : lecture forward-only sans chargement DOM
- **Formats** : `.xlsx`, `.xlsb`, `.xls` (fichiers legacy)
- **Performance** : 100K+ lignes avec empreinte mémoire minimale
- **Async** : support non-bloquant pour le pipeline DataExchange
- **Licence** : compatible usage commercial sans coût récurrent
- **Dépendances** : minimales (éviter les conflits avec ClosedXML)

## Décision

**Sylvan.Data.Excel** pour la lecture de fichiers Excel dans `Granit.DataExchange.Excel`.

> ClosedXML reste pour la **génération** (`Granit.DocumentGeneration.Excel`).

## Alternatives évaluées

### Option 1 : Sylvan.Data.Excel (retenue)

- **Licence** : MIT
- **Avantage** : zero dépendances transitives (pur managed), `DbDataReader`
  forward-only streaming, support `.xlsx`/`.xlsb`/`.xls`, plus faible empreinte
  mémoire de l'écosystème, async natif (`CreateAsync`, `ReadAsync`)
- **Maturité** : écosystème Sylvan (Csv à 3.1M downloads), version 0.5.2
- **Inconvénient** : communauté plus petite (867K downloads), pas
  d'`IAsyncEnumerable` natif (wrapping nécessaire)

### Option 2 : ClosedXML (déjà utilisé pour la génération)

- **Licence** : MIT
- **Avantage** : API riche, déjà dans le graphe de dépendances, même
  bibliothèque pour lecture et écriture
- **Inconvénient** : **modèle DOM** — charge tout le classeur en mémoire.
  Pour 100K lignes, consommation de centaines de MB (chaque `XLCell` a son
  propre `XLStyle`). Issues GitHub #86, #818, #1996 documentent cette
  limitation. Inadapté pour l'import de fichiers volumineux.

### Option 3 : MiniExcel

- **Licence** : Apache-2.0
- **Avantage** : API très simple (`Query<T>()` en une ligne), streaming
  SAX-like (~17 MB pour 1M lignes), `IAsyncEnumerable` natif (v2 preview)
- **Inconvénient** : dépendance transitive sur `DocumentFormat.OpenXml`
  (risque de conflit de version avec ClosedXML qui dépend du même package),
  pas de support `.xls` ni `.xlsb`, accès typé via dynamic/Dictionary
  (erreurs runtime possibles)

### Option 4 : ExcelDataReader

- **Licence** : MIT
- **Avantage** : le plus populaire (92M downloads), `IDataReader` forward-only,
  support `.xls`/`.xlsx`/`.xlsb`, battle-tested
- **Inconvénient** : **aucun support async** (pas d'`async`, pas de `ReadAsync`,
  pas d'`IAsyncEnumerable`), cible uniquement netstandard2.0 (pas d'optimisations
  modernes .NET), accesseurs typés basiques

### Option 5 : Open XML SDK (Microsoft)

- **Licence** : MIT
- **Avantage** : SDK officiel, mode SAX (`OpenXmlReader`) pour streaming ultime
- **Inconvénient** : API **très bas niveau** — manipulation directe des éléments
  XML, gestion manuelle des tables de chaînes partagées, interprétation des
  références de cellules, gestion des indices de styles. Centaines de lignes
  pour ce que les autres bibliothèques font en une ligne.

## Justification

| Critère | Sylvan.Data.Excel | ClosedXML | MiniExcel | ExcelDataReader | Open XML SDK |
| ------- | ----------------- | --------- | --------- | --------------- | ------------ |
| Licence | MIT | MIT | Apache-2.0 | MIT | MIT |
| Modèle de lecture | **Forward-only** | DOM (tout en RAM) | SAX streaming | Forward-only | DOM ou SAX |
| Mémoire 100K rows | **Très faible** | Centaines MB | ~17 MB | Faible-moyen | SAX: faible |
| Formats | .xlsx/.xlsb/.xls | .xlsx | .xlsx/.csv | .xlsx/.xlsb/.xls | .xlsx/.xlsb |
| Async | **Oui** | Non | Oui | **Non** | Non |
| Dépendances transitives | **Zéro** | OpenXml | OpenXml | Aucune | N/A |
| API | DbDataReader | Rich object model | dynamic/Dictionary | IDataReader | XML nodes |
| NuGet downloads | ~867K | ~45M | ~10.1M | ~92M | ~250M+ |

Le critère décisif est la combinaison **zero dépendances transitives** +
**streaming forward-only** + **support async** + **support .xls legacy**.

Sylvan.Data.Excel est le seul à cocher les quatre cases. Le point **zero
dépendances** est critique : `Granit.DocumentGeneration.Excel` tire déjà
`ClosedXML` → `DocumentFormat.OpenXml`. Ajouter MiniExcel apporterait une
seconde dépendance transitive sur `DocumentFormat.OpenXml` avec un risque
de conflit de version. Sylvan.Data.Excel évite ce problème entièrement.

## Conséquences

### Positives

- Plus faible empreinte mémoire pour la lecture de fichiers Excel en .NET
- Zero dépendances transitives (pas de conflit avec ClosedXML/OpenXml)
- Support des 3 formats courants : `.xlsx`, `.xlsb`, `.xls` (legacy)
- API `DbDataReader` familière et fortement typée
- Async natif (`CreateAsync`, `ReadAsync`)
- MIT : aucun coût, compatible usage commercial

### Négatives

- Communauté plus petite que ExcelDataReader ou MiniExcel
- Version 0.5.x (écosystème Sylvan stable mais versioning prudent)
- Pas d'`IAsyncEnumerable` natif — nécessite un wrapper dans
  `SylvanExcelFileParser` (trivial : boucle `while ReadAsync yield return`)
- Pas de support des fichiers protégés par mot de passe (cas rare pour l'import)
