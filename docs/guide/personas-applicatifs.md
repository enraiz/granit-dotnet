# Personas applicatifs

[← Index des guides](index.md)

Personas orientés application, complémentaires au [référentiel canonical
(governance-compliance)](https://gitlab.digitaldynamics.be/digital-dynamics/governance-compliance/-/blob/main/docs/03-organization/ORG-05-PERSONAS.md)
qui couvre les personas techniques (Niveau 1) et gouvernance (Niveau 2).

Ce document définit les **personas applicatifs (Niveau 4)** — les rôles fonctionnels
que les utilisateurs finaux endossent au sein d'une application construite avec Granit.
Ils complètent les personas utilisateurs finaux (Niveau 3 : Utilisateur, Professionnel
de santé, Product Owner) sans les remplacer.

## Règles d'utilisation

1. **Utiliser ces personas** dans les user stories applicatives
   (`En tant que [persona]`)
2. **Un persona applicatif peut être cumulé** avec un persona de Niveau 3
   (ex : un Professionnel de santé peut aussi être Approbateur)
3. **Ne jamais inventer** un nouveau persona sans validation et ajout à ce
   référentiel
4. **Le mapping vers les rôles Keycloak** est indicatif — chaque application
   définit ses propres rôles dans son realm

## Visiteur

| Champ | Valeur |
| --- | --- |
| **Nom canonique** | Visiteur |
| **Description** | Personne non authentifiée qui accède aux ressources publiques de l'application |
| **Responsabilités** | Consulter les pages publiques, s'inscrire, se connecter |
| **Authentification** | Aucune (anonyme) |
| **Rôle Keycloak type** | — (pas de token) |
| **Permissions Granit** | Aucune — uniquement les endpoints décorés `.AllowAnonymous()` |
| **Modules concernés** | `Granit.Cookies` (consentement), `Granit.Cors` |

## Utilisateur authentifié

| Champ | Valeur |
| --- | --- |
| **Nom canonique** | Utilisateur authentifié |
| **Description** | Personne connectée avec un compte valide, sans rôle spécifique |
| **Responsabilités** | Accéder aux fonctionnalités de base, consulter ses données, gérer son profil |
| **Authentification** | JWT Bearer (Keycloak) |
| **Rôle Keycloak type** | `user` (rôle par défaut à la création du compte) |
| **Permissions Granit** | Endpoints protégés par `.RequireAuthorization()` sans policy spécifique |
| **Modules concernés** | `Granit.Authentication.JwtBearer`, `Granit.Security` (`ICurrentUserService`) |
| **Relation Niveau 3** | Raffine le persona *Utilisateur* du référentiel canonical |

## Administrateur d'application

| Champ | Valeur |
| --- | --- |
| **Nom canonique** | Administrateur d'application |
| **Description** | Gère la configuration fonctionnelle de l'application, les données de référence et les utilisateurs |
| **Responsabilités** | Paramétrage, gestion des données de référence, feature flags, gestion des utilisateurs, imports de données |
| **Authentification** | JWT Bearer (Keycloak) |
| **Rôle Keycloak type** | `admin` (configurable via `GranitAuthorizationOptions.AdminRoles`) |
| **Permissions Granit** | `ReferenceData.Manage`, `Settings.Manage`, `Features.Manage`, `DataImport.Admin`, `BackgroundJobs.Admin`, `Localization.Overrides.Manage` |
| **Modules concernés** | `Granit.ReferenceData.Endpoints`, `Granit.Settings`, `Granit.Features`, `Granit.DataImport.Endpoints`, `Granit.BackgroundJobs.Endpoints`, `Granit.Localization.Endpoints` |

## Approbateur

| Champ | Valeur |
| --- | --- |
| **Nom canonique** | Approbateur |
| **Description** | Valide ou rejette les transitions de workflow (publication, validation documentaire, changements de statut) |
| **Responsabilités** | Examiner les éléments en attente de validation, approuver ou rejeter avec commentaire, suivre le backlog d'approbation |
| **Authentification** | JWT Bearer (Keycloak) |
| **Rôle Keycloak type** | Variable — tout rôle Keycloak possédant la permission requise par la transition (résolu dynamiquement par `IApproverResolver`) |
| **Permissions Granit** | Dépend du workflow — ex. `document.publish`, `import.validate` |
| **Modules concernés** | `Granit.Workflow`, `Granit.Workflow.Notifications` (`KeycloakApproverResolver`), `Granit.Notifications` (notification d'approbation en attente) |
| **Résolution** | L'approbateur n'est pas un rôle Keycloak fixe. Le `KeycloakApproverResolver` interroge dynamiquement Keycloak pour trouver les utilisateurs dont le rôle possède la permission requise par la transition. |

## Gestionnaire de contenu

| Champ | Valeur |
| --- | --- |
| **Nom canonique** | Gestionnaire de contenu |
| **Description** | Crée, édite et gère les documents, templates et contenus de l'application |
| **Responsabilités** | Rédaction de templates, génération de documents (PDF, Excel), gestion des modèles de notification, import de données |
| **Authentification** | JWT Bearer (Keycloak) |
| **Rôle Keycloak type** | `content-manager` |
| **Permissions Granit** | `Template.Manage`, `Document.Generate`, `DataImport.Execute`, `Notification.Template.Manage` |
| **Modules concernés** | `Granit.Templating`, `Granit.Templating.EntityFrameworkCore`, `Granit.DocumentGeneration`, `Granit.DataImport`, `Granit.Notifications` |
| **Workflow** | Les contenus publiables passent par le workflow Draft → PendingReview → Published. Le gestionnaire de contenu crée le brouillon ; l'Approbateur valide la publication. |

## Matrice persona × module Granit

| Module | Visiteur | Authentifié | Admin | Approbateur | Gestionnaire |
| --- | --- | --- | --- | --- | --- |
| Authentication / Security | — | Oui | Oui | Oui | Oui |
| Authorization | — | Lecture | Admin | Oui | Oui |
| ReferenceData | Lecture\* | Lecture | CRUD | — | Lecture |
| Settings | — | — | CRUD | — | — |
| Features | — | — | CRUD | — | — |
| DataImport | — | — | Admin | Validation | Exécution |
| BackgroundJobs | — | — | Admin | — | — |
| Localization | — | — | Overrides | — | — |
| Templating | — | — | — | — | CRUD |
| DocumentGeneration | — | Lecture | — | — | Génération |
| Workflow | — | Lecture | Config | Approve/Reject | Draft/Submit |
| Notifications | — | Réception | Config | Réception | Config templates |
| BlobStorage | — | Upload/Download | Admin | — | Upload |
| Timeline | — | Ses actions | Toutes | Ses actions | Ses actions |

\* Lecture des données de référence publiques uniquement (si endpoint `.AllowAnonymous()`).

## Mapping persona canonical → persona applicatif

Les personas de Niveau 3 (référentiel canonical) se mappent ainsi aux personas
applicatifs :

| Persona Niveau 3 | Personas applicatifs typiques |
| --- | --- |
| Utilisateur | Visiteur → Utilisateur authentifié |
| Professionnel de santé | Utilisateur authentifié + Gestionnaire de contenu |
| Product Owner | Administrateur d'application (configuration fonctionnelle) |

## Historique des modifications

| Date | Version | Auteur | Description |
| --- | --- | --- | --- |
| 2026-03-02 | 1.0 | JF + Claude | Création initiale — 5 personas applicatifs |
