# Gouvernance des traductions — Applications

[← Guides](index.md)

Ce document définit les règles de gouvernance des traductions au niveau
**applicatif** (toute application construite sur Granit). Pour les
règles du framework lui-même, voir la
[gouvernance Granit](../framework/utilities/localization/gouvernance.md).

## Architecture cible

```text
┌──────────────────────────────────────────────────────┐
│  JSON embarqués dans le backend applicatif            │
│  = valeurs par défaut, livrées avec le code          │
│  = couvrent 100 % des clés (backend + frontend)      │
├──────────────────────────────────────────────────────┤
│  Overrides DB (ILocalizationOverrideStore)            │
│  = modifiés par l'administrateur en production       │
│  = priorité sur les JSON embarqués                   │
├──────────────────────────────────────────────────────┤
│  GET /api/granit/localization?culture=fr              │
│  = fusion JSON + overrides DB                        │
│  = consommé par le backend ET les frontends          │
└──────────────────────────────────────────────────────┘
```

### Principe directeur

Le backend est la **source unique** de vérité pour toutes les traductions.
Les frontends React ne maintiennent **aucun fichier i18n local**. Ils
consomment l'endpoint `/api/granit/localization`.

## Convention de nommage des clés

### Format

```text
{App}:{Domaine}:{Portée}.{Clé}
```

### Catégories de clés

| Préfixe | Usage | Exemple |
| --- | --- | --- |
| `{App}:Common:*` | Textes partagés (front + admin + back) | `{App}:Common:Actions.Save` |
| `{App}:{Module}:*` | Textes spécifiques à un module métier | `{App}:Patients:List.Title` |
| `{App}:Admin:*` | Textes spécifiques à l'interface admin | `{App}:Admin:Users.Title` |
| `{App}:Front:*` | Textes spécifiques au front client | `{App}:Front:Nav.Dashboard` |

### Clés génériques obligatoirement dans `Common`

Les textes réutilisables vivent **exclusivement** dans `{App}:Common:*`.
Un développeur ne crée jamais une clé module-spécifique pour un texte
générique. Liste non exhaustive :

```text
{App}:Common:Actions.Save
{App}:Common:Actions.Cancel
{App}:Common:Actions.Delete
{App}:Common:Actions.Confirm
{App}:Common:Actions.Edit
{App}:Common:Actions.Create
{App}:Common:Actions.Search
{App}:Common:Actions.Close
{App}:Common:Actions.Back
{App}:Common:Actions.Next
{App}:Common:Actions.Previous
{App}:Common:Labels.Yes
{App}:Common:Labels.No
{App}:Common:Labels.Loading
{App}:Common:Labels.NoResults
{App}:Common:Labels.Required
{App}:Common:Confirm.Delete
{App}:Common:Confirm.Unsaved
```

### Règles de nommage

- **PascalCase** pour tous les segments
- Le `:` sépare les niveaux de namespace (App, Domaine, Portée)
- Le `.` sépare les sous-clés au sein d'une portée
- Pas d'espaces, pas de caractères spéciaux
- Pas de numéros de version dans les clés

## Structure des fichiers JSON dans le backend

```text
my-app-backend/
└── Localization/
    └── MyApp/
        ├── en.json         # Anglais US (base)
        ├── en-GB.json      # Anglais UK (overrides)
        ├── fr.json         # Français France (base)
        ├── fr-CA.json      # Français Canada (overrides)
        ├── nl.json
        ├── de.json
        ├── es.json
        ├── it.json
        └── pt.json
```

Les fichiers contiennent à la fois les clés backend **et** les clés frontend :

```json
{
  "culture": "fr",
  "texts": {
    "MyApp:Patients:NotFound": "Patient introuvable.",
    "MyApp:Patients:List.Title": "Liste des patients",
    "MyApp:Patients:Form.Save": "Enregistrer le patient",
    "MyApp:Common:Actions.Save": "Enregistrer",
    "MyApp:Common:Actions.Cancel": "Annuler",
    "MyApp:Admin:Users.Title": "Gestion des utilisateurs"
  }
}
```

## Consommation côté frontend

### Chargement initial

Le frontend appelle l'endpoint de localisation au démarrage de l'application.
Le package `granit-front` fournira un provider React dédié :

```tsx
<GranitLocalizationProvider endpoint="/api/granit/localization">
  <App />
</GranitLocalizationProvider>
```

### Cache côté client

- Les traductions sont stockées en `localStorage` avec un hash de version
- Au démarrage, le frontend envoie le hash ; si le serveur retourne `304`,
  les traductions locales sont réutilisées
- L'invalidation est automatique lors d'un override DB par l'admin

### Migration progressive

Pendant la phase de transition, les deux systèmes peuvent cohabiter :

```tsx
// Fichiers locaux (temporaire) = fallback
// API backend = priorité (écrase les locaux)
const backendTexts = await fetchLocalization(lang);
const localTexts = localFallback[lang];
i18n.addResourceBundle(lang, 'translation', { ...localTexts, ...backendTexts });
```

Au fur et à mesure que les clés migrent vers le backend, elles sont retirées
des fichiers React. Quand un fichier local est vide, il est supprimé.

## Workflow développeur

### Avant d'ajouter une clé

```text
1. J'ai besoin d'un texte traduit
         │
         ▼
2. Je cherche dans {App}:Common:*
   grep -ri "save" Localization/
         │
    ┌────┴─────────┐
    │ Existe       │ N'existe pas
    ▼              ▼
3. Réutiliser   4. Le texte est-il générique ?
                       │
                  ┌────┴────┐
                  │ Oui     │ Non
                  ▼         ▼
             Créer dans   Créer dans
             Common:*     {Module}:*
```

### Ajout d'une clé

1. Chercher si une clé équivalente existe (`grep` dans les JSON)
2. Déterminer la bonne catégorie (`Common`, module, `Admin`, `Front`)
3. Ajouter dans les **7 fichiers de base** avec les 7 traductions
4. Si la traduction diffère pour `fr-CA` ou `en-GB`, ajouter dans le
   fichier régional
5. Utiliser la clé dans le code (`.cs` ou `.tsx`)

### Suppression d'une clé

1. Retirer tous les usages dans le code (`.cs`, `.tsx`, `.ts`)
2. Retirer la clé des fichiers JSON (base + régionaux)
3. Vérifier qu'aucun autre module ne la référence

## Administration des traductions

### Ce que l'administrateur peut faire

- **Modifier** la valeur d'une clé existante (override DB)
- **Consulter** toutes les clés et leurs valeurs par culture
- **Réinitialiser** un override (revenir à la valeur JSON par défaut)

### Ce que l'administrateur ne peut PAS faire

- **Créer** une nouvelle clé (les clés sont définies dans le code)
- **Supprimer** une clé (les clés sont gérées par le cycle de vie du code)
- **Utiliser une traduction automatique** sans validation humaine
  (contexte médical/HDS)

## Vérification en CI

Le script `scripts/check-translations.sh` (dans le backend applicatif)
exécute 4 vérifications automatiques dans le stage `quality` du pipeline :

| Check | Niveau | Description |
| --- | --- | --- |
| Fichiers de base | Bloquant | Les 7 langues (en, fr, nl, de, es, it, pt) doivent exister par resource |
| Cohérence inter-langues | Bloquant | Toutes les langues de base doivent contenir les mêmes clés |
| Doublons sémantiques | Warning | Deux clés avec la même valeur dans une langue |
| Clés orphelines | Warning | Clés non référencées dans les fichiers `.cs` |

### Exécution locale

```bash
# Vérification standard (warnings non bloquants)
bash scripts/check-translations.sh

# Mode strict (warnings = erreurs, utile pour un nettoyage ponctuel)
bash scripts/check-translations.sh --strict
```

### Déclenchement CI

Le job `translations` se déclenche :

- sur les MR **si des fichiers `Localization/**/*.json` ont changé**
- systématiquement sur `develop` et `main`

### Limitations connues

- Les clés utilisées uniquement côté frontend (React) ne sont pas détectées
  par le check orphelines (qui analyse les `.cs`)
- Les doublons sémantiques sont souvent légitimes (ex: "Cancel" dans
  plusieurs contextes) — le check est informatif

## Calendrier de migration

| Phase | Contenu |
| --- | --- |
| 1 | Intégrer le provider `GranitLocalizationProvider` dans le frontend |
| 2 | Migrer les clés `Common:*` (actions, labels, confirmations) vers le backend |
| 3 | Migrer les clés frontend module par module |
| 4 | Migrer les clés admin |
| 5 | Supprimer les fichiers i18n locaux React |
| 6 | Activer les checks CI (orphelins, doublons, manquants) |
