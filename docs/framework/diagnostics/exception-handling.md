# Exception Handling

`Granit.ExceptionHandling` fournit la gestion centralisée des
exceptions pour les API ASP.NET Core. Il intercepte toutes les exceptions non gérées
et retourne une réponse JSON standardisée au format **RFC 7807 Problem Details**.

## Objectif

Sans ce module, chaque contrôleur doit gérer ses propres erreurs, et les exceptions
non gérées exposent potentiellement des stack traces contenant des données médicales
(violation ISO 27001). Ce module garantit :

- Les erreurs 5xx ne révèlent jamais d'informations internes en production
- Chaque réponse d'erreur contient un `traceId` pour la corrélation dans Grafana/Loki
- Les clients reçoivent un JSON prévisible, conforme RFC 7807
- Les règles métier (400, 404, 409, 422) sont exprimées via des types d'exceptions dédiés

## Installation

```bash
dotnet add package Granit.ExceptionHandling
```

## Configuration

### Avec le système de modules (recommandé)

```csharp
// Program.cs
await builder.AddGranitAsync<AppModule>();
var app = builder.Build();

// Doit être le premier middleware, avant routing et auth
app.UseGranitExceptionHandling();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

```csharp
// AppModule.cs
[DependsOn(typeof(GranitExceptionHandlingModule))]
public sealed class AppModule : GranitModule { }
```

### Enregistrement direct

```csharp
builder.Services.AddGranitExceptionHandling();

var app = builder.Build();
app.UseGranitExceptionHandling(); // Premier middleware obligatoire
```

La configuration se fait via `appsettings.json` :

```json
{
  "Granit": {
    "ExceptionHandling": {
      "ExposeInternalErrorDetails": false
    }
  }
}
```

> `ExposeInternalErrorDetails` doit être `true` uniquement en développement — jamais en
> production (règle ISO 27001). Utiliser la surcharge par environnement
> (`appsettings.Development.json`) pour activer cette option localement.

## Exceptions disponibles

Les types d'exceptions sont définis dans `Granit.Core` afin que tous les packages
(Vault, Persistence, etc.) puissent les lancer sans dépendance sur un package web.

| Classe | Code HTTP | Interfaces | Usage |
| --- | --- | --- | --- |
| `BusinessException` | 400 | `IHasErrorCode`, `IUserFriendlyException` | Règle métier non respectée |
| `NotFoundException` | 404 | `IUserFriendlyException` | Ressource non trouvée (cas génériques, non-entité) |
| `EntityNotFoundException` | 404 | `IUserFriendlyException` | Entité de domaine introuvable (message générique côté client) |
| `ForbiddenException` | 403 | `IUserFriendlyException` | Accès refusé (authentifié) |
| `ConflictException` | 409 | `IHasErrorCode`, `IUserFriendlyException` | Conflit de ressource |
| `ValidationException` | 422 | `IHasValidationErrors`, `IUserFriendlyException` | Erreurs de validation par champ |

### Exemples

```csharp
// Règle métier
throw new BusinessException("Appointment:SlotUnavailable",
    "Le créneau demandé n'est plus disponible.");

// Entité introuvable — le client reçoit "The requested resource was not found."
// Le type et l'id sont dans les propriétés EntityType/EntityId et dans ToString() (logs)
throw new EntityNotFoundException(typeof(Patient), patientId);

// Accès interdit (violation de tenant)
if (appointment.TenantId != currentUser.TenantId)
    throw new ForbiddenException("Accès à ce rendez-vous non autorisé.");

// Conflit de ressource
throw new ConflictException("Patient:DuplicateNationalId",
    "Un patient avec ce numéro NISS existe déjà.");

// Validation par champs
Dictionary<string, string[]> errors = new()
{
    ["Email"] = ["Le champ Email est requis."],
    ["Nom"]   = ["Le nom doit contenir entre 3 et 50 caractères."]
};
throw new ValidationException(errors);
```

## Format de réponse RFC 7807

Chaque réponse d'erreur est un JSON conforme RFC 7807 avec des extensions Granit :

```json
{
  "status": 400,
  "title": "Le créneau demandé n'est plus disponible.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errorCode": "Appointment:SlotUnavailable"
}
```

Pour une `ValidationException` (422) :

```json
{
  "status": 422,
  "title": "One or more validation errors occurred.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": {
    "Email": ["Le champ Email est requis."],
    "Nom": ["Le nom doit contenir entre 3 et 50 caractères."]
  }
}
```

### Champs garantis

| Champ | Présent | Description |
| --- | --- | --- |
| `status` | Toujours | Code HTTP |
| `title` | Toujours | Message (masqué en prod pour les 5xx) |
| `traceId` | **Toujours** | ID OTEL pour corrélation Grafana/Loki |
| `errorCode` | Si `IHasErrorCode` | Code structuré pour le client |
| `errors` | Si `IHasValidationErrors` | Erreurs par champ |

## Niveaux de log

| Plage status | Niveau | Cas |
| --- | --- | --- |
| 5xx | `Error` | Exceptions internes non gérées |
| 4xx | `Warning` | Erreurs métier, validation, 403/404 |
| 499 | `Information` | Client a fermé la connexion (`OperationCanceledException`) |

## Étendre le mapping HTTP

Pour mapper une exception spécifique à votre domaine, enregistrez un
`IExceptionStatusCodeMapper` supplémentaire :

```csharp
internal sealed class MyDomainExceptionStatusCodeMapper : IExceptionStatusCodeMapper
{
    public int? TryGetStatusCode(Exception exception) => exception switch
    {
        MaintenanceWindowException => StatusCodes.Status503ServiceUnavailable,
        _                          => null  // déléguer au mapper suivant
    };
}

// Dans Program.cs ou une extension de services :
services.AddSingleton<IExceptionStatusCodeMapper, MyDomainExceptionStatusCodeMapper>();
```

Le premier mapper retournant un entier non-null gagne. Le mapper par défaut
(`DefaultExceptionStatusCodeMapper`) est toujours consulté en dernier et retourne
`500` pour tout type inconnu.

## Intégration avec Granit.Persistence (EF Core)

`Granit.Persistence` enregistre automatiquement `EfCoreExceptionStatusCodeMapper`
lorsque `Granit.ExceptionHandling` est actif dans le conteneur.
Ce mapper mappe `DbUpdateConcurrencyException → 409 Conflict`.

L'enregistrement est conditionnel : si `Granit.ExceptionHandling` n'est pas
configuré, aucun mapper EF Core n'est ajouté.

## Localisation des titres d'erreur

Si `Granit.Localization` est configuré dans l'application, le handler tente de
résoudre une traduction pour les exceptions implémentant `IHasErrorCode` :

1. Le préfixe du `ErrorCode` identifie la ressource de localisation.
   Exemple : `"BlobStorage:NotFound"` → préfixe `"BlobStorage"` → ressource
   `BlobStorageLocalizationResource` (annotée `[LocalizationResourceName("BlobStorage")]`).
2. Si une traduction est trouvée pour la clé complète, elle remplace le message
   de l'exception dans `title`.
3. Si aucune traduction n'existe, le message original est utilisé (pour les exceptions
   `IUserFriendlyException`) ou le message générique `"An unexpected error occurred."`
   (pour les 5xx en production).

Chaque module qui définit des exceptions avec `IHasErrorCode` doit donc :

- Créer une classe `XxxLocalizationResource` annotée `[LocalizationResourceName("Xxx")]`
- Fournir des fichiers JSON `Localization/Xxx/{culture}.json` embarqués comme ressource
- Enregistrer la ressource dans son module via `GranitLocalizationOptions`

```csharp
// Exemple : BlobStorageLocalizationResource
[LocalizationResourceName("BlobStorage")]
[InheritResource(typeof(GranitLocalizationResource))]
public sealed class BlobStorageLocalizationResource;
```

```json
// Localization/BlobStorage/fr.json
{
  "culture": "fr",
  "texts": {
    "BlobStorage:NotFound": "Le fichier demandé est introuvable.",
    "BlobStorage:NotValid": "Le fichier n'est pas disponible au téléchargement."
  }
}
```

### Inventaire des errorCode localisés

| Module | Ressource | Clés | Exception |
| --- | --- | --- | --- |
| `Granit.BlobStorage` | `BlobStorage` | `BlobStorage:NotFound`, `BlobStorage:NotValid` | `BusinessException` |
| `Granit.Features` | `Features` | `Features:NotEnabled`, `Features:LimitExceeded`, `Features:InvalidValue` | `BusinessException`, `FeatureValueValidationException` |
| `Granit.Templating` | `Template` | `Template:TransitionDenied` | `TemplateTransitionDeniedException` |
| `Granit.Templating` | `TemplateCategory` | `TemplateCategory:DuplicateName`, `TemplateCategory:HasTemplates` | `ConflictException` |
| `Granit.Cookies` | `Cookies` | `Cookies:Unregistered` | `UnregisteredCookieException` |

> **Convention** : tout module qui lance une exception `IHasErrorCode` **doit** fournir
> les fichiers de localisation correspondants dans les 9 cultures
> (en, fr, nl, de, es, it, pt, en-GB, fr-CA). Sans ces fichiers, le handler
> utilise le `Message` de l'exception en fallback (non traduit).

## Contraintes RGPD/ISO 27001

### Règle fondamentale : masquage des erreurs 5xx

En production, les exceptions non marquées `IUserFriendlyException` ont leur `title`
remplacé par `"An unexpected error occurred."`. Cela empêche l'exposition de :

- Données PHI dans les messages d'exception (ex : `"Patient#12345 not found in cache"`)
- Chemins internes du serveur
- Fragments de requêtes SQL
- Noms de tables ou de colonnes de base de données

L'exception complète est toujours loggée via `ILogger` (vers Loki) pour le diagnostic.

### Règle EntityNotFoundException

`EntityNotFoundException` retourne un message générique `"The requested resource was not found."`
au client afin de ne pas exposer les noms de classes internes (ex : `TemplateCategoryEntity`,
`PatientEntity`). Le type d'entité et l'identifiant restent disponibles :

- Via les propriétés `EntityType` et `EntityId` (pour le code)
- Via `ToString()` (pour les logs — le handler log toujours l'exception complète)

### Règle IUserFriendlyException

N'appliquer `IUserFriendlyException` (ou utiliser `BusinessException`, `ConflictException`,
`ForbiddenException`, `ValidationException`) que si le message **ne contient aucune
donnée sensible**. En cas de doute, ne pas l'utiliser.

```csharp
// Correct : message générique, pas de PHI
throw new BusinessException("Patient:QuotaExceeded",
    "Le nombre maximum de rendez-vous pour cette période est atteint.");

// Incorrect : le message contient un identifiant patient
// throw new BusinessException("Patient:Error", $"Erreur pour patient {patient.Id}");
```

### Corrélation d'incident avec traceId

Le champ `traceId` dans chaque réponse d'erreur est l'identifiant de trace
OpenTelemetry. Pour corréler une erreur client avec les logs Loki :

1. Récupérer le `traceId` dans la réponse JSON de l'API
2. Dans Grafana → Explorer → Loki, rechercher : `{app="my-backend"} |= "traceId"`
3. Ou directement dans Tempo via le `traceId` complet

## Dépendances Granit

| Direction       | Modules                                   |
| --------------- | ----------------------------------------- |
| **Dépend de**   | `Granit.Core`                             |
| **Utilisé par** | `Granit.Persistence`, `Granit.Validation` |

> Voir le [graphe de dépendances complet](../dependencies.md).
