# Style et nommage — Frontend

[← Index des conventions](../index.md)

## TypeScript strict

Tous les projets frontend utilisent TypeScript en mode strict. Ces options sont
**obligatoires** dans `tsconfig.json` :

```json
{
  "compilerOptions": {
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noUncheckedIndexedAccess": true,
    "verbatimModuleSyntax": true,
    "target": "ES2022",
    "module": "ESNext"
  }
}
```

Règles strictes :

- Zéro **`any`** — utilisez `unknown` + type guard si le type est inconnu
- Zéro **`@ts-ignore`** / **`@ts-expect-error`** sans justification en commentaire
- **`satisfies`** pour valider un type tout en préservant l'inférence littérale
- **Dériver > déclarer** : préférez `z.infer`, `typeof`, `as const` plutôt que des types
  manuels dupliqués
- **Return types** : omettez quand l'inférence suffit, explicitez pour les fonctions à
  branches multiples ou le code bibliothèque (`@granit/*`)

```tsx
// ✅ satisfies — préserve l'inférence littérale
const config = {
  apiUrl: "/api/v1",
  timeout: 5000,
} satisfies ApiConfig;

// ✅ as const — type littéral immuable
const ROLES = ["admin", "user", "viewer"] as const;
type Role = (typeof ROLES)[number]; // "admin" | "user" | "viewer"

// ❌ Type manuel dupliqué
type Role = "admin" | "user" | "viewer"; // redondant si ROLES existe
```

## Conventions de nommage

| Élément | Convention | Exemple |
| --- | --- | --- |
| Composants | PascalCase | `PatientCard`, `AdminRoleGuard` |
| Hooks | `use` + PascalCase | `useAuth`, `usePatientList` |
| Utilitaires / fonctions | camelCase | `cn()`, `formatDate()` |
| Factories (framework) | `create` + PascalCase | `createLogger`, `createApiClient` |
| Constantes | UPPER_SNAKE_CASE | `API_BASE_URL`, `MAX_RETRY` |
| Types | PascalCase, pas de préfixe `I` | `Patient`, `AuthContextValue` |
| Interfaces | PascalCase, pas de préfixe `I` | `Patient`, `ApiResponse` |
| Props | `{Component}Props` | `PatientCardProps` |
| Enums | PascalCase + valeurs PascalCase | `LogLevel.Debug` |
| Fichiers composant | kebab-case `.tsx` | `patient-card.tsx` |
| Fichiers utilitaire | kebab-case `.ts` | `format-date.ts` |
| Fichiers de test | même nom + `.test.ts(x)` | `patient-card.test.tsx` |
| Fichiers Storybook | même nom + `.stories.tsx` | `patient-card.stories.tsx` |
| Dossiers | kebab-case | `patient-list/`, `auth-context/` |

## `type` vs `interface`

Utilisez **`type`** pour les props, unions, intersections et types utilitaires.
Utilisez **`interface`** pour les modèles de données (entités, DTOs) — extensible
avec `extends`.

Préférez **`z.infer<typeof schema>`** pour dériver les types des schémas Zod — jamais
de type TypeScript manuel dupliqué.

```tsx
// ✅ type pour les props et unions
type PatientCardProps = { name: string; age: number };
type Status = "active" | "inactive" | "archived";

// ✅ interface pour les modèles de données
interface Patient {
  id: string;
  name: string;
  birthDate: string;
}

// ✅ Inférence Zod — une seule source de vérité
const createPatientSchema = z.object({ name: z.string(), age: z.number() });
type CreatePatientDto = z.infer<typeof createPatientSchema>;
```

## Exports

Utilisez **named exports** (`export function`, `export const`) — jamais de
`export default`.

Exceptions :

- Fichiers Storybook (`export default meta` — requis par le format CSF)
- Pages route si le routeur l'exige

Raisons : refactoring automatique fiable, meilleure autocomplétion, détection des
imports inutilisés.

```tsx
// ✅ Named export
export function PatientCard({ name }: Readonly<PatientCardProps>) {
  return <div>{name}</div>;
}

// ❌ Default export (fragile au renommage)
export default function PatientCard() { /* ... */ }
```

## Imports

Ordre strict, enforced par ESLint `import/order` :

1. React / framework
2. Librairies tierces
3. `@granit/*` (packages framework)
4. `@/` (alias projet — chemins absolus)
5. `./` relatifs

Règles :

- **Ligne vide** entre chaque groupe
- **`import type { X }`** pour les imports de types uniquement (enforced par
  `verbatimModuleSyntax`)
- **Jamais d'import barrel** `from '.'` dans les fichiers internes (seulement dans
  `index.ts` public)

```tsx
// ✅ Imports ordonnés avec séparateurs
import { useState } from "react";

import { useQuery } from "@tanstack/react-query";

import { createLogger } from "@granit/logger";

import { cn } from "@/lib/utils";
import type { Patient } from "@/features/patients/types/patient";

import { PatientAvatar } from "./PatientAvatar";
```

## ESLint et Prettier

Règles clés ESLint :

- **`no-console`** : erreur — utilisez `@granit/logger` (`createLogger`)
- **`consistent-type-imports`** : enforced
- **`no-unused-vars`** + `noUnusedLocals` tsconfig
- **`import/no-restricted-paths`** : interdire les imports cross-feature (voir
  Organisation des fichiers)

Prettier : configuration partagée (semi, singleQuote, trailingComma).

**Zéro warnings ESLint** — corriger ou `eslint-disable-next-line` avec justification
en commentaire.

## Organisation des fichiers

### Structure feature-based

Organisez le code par domaine métier, pas par type de fichier :

```text
src/
├── app/                      ← point d'entrée, providers, router
│   ├── app.tsx
│   ├── provider.tsx
│   └── router.tsx
├── components/               ← composants partagés (wrappers, layout)
│   └── ui/                   ← shadcn/ui (JAMAIS modifier directement)
├── hooks/                    ← hooks partagés
├── lib/                      ← utilitaires (cn, axios, queryClient)
├── types/                    ← types partagés
├── features/                 ← le cœur du métier
│   └── patients/
│       ├── api/              ← query keys, hooks API (si pas Orval)
│       │   └── patient-queries.ts
│       ├── components/
│       │   ├── patient-card.tsx
│       │   ├── patient-card.test.tsx
│       │   └── patient-card.stories.tsx
│       ├── hooks/
│       │   └── use-patient-search.ts
│       ├── types/
│       │   └── patient.ts
│       └── index.ts          ← barrel export public
└── locales/                  ← fichiers i18n par langue
```

Principe : supprimer une feature = supprimer un dossier, zéro impact sur le reste.

### Flux unidirectionnel

Architecture inspirée de
[Bulletproof React](https://github.com/alan2207/bulletproof-react) :
`shared → features → app`. Jamais l'inverse.

| Couche | Peut importer de | Ne peut pas importer de |
| --- | --- | --- |
| `src/components/`, `src/hooks/`, `src/lib/`, `src/types/` | Rien (partagé) | `features/`, `app/` |
| `src/features/*` | Shared uniquement | Autre feature, `app/` |
| `src/app/` | Tout (compose les features) | — |

Enforcez via ESLint `import/no-restricted-paths` pour interdire les imports
cross-feature.

### Règles de fichiers

- Un composant par fichier
- Co-location : test + story à côté du composant
- `src/components/ui/` réservé aux composants shadcn/ui (jamais modifiés directement)
- `src/lib/` pour les utilitaires (`cn`, `axios`, `queryClient`)
- Évitez les barrel files (`index.ts`) internes — ils nuisent au tree shaking Vite

## Structure des packages `@granit/*`

Les packages du framework partagé (`granit-front`) suivent une organisation
interne standardisée, distincte de la structure feature-based des applications.

### Structure canonique

```text
packages/@granit/{package}/
├── src/
│   ├── types/                ← types, DTOs, enums (barrel index.ts)
│   │   ├── index.ts          ← ré-exporte tous les fichiers du dossier
│   │   ├── {domaine}.ts      ← types groupés par domaine
│   │   └── {enum}.ts         ← const enum-like (`as const`) + type Value
│   ├── api/                  ← fonctions HTTP (axios)
│   │   └── {package}-api.ts
│   ├── hooks/                ← hooks React custom
│   │   └── use-{feature}.ts
│   ├── components/           ← composants headless React
│   │   └── {component}.tsx
│   ├── providers/            ← Context providers + useConfig hooks
│   │   └── {package}-provider.tsx
│   ├── adapters/             ← adaptateurs CMP/tiers (cookies-klaro)
│   ├── utils/                ← utilitaires internes (si nécessaire)
│   ├── __tests__/            ← tous les tests centralisés
│   │   ├── setup.ts
│   │   └── {module}.test.ts(x)
│   └── index.ts              ← point d'entrée public (ré-exports)
├── package.json
└── tsconfig.json
```

### Règles

- **`types/`** est le seul dossier avec un barrel (`index.ts`). Les autres
  dossiers (`hooks/`, `api/`, etc.) n'ont pas de barrel — `src/index.ts` importe
  directement chaque fichier.
- **Extensions `.js`** dans toutes les imports internes (convention ESM, résolu
  par Vite vers `.ts`) :

  ```tsx
  // ✅ extension .js dans un package @granit/*
  import { fetchStatus } from '../api/workflow-api.js';
  import type { TransitionDto } from '../types/index.js';

  // ❌ extension .ts
  import { fetchStatus } from '../api/workflow-api.ts';
  ```

- **`import type`** pour les imports de types uniquement.
- **Tests** dans `__tests__/` centralisé (pas de co-location dans les packages
  framework — contrairement aux applications).
- **Nommage kebab-case** pour tous les fichiers et dossiers.
- **Enums TypeScript** : utiliser `as const` + type dérivé (pas d'`enum` natif) :

  ```tsx
  export const WorkflowLifecycleStatus = {
    Draft: 0,
    PendingReview: 1,
    Published: 2,
    Archived: 3,
  } as const;

  export type WorkflowLifecycleStatusValue =
    (typeof WorkflowLifecycleStatus)[keyof typeof WorkflowLifecycleStatus];
  ```

- **`src/index.ts`** : unique point d'entrée, exporte l'API publique complète.
  Les consumers (`guava-front`, `guava-admin`) importent uniquement depuis
  `@granit/{package}`.
- **Pas de dossiers vides** : n'inclure que les sous-dossiers pertinents
  (`@granit/auth` n'a pas `api/` ni `components/`, `@granit/cookies-klaro`
  utilise `adapters/` au lieu de `hooks/`).

### Tableau récapitulatif des packages

| Package | types/ | api/ | hooks/ | components/ | providers/ | adapters/ |
| --- | --- | --- | --- | --- | --- | --- |
| `@granit/querying` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `@granit/data-export` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `@granit/notifications` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `@granit/workflow` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `@granit/timeline` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `@granit/auth` | ✓ | — | ✓ | — | ✓ | — |
| `@granit/cookies` | ✓ | — | ✓ | — | ✓ | — |
| `@granit/cookies-klaro` | ✓ | — | — | — | — | ✓ |
| `@granit/ui` | — | — | — | ✓ (flat) | — | — |
| `@granit/ui-back` | — | — | — | ✓ (flat) | — | — |
| `@granit/utils` | — | — | — | — | — | — |
| `@granit/logger` | — | — | — | — | — | — |
| `@granit/api-client` | — | — | — | — | — | — |

## Commentaires et documentation

- Même règles que le backend : pas de `TODO` sans issue GitLab liée
- **TSDoc** (`/** */`) sur les hooks et utilitaires exportés — s'affiche dans
  l'autocomplétion IDE
- Pas de TSDoc sur les composants React simples (les Props typées servent de
  documentation)
- TSDoc sur les props complexes qui nécessitent une explication

```tsx
// ✅ TSDoc sur un hook custom
/**
 * Searches patients by name with debouncing.
 * @param debounceMs - Debounce delay in milliseconds (default: 300)
 */
export function usePatientSearch(debounceMs = 300) { /* ... */ }

// ❌ TSDoc inutile sur un composant trivial
/** Displays a patient card. */
export function PatientCard({ name }: Readonly<PatientCardProps>) { /* ... */ }
```

## Voir aussi

- [Composants](composants.md) — React, shadcn/ui, CVA, Storybook, accessibilité
- [État et API](etat-et-api.md) — React Query, Orval, authentification, i18n, tests
- [React TypeScript Cheatsheet](https://react-typescript-cheatsheet.netlify.app)
- [Total TypeScript Tips](https://www.totaltypescript.com/tips)
