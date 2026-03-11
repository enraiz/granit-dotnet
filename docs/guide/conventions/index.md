# Conventions de codage

[← Index des guides](../index.md)

Ce guide centralise les conventions de nommage, le style de code, les bonnes pratiques
et le workflow pour les projets Granit. Il fait référence : si une convention
existante diverge de ces documents, c'est ce guide qui prévaut.

## Conventions transversales

Ces conventions s'appliquent à **tous les projets** (backend et frontend).

| Document | Contenu |
| --- | --- |
| [Definition of Done](dod.md) | Vérifications bloquantes avant tout push ou MR |
| [Workflow Git](workflow.md) | Branching GitFlow, Conventional Commits, cibles MR, THIRD-PARTY-NOTICES |
| [Sécurité](securite.md) | Secrets, PII, chiffrement, Cloud Act |
| [Langues et localisation](langues.md) | Langues par type de contenu, 7 locales obligatoires |

## Conventions .NET / C#

| Document | Contenu |
| --- | --- |
| [Style et nommage](style-et-nommage.md) | Nommage, `var`, style, organisation des fichiers, XML docs, commentaires |
| [Architecture](architecture.md) | Structure des projets, modèle domaine, DI, endpoints API, HttpClient |
| [Structure des modules](structure-modules.md) | Blueprint standard des dossiers d'un module Granit (Internal/, Domain/, Extensions/, etc.) |
| [Implémentation](implementation.md) | Async, temps, nullabilité, collections, records, exceptions, logging, regex, observabilité, EF Core |
| [API REST](api-rest.md) | Nommage JSON, formats, pagination (offset + cursor), filtres, tri, batch 207, dépréciation, cache |

## Conventions frontend (TypeScript / React)

> Les conventions frontend sont maintenues dans le dépôt
> [`granit-front`](https://gitlab.digitaldynamics.be/digital-dynamics/granit-front/-/tree/develop/docs/guide/conventions) :
> style et nommage, composants, état et API.

## Voir aussi

| Section | Description |
| --- | --- |
| [Démarrage rapide](../demarrage-rapide/index.md) | Tutoriel pas-à-pas couvrant ces conventions en pratique |
| [Personas applicatifs](../personas-applicatifs.md) | Rôles fonctionnels et mapping Keycloak |
| [Framework](../../framework/index.md) | Documentation de référence de chaque module |
| [Patterns](../../patterns/index.md) | Catalogue des 39 design patterns identifiés dans Granit |
| [Tests](../../testing/index.md) | Conventions et bonnes pratiques de test |
| [Cookbook](../../cookbook/index.md) | Recettes pratiques combinant plusieurs modules |
