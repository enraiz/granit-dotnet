# ADR-021 : Choix de TanStack Table pour le composant tableau React

- **Statut** : Accepté
- **Date** : 2026-03-03
- **Issue** : [#500](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/500)
- **Auteurs** : Equipe Digital Dynamics
- **Portee** : granit-front (`@granit/querying`)

## Contexte

Le module Granit.Querying necessite un composant tableau (data grid) cote frontend
React pour afficher les listes paginées, filtrées, triées et groupées des
applications consommatrices. Le design system du projet est basé sur **Shadcn/UI**
(Radix UI + Tailwind CSS).

Besoins fonctionnels identifies :

- Tri multi-colonnes (click header, server-side)
- Filtrage server-side piloté par une barre de recherche Odoo-like (omnibox +
  facettes)
- Pagination server-side (offset + cursor/keyset)
- Group By inline avec expand/collapse et agregats
- Masquer/afficher des colonnes (column visibility)
- Reordonner et redimensionner les colonnes
- Selection multi-lignes avec actions groupées (bulk actions)
- Virtualisation pour les grands datasets

Contraintes :

- Le design system est Shadcn/UI : tout composant doit etre compatible
- Licence MIT obligatoire (pas de paywall sur des features critiques)
- Bundle size raisonnable (application de gestion, pas un dashboard financier)

## Décision

**TanStack Table v8 (headless) + Shadcn/UI** est retenu comme composant tableau
pour le package `@granit/querying`. Les composants visuels sont construits avec
nos propres composants Shadcn/UI par-dessus la lib headless.

## Alternatives évaluées

### Option 1 : TanStack Table v8 + Shadcn/UI (retenue)

- **Licence** : MIT
- **Avantage** : 100% compatible Shadcn/UI, headless (controle total du rendu)
- **Avantage** : Bundle leger (~30-40 KB gzippe)
- **Avantage** : Server-side natif (`manualFiltering`, `manualSorting`,
  `manualPagination`)
- **Avantage** : Column visibility, ordering, pinning, resizing built-in
- **Avantage** : Row selection cross-page, grouping client-side built-in
- **Avantage** : Ecosysteme Shadcn riche (`shadcn/ui Data Table`, `tablecn`,
  `bazza/ui DataTableFilter`)
- **Avantage** : Permet de construire l'UX Odoo-like (omnibox, facettes)

### Option 2 : AG Grid Community

- **Licence** : MIT (Community), $999/dev/an (Enterprise)
- **Avantage** : Features out-of-the-box, virtualisation excellente, edition
  inline gratuite
- **Inconvenient** : Incompatible Shadcn/UI (impose son propre design system)
- **Inconvenient** : Features critiques derriere le paywall Enterprise
  (server-side row model, row grouping, aggregation, set filter, export Excel)
- **Inconvenient** : Bundle volumineux (~200-300 KB gzippe)
- **Inconvenient** : Barre de recherche Odoo-like impossible (conflit avec les
  filtres Excel-style integres dans les headers)

### Option 3 : Material React Table (MRT) v3

- **Licence** : MIT
- **Avantage** : Pret a l'emploi, CRUD editing built-in, meme API TanStack
  Table
- **Inconvenient** : Couple a Material UI, totalement incompatible avec
  Shadcn/UI
- **Inconvenient** : Barre de recherche Odoo-like impossible sans desactiver
  les filtres MUI

## Justification

| Critere | TanStack + Shadcn | AG Grid Community | MRT |
| ------- | ----------------- | ----------------- | --- |
| Compatibilite Shadcn/UI | 100% | Faible | 0% |
| Licence | MIT | MIT + $999/dev Enterprise | MIT |
| Bundle size | ~30-40 KB | ~200-300 KB | ~80-100 KB |
| Server-side sort/filter/page | Natif (manual flags) | Limite (Community) | Via TanStack |
| Column visibility | Built-in | Built-in | Built-in |
| Row selection + bulk | Built-in | Built-in | Built-in |
| Group By | Client built-in | Enterprise only | Via TanStack |
| Virtualisation | @tanstack/react-virtual | Built-in | @tanstack/react-virtual |
| UX Odoo-like possible | Oui | Non | Non |
| Edition inline | DIY | Gratuit | Built-in |
| Export Excel | DIY | Enterprise only | DIY |

Le critere bloquant est la **compatibilite Shadcn/UI** : AG Grid et MRT
imposent leur propre design system, ce qui cree un conflit irreconciliable
avec le design system du projet.

## Conséquences

### Positives

- Controle total du rendu : l'UX Odoo-like (omnibox, facettes, presets) est
  realisable
- Aucun cout de licence, aucune feature bloquee
- Le wrapper `@granit/querying` devient un asset reutilisable pour toutes les
  applications consommatrices
- Bundle optimise (< 40 KB pour le tableau)

### Négatives

- Investissement initial plus eleve pour construire les composants de base
  (amorti par le package `@granit/querying` reutilisable)
- Server-side grouping necessite un developpement custom (pas de
  `manualGrouping` natif dans TanStack Table v8)
- Edition inline et export Excel a developper si besoin (hors scope Querying)

## Conditions de réévaluation

Ce choix devrait etre reevalue si :

- TanStack Table v9 introduit des breaking changes majeurs (actuellement en
  alpha, API similaire attendue)
- AG Grid Community ajoute le server-side row model et le grouping en gratuit
- Un nouveau composant headless compatible Shadcn/UI emerge avec un meilleur
  support du server-side grouping

## Références

- [TanStack Table v8](https://tanstack.com/table/v8)
- [AG Grid Community vs Enterprise](https://www.ag-grid.com/react-data-grid/community-vs-enterprise/)
- [Material React Table](https://www.material-react-table.com/)
- [shadcn/ui Data Table](https://ui.shadcn.com/docs/components/data-table)
- [bazza/ui DataTableFilter](https://ui.bazza.dev/docs/data-table-filter)
- Issue : [#500 - Granit.Querying Epic](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/500)
