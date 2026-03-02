# Composants — Frontend

[← Index des conventions](../index.md)

## Composants React — règles générales

- **Composants fonctionnels uniquement** — jamais de classes
- **Named export** (`export function`) — jamais de `export default` (sauf Storybook meta)
- **`Readonly<Props>`** pour toutes les props
- **Destructuration des props** dans la signature de la fonction
- **Zéro `useEffect` pour dériver un état** — utilisez `useMemo` ou calculez
  directement dans le render
- **Extraction** : si un composant dépasse ~100 lignes ou accumule les hooks, extrayez
  la logique dans un hook custom `use{Feature}Logic()`
- **Composition > héritage** : `children`, render props, slots via composants en props
- **Forwarding refs** avec `React.forwardRef` pour les composants réutilisables

```tsx
// ✅ Composant standard
export function PatientCard({ name, age }: Readonly<PatientCardProps>) {
  return (
    <div className="rounded-lg border p-4">
      <h3 className="font-semibold">{name}</h3>
      <p className="text-muted-foreground">{age} ans</p>
    </div>
  );
}

// ✅ Extraction de logique complexe dans un hook
export function PatientList() {
  const { patients, isLoading, searchTerm, setSearchTerm } = usePatientListLogic();
  // La vue reste simple — toute la logique est dans le hook
  return (/* ... */);
}
```

## Patterns TypeScript pour React

### Wrapper d'élément HTML natif

Utilisez `React.ComponentPropsWithoutRef<'element'>` pour étendre les props HTML
natives tout en ajoutant des props custom :

```tsx
type SubmitButtonProps = React.ComponentPropsWithoutRef<"button"> & {
  loading?: boolean;
};

export function SubmitButton({ loading, children, ...props }: Readonly<SubmitButtonProps>) {
  return (
    <Button type="submit" disabled={loading} {...props}>
      {loading ? <Spinner /> : children}
    </Button>
  );
}
```

### Extraction de props

Utilisez `React.ComponentProps<typeof Component>` pour réutiliser les types d'un
composant existant :

```tsx
type ButtonProps = React.ComponentProps<typeof Button>;
```

### Composants génériques

Pour les composants data-driven (listes, tableaux), utilisez un paramètre
de type générique :

```tsx
export function DataList<T>({ items, renderItem }: Readonly<{
  items: T[];
  renderItem: (item: T) => React.ReactNode;
}>) {
  return <ul>{items.map((item, i) => <li key={i}>{renderItem(item)}</li>)}</ul>;
}
```

### Props mutuellement exclusives

Utilisez des discriminated unions pour les variantes de composants :

```tsx
type NotificationProps =
  | { variant: "toast"; duration: number }
  | { variant: "banner"; dismissible: boolean };

export function Notification(props: Readonly<NotificationProps>) {
  if (props.variant === "toast") {
    // TypeScript sait que props.duration existe ici
    return <Toast duration={props.duration} />;
  }
  // TypeScript sait que props.dismissible existe ici
  return <Banner dismissible={props.dismissible} />;
}
```

### Polymorphic components

Pour les composants à rendu flexible (lien ou bouton selon le contexte) :

```tsx
type PolymorphicProps<E extends React.ElementType> = {
  as?: E;
} & Omit<React.ComponentPropsWithoutRef<E>, "as">;

export function Card<E extends React.ElementType = "div">({
  as, ...props
}: Readonly<PolymorphicProps<E>>) {
  const Component = as ?? "div";
  return <Component {...props} />;
}
```

Référence :
[React TypeScript Cheatsheet](https://react-typescript-cheatsheet.netlify.app/docs/advanced/patterns_by_usecase)

## shadcn/ui — règles strictes

**JAMAIS modifier les fichiers dans `src/components/ui/`** — ce sont des composants
générés par shadcn CLI.

Règles :

- Pour personnaliser : créez un **wrapper** dans `src/components/`
- Mettez à jour via `npx shadcn@latest add <component>` (écrase le fichier)
- Préservez l'attribut `data-slot` pour le testing et le styling

```tsx
// ✅ Wrapper au lieu de modifier Button.tsx
export function SubmitButton({ children, ...props }: Readonly<ButtonProps>) {
  return (
    <Button type="submit" variant="default" {...props}>
      {children}
    </Button>
  );
}

// ❌ Ne JAMAIS modifier src/components/ui/button.tsx
```

## CVA (class-variance-authority)

Utilisez CVA pour tout composant avec des variantes visuelles. C'est le pattern
standard pour gérer les variantes de style dans shadcn/ui.

```tsx
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground",
        destructive: "bg-destructive text-destructive-foreground",
        outline: "border border-input",
        success: "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200",
      },
    },
    defaultVariants: { variant: "default" },
  },
);

type BadgeProps = React.HTMLAttributes<HTMLDivElement> &
  VariantProps<typeof badgeVariants>;

export function Badge({ className, variant, ...props }: Readonly<BadgeProps>) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />;
}
```

## `cn()` et classes conditionnelles

Utilisez **toujours** `cn()` (`clsx` + `tailwind-merge`) pour combiner des classes
Tailwind. Jamais de template literals.

`cn()` est importé depuis `@/lib/utils`.

```tsx
// ✅ cn() gère les conflits Tailwind et les conditions
<div className={cn("p-4 rounded-lg", isActive && "bg-primary", className)} />

// ✅ Classes conditionnelles multiples
<button className={cn(
  "px-4 py-2 rounded-md font-medium transition-colors",
  variant === "primary" && "bg-primary text-primary-foreground",
  variant === "ghost" && "hover:bg-accent hover:text-accent-foreground",
  disabled && "opacity-50 cursor-not-allowed",
)} />

// ❌ Pas de template literals — conflits Tailwind non résolus
<div className={`p-4 rounded-lg ${isActive ? "bg-primary" : ""}`} />
```

## Design tokens et Tailwind CSS v4

### Tokens sémantiques

Utilisez **`@theme`** pour définir les tokens sémantiques (couleurs, espacements,
typographie). Chaque application a sa propre palette :

| Application | Palette | Couleur primaire |
| --- | --- | --- |
| guava-front | `brand-*` | Teal |
| guava-admin | `admin-*` | Indigo |

```css
/* ✅ Tokens sémantiques dans @theme */
@theme {
  --color-brand-50: oklch(0.97 0.02 175);
  --color-brand-500: oklch(0.55 0.15 175);
  --color-sidebar-bg: var(--color-brand-50);
}
```

### Règles Tailwind

- **Jamais de valeurs arbitraires** (`text-[#1a2b3c]`) — utilisez toujours un token
  sémantique
- **Mobile-first** : base = mobile, puis `sm:`, `md:`, `lg:`
- **Dark mode** : `next-themes` + classe `dark:` (guava-admin) — prévoyez les variantes
  dark pour tout composant

```tsx
// ✅ Mobile-first responsive
<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
  {patients.map((p) => <PatientCard key={p.id} patient={p} />)}
</div>

// ✅ Dark mode
<div className="bg-white text-gray-900 dark:bg-gray-900 dark:text-gray-100" />

// ❌ Valeur arbitraire
<div className="text-[#1a2b3c]" />
```

## Accessibilité (WCAG 2.1 AA — obligatoire HDS)

Le contexte HDS (Hébergeur de Données de Santé) impose la conformité WCAG 2.1
niveau AA. Ces règles sont **non-négociables** :

- **Tout composant interactif** doit avoir un label accessible (`aria-label`,
  `aria-labelledby`, ou `<label>`)
- **Contraste minimum** : 4.5:1 pour le texte, 3:1 pour les éléments graphiques
- **Navigation clavier** : tous les éléments interactifs atteignables au clavier
- **Focus visible** : jamais `outline: none` sans alternative visible
- **Éléments HTML sémantiques** préférés aux rôles ARIA (`<button>` au lieu de
  `<div role="button">`)
- **`@storybook/addon-a11y`** pour vérifier dans Storybook

```tsx
// ✅ Label accessible
<label htmlFor="patient-search">Rechercher un patient</label>
<input id="patient-search" type="search" aria-describedby="search-help" />
<p id="search-help" className="text-sm text-muted-foreground">
  Recherchez par nom, prénom ou numéro de dossier
</p>

// ✅ Bouton icône avec aria-label
<button aria-label="Fermer la modale" onClick={onClose}>
  <XIcon className="h-4 w-4" />
</button>

// ❌ Pas de label accessible
<input type="search" placeholder="Rechercher..." />
```

## Storybook

**Obligatoire** pour tout composant dans `src/components/` (partagés et UI).
**Recommandé** pour les composants feature complexes.

Conventions :

- Tags `['autodocs']` sur chaque story pour la documentation automatique
- Decorators de thème + i18n
- `play` functions pour les tests d'interaction visuels

```tsx
import type { Meta, StoryObj } from "@storybook/react";

import { PatientCard } from "./PatientCard";

const meta: Meta<typeof PatientCard> = {
  component: PatientCard,
  tags: ["autodocs"],
  decorators: [withTheme, withI18n],
  argTypes: {
    status: { control: "select", options: ["active", "inactive"] },
  },
};
export default meta;

type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: { name: "Jean Dupont", age: 42, status: "active" },
};

export const Inactive: Story = {
  args: { name: "Marie Martin", age: 35, status: "inactive" },
};
```

## Responsive et mobile

- **Mobile-first** : écrivez les styles pour mobile en premier, ajoutez les
  breakpoints pour les écrans plus grands
- **Breakpoints Tailwind** : `sm` (640px), `md` (768px), `lg` (1024px), `xl` (1280px)
- **Capacitor** (guava-front uniquement) : testez sur iOS/Android natif
- Pas de `window.innerWidth` — utilisez les media queries CSS / Tailwind

## Performance

- **Code splitting** : `React.lazy()` + `Suspense` pour charger les features à la
  demande

  ```tsx
  const PatientDashboard = React.lazy(() => import("./features/patients/PatientDashboard"));

  <Suspense fallback={<PageSkeleton />}>
    <PatientDashboard />
  </Suspense>
  ```

- **Images** : format WebP, lazy loading (`loading="lazy"`), pas d'import direct
  d'images lourdes
- **Mémoïsation** : `useMemo` / `useCallback` uniquement quand mesuré nécessaire (pas
  par défaut)
- **Barrel files** : évitez les `index.ts` qui ré-exportent tout — nuit au tree shaking
  Vite

## Sécurité (HDS)

- **Sanitisation HTML** : si rich text (Tiptap, Markdown), utilisez `DOMPurify` avant
  tout rendu via `dangerouslySetInnerHTML` — obligatoire en contexte HDS

  ```tsx
  import DOMPurify from "dompurify";

  // ✅ Sanitisation avant rendu
  <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(htmlContent) }} />

  // ❌ Rendu direct — risque XSS
  <div dangerouslySetInnerHTML={{ __html: htmlContent }} />
  ```

- **Pas de secrets côté client** — les tokens sont gérés par le SDK auth, jamais en
  `localStorage` brut
- **CSP (Content Security Policy)** : configurez les headers côté serveur
- **XSS** : jamais d'interpolation directe de données utilisateur dans le DOM

## Voir aussi

- [Style et nommage](style-et-nommage.md) — TypeScript strict, nommage, imports
- [État et API](etat-et-api.md) — React Query, Orval, authentification, i18n, tests
- [Bulletproof React](https://github.com/alan2207/bulletproof-react) — architecture
  de référence
