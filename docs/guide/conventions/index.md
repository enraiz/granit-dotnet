# Conventions de codage

[← Index des guides](../index.md)

Ce guide centralise les conventions de nommage, le style de code, les bonnes pratiques
et le workflow pour les projets Digital Dynamics. Il fait référence : si une convention
existante diverge de ces documents, c'est ce guide qui prévaut.

## Conventions transversales

Ces conventions s'appliquent à **tous les projets** (backend et frontend).

| Document | Contenu |
| --- | --- |
| [Definition of Done](dod.md) | Vérifications bloquantes avant tout push ou MR |
| [Workflow Git](workflow.md) | Branching GitFlow, Conventional Commits, cibles MR, THIRD-PARTY-NOTICES |
| [Sécurité](securite.md) | Secrets, PII, chiffrement, Cloud Act |
| [Langues et localisation](langues.md) | Langues par type de contenu, 7 locales obligatoires |

## Conventions backend (.NET / C#)

| Document | Contenu |
| --- | --- |
| [Style et nommage](backend/style-et-nommage.md) | Nommage, `var`, style, organisation des fichiers, XML docs, commentaires |
| [Architecture](backend/architecture.md) | Structure des projets, modèle domaine, DI, endpoints API, HttpClient |
| [Structure des modules](backend/structure-modules.md) | Blueprint standard des dossiers d'un module Granit (Internal/, Domain/, Extensions/, etc.) |
| [Implémentation](backend/implementation.md) | Async, temps, nullabilité, collections, records, exceptions, logging, regex, observabilité, EF Core |

## Conventions frontend (TypeScript / React)

| Document | Contenu |
| --- | --- |
| [Style et nommage](frontend/style-et-nommage.md) | TypeScript strict, nommage, `type` vs `interface`, exports, imports, ESLint, organisation feature-based |
| [Composants](frontend/composants.md) | React, patterns TS, shadcn/ui, CVA, Storybook, accessibilité WCAG, design tokens, responsive, performance, sécurité HDS |
| [État et API](frontend/etat-et-api.md) | React Query, Query Factory, Orval, authentification Keycloak, routing, logging, i18n, formulaires Zod, tests Vitest |

## Voir aussi

| Section | Description |
| --- | --- |
| [Démarrage rapide](../demarrage-rapide/index.md) | Tutoriel pas-à-pas couvrant ces conventions en pratique |
| [Personas applicatifs](../personas-applicatifs.md) | Rôles fonctionnels et mapping Keycloak |
| [Framework](../../framework/index.md) | Documentation de référence de chaque module |
| [Patterns](../../patterns/index.md) | Catalogue des 39 design patterns identifiés dans Granit |
| [Tests](../../testing/index.md) | Conventions et bonnes pratiques de test |
| [Cookbook](../../cookbook/index.md) | Recettes pratiques combinant plusieurs modules |
