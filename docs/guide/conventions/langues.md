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
| Fichiers de localisation (`Localization/**/*.json`) | 7 langues |

## Diacritiques

**Toujours** utiliser les accents français corrects (é, è, ê, à, â, ù, û, ô, î,
ï, ç, œ) dans tout le contenu français (docs, issues, commits). Jamais dans le code.

## Localisation — 7 langues obligatoires

| Code | Langue |
| --- | --- |
| `en` | English (fallback) |
| `fr` | Français |
| `nl` | Nederlands |
| `de` | Deutsch |
| `es` | Español |
| `it` | Italiano |
| `pt` | Português |

Lors de l'ajout d'une clé de localisation, créez ou mettez à jour **les 7 fichiers**.

Cette liste s'applique aussi aux entités `ReferenceDataEntity` :
`LabelEn`, `LabelFr`, `LabelNl`, `LabelDe`, `LabelEs`, `LabelIt`, `LabelPt`.
