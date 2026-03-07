# Langues et localisation

[← Conventions](index.md)

## Langue par type de contenu

| Contenu | Langue |
| --- | --- |
| Code C# (identifiants, XML docs, commentaires `//`) | Anglais |
| Code TypeScript/React (identifiants, JSDoc, commentaires) | Anglais |
| `docs/**/*.md` | Français |
| Issues GitLab (titre, description, commentaires) | Français |
| Commits (Conventional Commits) | Français |
| `CLAUDE.md`, skills | Anglais |
| Fichiers de localisation (`Localization/**/*.json`) | 9 cultures |

## Diacritiques

**Toujours** utiliser les accents français corrects (é, è, ê, à, â, ù, û, ô, î,
ï, ç, œ) dans tout le contenu français (docs, issues, commits). Jamais dans le code.

## Localisation — 9 cultures (7 langues de base + 2 variantes régionales)

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

### Variantes régionales

| Code | Langue | Fallback |
| --- | --- | --- |
| `fr-CA` | Français — Canada | `fr` |
| `en-GB` | English — United Kingdom | `en` |

Les fichiers de variantes régionales ne contiennent que les **clés qui diffèrent**
de la langue de base. Le mécanisme de fallback natif .NET (`CultureInfo.Parent`)
résout automatiquement : `fr-CA` → `fr` → culture par défaut.

### Rétrocompatibilité

Les applications existantes qui utilisent `"fr"` ou `"en"` continuent de
fonctionner sans modification. Le code culture `"fr"` résout `fr.json`
(= Français France), `"en"` résout `en.json` (= English US).

Lors de l'ajout d'une clé de localisation, créez ou mettez à jour **les 7 fichiers
de base** (`en`, `fr`, `nl`, `de`, `es`, `it`, `pt`). Les fichiers de variantes
régionales (`fr-CA`, `en-GB`) ne doivent être modifiés que si la traduction
diffère de la langue de base.

### `ReferenceDataEntity`

Les 7 propriétés de label restent inchangées :
`LabelEn`, `LabelFr`, `LabelNl`, `LabelDe`, `LabelEs`, `LabelIt`, `LabelPt`.

La propriété `Label` utilise `TwoLetterISOLanguageName`, ce qui fait que `fr-CA`
résout `LabelFr` et `en-GB` résout `LabelEn`.
