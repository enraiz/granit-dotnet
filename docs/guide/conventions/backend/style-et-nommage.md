# Style et nommage (.NET / C#)

[← Conventions](../index.md)

## Conventions de nommage

| Élément | Convention | Exemple |
| --- | --- | --- |
| Types | PascalCase, `sealed` par défaut | `sealed class AuditedEntityInterceptor` |
| Interfaces | `I` + PascalCase | `ICurrentUserService` |
| Méthodes | PascalCase, suffixe `Async` | `EncryptAsync()` |
| Propriétés | PascalCase | `CreatedAt`, `TenantId?` |
| Champs privés | `_camelCase` (underscore) | `_currentUserService` |
| Constantes | PascalCase (pas UPPER\_SNAKE) | `SectionName` |
| Paramètres / locales | camelCase | `string errorCode` |
| Génériques | `T` ou `TPréfixe` | `TModule`, `TEntity` |
| Options | suffixe `Options`, `sealed` | `sealed class VaultOptions` |
| Extensions DI | `Add*` / `Use*` | `AddGranitTiming()` |
| Modules | suffixe `Module` | `GranitTimingModule` |
| Enums | PascalCase, valeurs PascalCase | `SequentialGuidType.AtEnd` |
| DTOs endpoint | `[Module][Concept][Suffixe]` | `WorkflowTransitionRequest` |

## Nommage des DTOs exposés par les endpoints (OpenAPI)

Dans un schéma OpenAPI, les namespaces C# sont aplatis : seul le **nom court** du
type apparaît. Deux modules exposant un `AttachmentInfo` produisent un conflit.

### Règle : préfixer par le contexte métier

Tout type public utilisé comme paramètre ou retour d'un endpoint (y compris les
types imbriqués dans ces DTOs) **doit** porter un préfixe identifiant son module.

| ❌ Trop générique | ✅ Préfixé | Module |
| --- | --- | --- |
| `AttachmentInfo` | `TimelineAttachmentInfo` | Timeline |
| `ColumnMapping` | `ImportColumnMapping` | DataExchange |
| `FieldMetadata` | `ImportFieldMetadata` | DataExchange |
| `TransitionRequest` | `WorkflowTransitionRequest` | Workflow |

### Exceptions

Les types **transversaux par design** (utilisés par plusieurs modules comme
infrastructure partagée) n'ont pas besoin de préfixe module :

- `PagedResult<T>` (Granit.Querying) — un seul type partagé, pas de conflit
- `ProblemDetails` (framework ASP.NET) — standard RFC 7807

### Quand appliquer

- À la **création** de tout nouveau record/class dans un package `*.Endpoints`
  ou dans un package de base dont les types remontent dans les endpoints
- Lors d'une **revue de code** (MR) : vérifier que les noms de DTOs ne sont pas
  ambigus hors de leur namespace

## Style de code

### Utilisation de `var` — règle Microsoft

Utilisez `var` quand le type est évident à droite de l'assignation ; type explicite sinon.

```csharp
var stream = File.OpenRead("data.csv");           // ✅ type évident (FileStream)
var users = new Dictionary<int, User>();           // ✅ type évident (new)
ImportResult result = _service.ImportAsync(data);  // ✅ type explicite (pas évident)
var result = _service.ImportAsync(data);           // ❌ quel type ?
```

### Autres règles de style

- **Expression body** pour les méthodes à une instruction (`IDE0022`)

  ```csharp
  public IReadOnlyList<Type> GetModuleTypes() =>
      [.. _modules.Select(m => m.ModuleType)];
  ```

- **Accolades obligatoires** même pour les blocs d'une seule ligne

  ```csharp
  // ✅
  if (context is null)
  {
      return;
  }

  // ❌
  if (context is null) return;
  ```

- **Classes `sealed` par défaut** — ne pas sceller uniquement quand l'héritage
  est explicitement prévu
- **File-scoped namespaces** — `namespace Granit.Vault.Services;`
- **Expressions de collection C# 12+** — `[]` pour les listes vides,
  `[.. enumerable]` pour le spread
- **Pattern matching** — `is null`, `is not null` (jamais `== null`)
- **Target-typed `new()`** — quand le type est explicite à gauche

  ```csharp
  VaultOptions options = new();
  ```

### Zéro warnings

Le projet doit compiler sans aucun warning. Les warnings (nullable, obsolescence,
performance) sont des bugs latents. Les corriger ou les supprimer explicitement avec
`#pragma warning disable` accompagné d'une justification en commentaire.

## Organisation des fichiers

### Ordre des `using`

System → Microsoft → Projet/Tiers (enforced par `.editorconfig`) :

```csharp
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Granit.Vault.Options;
using VaultSharp;
```

### Structure type d'un fichier

1. Using statements
2. Namespace (file-scoped)
3. Documentation XML du type
4. Déclaration du type
5. Champs privés (`readonly`)
6. Constructeur (ou constructeur primaire)
7. Propriétés publiques
8. Méthodes publiques
9. Méthodes privées
10. Types imbriqués (en dernier)

### Règles générales

- **Un type par fichier**, nom du fichier = nom du type
- Exemple concret : `TransitEncryptionService.cs` dans `src/Granit.Vault/Services/`

## Documentation XML

- **Obligatoire** sur tous les types et membres publics
- `<summary>` bref (1 ligne), `<remarks>` pour le détail
- `<inheritdoc/>` pour les implémentations d'interface
- `<param>` et `<returns>` pour les méthodes publiques
- Contexte HDS/RGPD dans `<remarks>` quand pertinent

```csharp
/// <summary>
/// Exception representing a violated business rule.
/// Maps to <c>400 Bad Request</c>.
/// </summary>
/// <remarks>
/// Implements <see cref="IUserFriendlyException"/>: the message is safe to
/// expose to clients.
/// Implements <see cref="IHasErrorCode"/>: the code can be used for
/// localization lookup.
/// </remarks>
public class BusinessException : Exception
```

## Commentaires et TODOs

### Commentaires — expliquer le « pourquoi », pas le « quoi »

Un commentaire n'a de valeur que s'il explique une raison non-évidente. Le code
bien nommé se suffit à lui-même pour décrire *ce qu'il fait*.

```csharp
// ✅ Explains obscure business reason
// We must replace 'Ä' with 'Ae' specifically for legacy Swiss banking systems.
string sanitized = name.Replace("Ä", "Ae");

// ❌ States the obvious — the code already says this
// Replace Ä with Ae
string sanitized = name.Replace("Ä", "Ae");
```

### TODOs — attribution et traçabilité obligatoires

Chaque `TODO` doit comporter **l'auteur** et **le numéro d'issue GitLab** :

```csharp
// TODO(JDO): Refactor this once we migrate to .NET 11 (Issue #452)
```

Un `TODO` sans issue est du bruit. Créez l'issue d'abord, puis référencez-la.

### Commentaires interdits

- **Pas de code commenté** — utilisez Git pour l'historique
- **Pas de `// removed`** ou `// unused`** — supprimez le code mort
- **Pas de bandeaux de séparation** (`// ===== Section =====`) — utilisez des
  `#region` si nécessaire, ou mieux, extrayez une classe

## Tests

Les conventions de test sont documentées dans un guide dédié. En résumé :

- **Nommage** : `Method_Scenario_ExpectedBehavior`
- **Pattern AAA** (Arrange-Act-Assert) strict
- **Classes de test** `sealed`, pas d'héritage
- **Headers descriptifs** en début de fichier
- **Stack** : xUnit + Shouldly + NSubstitute + Bogus
- **CancellationToken** : toujours `TestContext.Current.CancellationToken`
  (jamais `CancellationToken.None`)

> Voir aussi : [conventions de test](../../testing/conventions.md),
> [assertions Shouldly](../../testing/assertions.md),
> [mocking NSubstitute](../../testing/mocking.md),
> [tutoriel tests](../demarrage-rapide/08-tests.md)
