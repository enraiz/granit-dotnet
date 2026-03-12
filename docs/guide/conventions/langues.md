# Langues et localisation

[← Conventions](index.md)

## Langue par type de contenu

| Contenu | Langue |
| --- | --- |
| Code C# (identifiants, XML docs, commentaires `//`) | Anglais |
| Code TypeScript/React (identifiants, JSDoc, commentaires) | Anglais |
| `docs/**/*.md` | Anglais (migration en cours, pages legacy en français) |
| Issues GitLab (titre, description, commentaires) | Français |
| Commits (Conventional Commits) | Anglais |
| `CLAUDE.md`, skills | Anglais |
| Fichiers de localisation (`Localization/**/*.json`) | 17 cultures |

## Diacritiques

**Toujours** utiliser les accents français corrects (é, è, ê, à, â, ù, û, ô, î,
ï, ç, œ) dans tout le contenu français (docs, issues, commits). Jamais dans le code.

## Localisation — 17 cultures (14 langues de base + 3 variantes régionales)

### Langues de base

| Code | Langue |
| --- | --- |
| `en` | English — United States (fallback) |
| `fr` | Français — France |
| `nl` | Nederlands |
| `de` | Deutsch |
| `es` | Español |
| `it` | Italiano |
| `pt` | Português |
| `zh` | 中文 (Chinese) |
| `ja` | 日本語 (Japanese) |
| `pl` | Polski |
| `tr` | Türkçe |
| `ko` | 한국어 (Korean) |
| `sv` | Svenska |
| `cs` | Čeština |

### Variantes régionales

| Code | Langue | Fallback |
| --- | --- | --- |
| `fr-CA` | Français — Canada | `fr` |
| `en-GB` | English — United Kingdom | `en` |
| `pt-BR` | Português — Brasil | `pt` |

Les fichiers de variantes régionales ne contiennent que les **clés qui diffèrent**
de la langue de base. Le mécanisme de fallback natif .NET (`CultureInfo.Parent`)
résout automatiquement : `fr-CA` → `fr` → culture par défaut.

### Rétrocompatibilité

Les applications existantes qui utilisent `"fr"` ou `"en"` continuent de
fonctionner sans modification. Le code culture `"fr"` résout `fr.json`
(= Français France), `"en"` résout `en.json` (= English US).

Lors de l'ajout d'une clé de localisation, créez ou mettez à jour **les 14 fichiers
de base** (`en`, `fr`, `nl`, `de`, `es`, `it`, `pt`, `zh`, `ja`, `pl`, `tr`, `ko`,
`sv`, `cs`). Les fichiers de variantes régionales (`fr-CA`, `en-GB`, `pt-BR`) ne
doivent être modifiés que si la traduction diffère de la langue de base.

### `ReferenceDataEntity`

Les 14 propriétés de label :
`LabelEn`, `LabelFr`, `LabelNl`, `LabelDe`, `LabelEs`, `LabelIt`, `LabelPt`,
`LabelZh`, `LabelJa`, `LabelPl`, `LabelTr`, `LabelKo`, `LabelSv`, `LabelCs`.

La propriété `Label` utilise `TwoLetterISOLanguageName`, ce qui fait que `fr-CA`
résout `LabelFr`, `en-GB` résout `LabelEn` et `pt-BR` résout `LabelPt`.
