# Gouvernance des traductions — Granit (framework)

[← Localisation](index.md)

Ce document définit les règles de gouvernance des traductions au niveau du
**framework Granit**. Pour les règles applicatives,
voir le [guide de gouvernance applicative](../../../guide/gouvernance-traductions.md).

## Principes fondamentaux

1. **Source unique** : les fichiers JSON embarqués sont la source de vérité.
   Les overrides DB (`ILocalizationOverrideStoreReader` / `ILocalizationOverrideStoreWriter`) permettent aux administrateurs
   de personnaliser les textes sans redéployer.
2. **Fallback natif** : le mécanisme `CultureInfo.Parent` de .NET gère les
   variantes régionales (`fr-CA` → `fr` → culture par défaut). Aucune logique
   de fallback manuelle n'est nécessaire.
3. **Convention de nommage** : chaque clé suit le format
   `{Ressource}:{Domaine}.{Clé}` ou `{Ressource}:{Domaine}:{Clé}`.
4. **Pas de traduction externe** : aucune API de traduction automatique (Google
   Translate, DeepL) n'est utilisée. Les textes médicaux et réglementaires
   exigent une traduction humaine vérifiée.

## Cultures supportées

### Langues de base (obligatoires)

Chaque package Granit contenant un dossier `Localization/` doit fournir les
**14 fichiers de base** : `en.json`, `fr.json`, `nl.json`, `de.json`, `es.json`,
`it.json`, `pt.json`, `zh.json`, `ja.json`, `pl.json`, `tr.json`, `ko.json`,
`sv.json`, `cs.json`.

### Variantes régionales

Les fichiers `fr-CA.json`, `en-GB.json` et `pt-BR.json` ne contiennent que les
**clés qui diffèrent** de la langue de base. Le fallback fait le reste.

| Base | Variantes | Défaut |
| --- | --- | --- |
| `en.json` (anglais américain) | `en-GB.json` | `en` = US |
| `fr.json` (français de France) | `fr-CA.json` | `fr` = France |
| `pt.json` (portugais) | `pt-BR.json` | `pt` = Portugal |

### `ReferenceDataEntity`

Les 14 propriétés `Label*` (`LabelEn`, `LabelFr`, `LabelNl`, `LabelDe`,
`LabelEs`, `LabelIt`, `LabelPt`, `LabelZh`, `LabelJa`, `LabelPl`, `LabelTr`,
`LabelKo`, `LabelSv`, `LabelCs`). La propriété `Label` utilise
`TwoLetterISOLanguageName` : `fr-CA` résout `LabelFr`, `en-GB` résout
`LabelEn`, `pt-BR` résout `LabelPt`.

## Convention de nommage des clés

### Format

```text
{Ressource}:{Domaine}.{Clé}
{Ressource}:{Domaine}:{SousDomaine}.{Clé}
```

### Exemples Granit

```text
Granit:EntityNotFound
Granit:ValidationError
Granit:Validation:InvalidEmail
PermissionGroup:Authorization
Permission:Authorization.Grants.Manage
BlobStorage:NotFound
Features:LimitExceeded
```

### Règles

- Le préfixe correspond au nom de la ressource
  (`[LocalizationResourceName("Granit")]`)
- Les `:` séparent les niveaux de namespace
- Les `.` séparent les sous-clés au sein d'un domaine
- Les clés sont en **PascalCase**
- Pas d'espaces ni de caractères spéciaux

## Cycle de vie d'une clé

### Ajout

1. Vérifier qu'une clé équivalente n'existe pas déjà dans le même package
2. Ajouter la clé dans les **14 fichiers de base** avec les traductions
3. Si la traduction diffère pour `fr-CA`, `en-GB` ou `pt-BR`, ajouter la clé dans
   le fichier régional correspondant
4. Le source generator (`Granit.Localization.SourceGenerator`) génère
   automatiquement la constante typée

### Modification

- Modifier la valeur dans le fichier JSON concerné
- Les overrides DB ne sont **pas** affectés (ils gardent la valeur
  personnalisée par l'admin)

### Suppression

1. Vérifier que la clé n'est plus référencée dans le code du package
   (recherche `grep -r "CléÀSupprimer"`)
2. Retirer la clé des 14 fichiers de base + fichiers régionaux
3. Les overrides DB orphelins restent en base mais ne sont plus servis
   (pas de nettoyage automatique — la clé n'existe plus dans les JSON)

## Résolution et priorité

L'ordre de résolution pour une clé donnée et une culture donnée est :

```text
1. Override DB pour la culture exacte (ex: fr-CA)
2. JSON pour la culture exacte (ex: fr-CA.json)
3. JSON pour la culture parente (ex: fr.json)
4. JSON pour la culture par défaut de la ressource
5. Héritage : clés des ressources parentes ([InheritResource])
6. Non trouvée → retourne la clé comme valeur
```

## Règles pour les packages Granit

### Chaque package est autonome

Un package Granit ne dépend pas d'un autre pour ses traductions. Si
`Granit.BlobStorage` a besoin d'un message d'erreur, il le définit dans
ses propres JSON, pas dans `Granit.Localization`.

### Les clés génériques restent dans `Granit.Localization`

Les messages communs à tous les modules (`EntityNotFound`, `ValidationError`,
`Unauthorized`, `Forbidden`, `InternalError`) vivent dans la ressource
`GranitLocalizationResource`. Les modules applicatifs en héritent via
`[InheritResource(typeof(GranitLocalizationResource))]`.

### Pas de clé applicative dans Granit

Les clés spécifiques à une application (ex: `MyApp:Patients:NotFound`) ne
doivent **jamais** être ajoutées dans un package Granit. Elles appartiennent
au backend de l'application.

## Vérification en CI

Les vérifications suivantes sont recommandées dans le pipeline CI :

### Cohérence des fichiers

Vérifier que chaque dossier `Localization/` contient les 17 fichiers attendus
(14 base + `fr-CA` + `en-GB` + `pt-BR`).

### Cohérence des clés

Vérifier que les 14 fichiers de base contiennent les mêmes clés. Une clé
présente dans `en.json` mais absente de `fr.json` est une erreur.

### Doublons de valeurs

Détecter les clés ayant exactement la même valeur dans un même fichier
(signe d'un doublon sémantique à fusionner).
