# État et API — Frontend

[← Index des conventions](../index.md)

## Architecture de l'état

Quatre catégories d'état, chacune avec son outil dédié :

| Catégorie | Outil | Exemples |
| --- | --- | --- |
| **État serveur** | React Query (TanStack Query) | Patients, utilisateurs, config |
| **État applicatif** | React Context | Auth, theme, locale |
| **État URL** | URL search params (`nuqs` ou natif) | Filtres, tri, pagination, onglet actif |
| **État local** | `useState` / `useReducer` | Ouverture modale, champ de saisie |

Règles strictes :

- **Jamais de duplication** : pas de `useState` qui copie des données React Query
- **URL comme source de vérité** pour tout état partageable par lien (filtres,
  pagination) — `?search=dupont&page=2` permet de partager le contexte
- **Pas de Redux, pas de Zustand** — React Context suffit pour l'état applicatif

```tsx
// ✅ React Query = source de vérité
const { data: patients, isLoading } = usePatients();

// ❌ Pas de copie locale
const [patients, setPatients] = useState<Patient[]>([]);
useEffect(() => {
  fetchPatients().then(setPatients);
}, []);
```

## React Query — conventions

### Query Factory

Centralisez les `queryKey` dans un objet factory par entité. Cela évite les bugs
d'invalidation et garantit la cohérence des clés :

```tsx
// ✅ features/patients/api/patient-queries.ts
export const patientKeys = {
  all: ["patients"] as const,
  lists: () => [...patientKeys.all, "list"] as const,
  list: (filters?: PatientFilter) => [...patientKeys.lists(), filters] as const,
  details: () => [...patientKeys.all, "detail"] as const,
  detail: (id: string) => [...patientKeys.details(), id] as const,
};
```

### Hooks custom

Un hook custom par opération, utilisant la factory :

```tsx
export function usePatients(filter?: PatientFilter) {
  return useQuery({
    queryKey: patientKeys.list(filter),
    queryFn: () => patientApi.getAll(filter),
  });
}

export function usePatient(id: string) {
  return useQuery({
    queryKey: patientKeys.detail(id),
    queryFn: () => patientApi.getById(id),
    enabled: Boolean(id),
  });
}

export function useCreatePatient() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: patientApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: patientKeys.lists() });
    },
  });
}
```

### Règles

- **`staleTime`** configuré par type de donnée (pas de valeur par défaut globale trop
  agressive)
- **`enabled`** pour les requêtes conditionnelles — pas de `useEffect` + `refetch`
- **Invalidation ciblée** après mutation — invalidez la liste, pas tout le cache

## Orval — génération du client API

**Orval** génère les hooks React Query à partir du schéma OpenAPI du backend.

- Fichier de config `orval.config.ts` à la racine du projet
- Génération dans `src/api/generated/` — **jamais modifier les fichiers générés**
- Regénérez après chaque changement d'API : `npx orval`
- Créez un wrapper custom si vous avez besoin de logique supplémentaire (au-dessus du
  hook généré)

```tsx
// ✅ Wrapper au-dessus du hook Orval généré
import { useGetPatients } from "@/api/generated";

export function usePatientList(filter?: PatientFilter) {
  const query = useGetPatients(filter);
  // Logique supplémentaire si nécessaire
  return query;
}
```

## Authentification

### Stack

- **Keycloak OIDC + PKCE** via `@granit/auth` (`createAuthContext`)
- Context `AuthProvider` en racine de l'application
- Tokens gérés automatiquement (refresh, expiration) — jamais manipuler les tokens
  manuellement

### Routing protégé

- `ProtectedRoute` pour les routes authentifiées
- `AdminRoleGuard` (guava-admin) pour le RBAC basé sur les rôles Keycloak
- Composition des guards par nesting de routes :

```tsx
// ✅ Composition des guards
<Route element={<ProtectedRoute />}>
  <Route element={<AdminRoleGuard requiredRole="admin" />}>
    <Route path="/users" element={<UserManagement />} />
  </Route>
  <Route path="/dashboard" element={<Dashboard />} />
</Route>
```

## Routing

- **React Router v7** avec `createBrowserRouter`
- Routes **lazy-loaded** : `React.lazy()` + `Suspense`
- Structure des routes reflète la structure des features
- `ProtectedRoute` wrapper pour l'authentification

```tsx
import { createBrowserRouter } from "react-router-dom";

const PatientDashboard = React.lazy(
  () => import("@/features/patients/PatientDashboard"),
);

export const router = createBrowserRouter([
  {
    element: <ProtectedRoute />,
    children: [
      {
        path: "/patients",
        element: (
          <Suspense fallback={<PageSkeleton />}>
            <PatientDashboard />
          </Suspense>
        ),
      },
    ],
  },
]);
```

## Logging

Utilisez **`@granit/logger`** (`createLogger`) — jamais `console.log`. La règle
ESLint `no-console` est configurée en erreur.

```tsx
import { createLogger } from "@granit/logger";

const logger = createLogger("PatientService");

// ✅ Logging structuré
logger.info("Patient created", { patientId, tenantId });
logger.error("Failed to fetch patients", { error, filter });

// ❌ console.log
console.log("Patient created", patientId);
```

### Transports

`LogTransport` strategy pattern :

- **Développement** : console transport (logs dans le navigateur)
- **Production** : HTTP transport (envoi vers le backend → observabilité Loki/Tempo)

## Internationalisation (i18n)

**7 locales obligatoires** : en, fr, nl, de, es, it, pt — même liste que le backend.

Framework : `react-i18next` + `i18next`.

Règles :

- Fichiers JSON par locale dans `src/locales/{lang}/`
- **Jamais de texte en dur** dans le JSX — toujours `t('key')`
- Clés de traduction : `namespace:section.key` (snake_case)

```tsx
// ✅ Traduction via hook
import { useTranslation } from "react-i18next";

export function PatientHeader() {
  const { t } = useTranslation("patients");
  return <h1>{t("patients:list.title")}</h1>;
}

// ❌ Texte en dur
export function PatientHeader() {
  return <h1>Liste des patients</h1>;
}
```

## Formulaires

### Stack

- **React Hook Form** pour la gestion des formulaires
- **Zod** pour la validation — le schéma est la source de vérité unique pour le type TS
- **`zodResolver`** pour connecter les deux
- Composants contrôlés shadcn/ui

### Pattern standard

**Jamais de type TypeScript dupliqué** — dérivez avec `z.infer<typeof schema>` :

```tsx
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

// ✅ Schéma Zod = source de vérité
const createPatientSchema = z.object({
  name: z.string().min(1, "Le nom est requis"),
  birthDate: z.string().date("Date de naissance invalide"),
  email: z.string().email("Email invalide"),
});

// ✅ Type dérivé — jamais de type manuel dupliqué
type CreatePatientDto = z.infer<typeof createPatientSchema>;

export function useCreatePatientForm() {
  return useForm<CreatePatientDto>({
    resolver: zodResolver(createPatientSchema),
    defaultValues: { name: "", birthDate: "", email: "" },
  });
}
```

## Tests

### Stack

- **Vitest** + **React Testing Library** + **jsdom**
- Coverage v8, seuil minimum **80 %**
- **msw** (Mock Service Worker) pour les tests d'intégration API

### Conventions

- Nommage : `describe("ComponentName")` + `it("should ...")` en anglais
- **Testez le comportement utilisateur** — pas l'implémentation interne
- **`data-slot`** ou **`data-testid`** pour les sélecteurs de test (jamais de
  sélecteurs CSS fragiles)
- Utilisez `screen.getByRole`, `screen.getByText`, `screen.getByLabelText` en priorité
  (plus proche de l'expérience utilisateur)

```tsx
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { PatientCard } from "./patient-card";

describe("PatientCard", () => {
  // ✅ Test de comportement
  it("should display patient name and age", () => {
    render(<PatientCard name="Jean Dupont" age={42} />);

    expect(screen.getByText("Jean Dupont")).toBeInTheDocument();
    expect(screen.getByText("42 ans")).toBeInTheDocument();
  });

  // ✅ Test d'interaction utilisateur
  it("should call onEdit when edit button is clicked", async () => {
    const onEdit = vi.fn();
    render(<PatientCard name="Jean Dupont" age={42} onEdit={onEdit} />);

    await userEvent.click(screen.getByRole("button", { name: /modifier/i }));

    expect(onEdit).toHaveBeenCalledOnce();
  });

  // ❌ Test d'implémentation
  it("should call useState", () => {
    // Ne testez JAMAIS les détails d'implémentation
  });
});
```

### Tests API avec msw

```tsx
import { http, HttpResponse } from "msw";
import { setupServer } from "msw/node";

const server = setupServer(
  http.get("/api/v1/patients", () =>
    HttpResponse.json([{ id: "1", name: "Jean Dupont" }]),
  ),
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

## Voir aussi

- [Style et nommage](style-et-nommage.md) — TypeScript strict, nommage, imports
- [Composants](composants.md) — React, shadcn/ui, CVA, Storybook, accessibilité
- [TanStack Query docs](https://tanstack.com/query/latest)
