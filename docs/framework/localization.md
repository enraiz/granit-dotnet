# Localisation

Foundation n'introduit aucune abstraction de localisation. La localisation repose
entièrement sur `Microsoft.Extensions.Localization.IStringLocalizer<T>`, le système
standard d'ASP.NET Core. Contrairement à ABP qui propose un système de ressources
virtuelles, Foundation délègue la gestion des traductions à l'application hôte.

> **Référence Microsoft** :
> [Localisation dans ASP.NET Core](https://learn.microsoft.com/fr-fr/aspnet/core/fundamentals/localization)

## Contexte Guava Health

Guava Health est une application de santé numérique opérant en Belgique, avec trois
cultures cibles :

| Culture | Usage |
| --- | --- |
| `fr-BE` (défaut) | Interface principale, professionnels de santé francophones |
| `nl-BE` | Professionnels de santé néerlandophones |
| `en` | Fallback API, intégrations FHIR, logs techniques |

> Les logs applicatifs et les messages d'erreur internes (non affichés à l'utilisateur)
> restent **en anglais** pour la cohérence avec les outils de monitoring (Grafana, Loki).

## Installation

```bash
dotnet add package Microsoft.Extensions.Localization
```

ASP.NET Core inclut la localisation via le framework — aucun package NuGet supplémentaire
n'est nécessaire pour les projets `web` ou `webapi`.

## Configuration dans Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Enregistrement des services de localisation
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// 2. Optionnel : localisation des DataAnnotations (validation)
builder.Services.AddMvc()
    .AddDataAnnotationsLocalization();

await builder.AddFoundationAsync<GuavaHostModule>();

var app = builder.Build();

// 3. Middleware de localisation (avant UseAuthentication)
var supportedCultures = new[] { "fr-BE", "nl-BE", "en" };
app.UseRequestLocalization(options =>
{
    options
        .SetDefaultCulture("fr-BE")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

app.UseAuthentication();
app.UseMultiTenancy();
app.UseAuthorization();

await app.UseFoundationAsync();
await app.RunAsync();
```

## Ressources de localisation

### Structure des fichiers

Les fichiers de ressources sont des fichiers `.resx` (XML) placés dans le dossier
`Resources/` de l'application, avec un fichier par culture :

```text
src/Guava.Host/
├── Resources/
│   ├── Modules.Auth.Messages.fr-BE.resx    (français Belgique — défaut)
│   ├── Modules.Auth.Messages.nl-BE.resx    (néerlandais Belgique)
│   └── Modules.Auth.Messages.en.resx       (anglais — fallback)
└── ...
```

Le nom du fichier correspond au **chemin du type** avec les `.` remplacés par des `/` :
`Modules.Auth.Messages` → classe `Modules.Auth.Messages`.

### Fichier .resx (exemple `fr-BE`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PatientCreated" xml:space="preserve">
    <value>Dossier patient créé avec succès.</value>
  </data>
  <data name="ConsentRequired" xml:space="preserve">
    <value>Le consentement du patient est requis pour accéder à ce dossier.</value>
  </data>
  <data name="InvalidFhirResource" xml:space="preserve">
    <value>La ressource FHIR fournie est invalide : {0}.</value>
  </data>
</root>
```

### Fichier .resx (exemple `nl-BE`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PatientCreated" xml:space="preserve">
    <value>Patiëntendossier succesvol aangemaakt.</value>
  </data>
  <data name="ConsentRequired" xml:space="preserve">
    <value>De toestemming van de patiënt is vereist om toegang te krijgen tot dit dossier.</value>
  </data>
</root>
```

## Utilisation

### Déclarer la classe de ressource marqueur

Créer une classe vide qui sert d'ancre pour `IStringLocalizer<T>` :

```csharp
namespace Guava.Modules.Auth;

// Classe marqueur — lie IStringLocalizer<AuthMessages> aux fichiers .resx
public sealed class AuthMessages { }
```

### Dans un service ou handler

```csharp
using Microsoft.Extensions.Localization;

public class ConsentService
{
    private readonly IStringLocalizer<AuthMessages> _localizer;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(
        IStringLocalizer<AuthMessages> localizer,
        ILogger<ConsentService> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<Result> RecordConsentAsync(Guid patientId, ConsentType type)
    {
        if (!await HasValidConsentAsync(patientId))
        {
            // Message localisé retourné à l'utilisateur
            return Result.Failure(_localizer["ConsentRequired"]);
        }

        _logger.LogInformation("Consent recorded for patient {PatientId}", patientId);

        return Result.Success(_localizer["PatientCreated"]);
    }
}
```

### Dans un handler Wolverine (method injection)

```csharp
public static async Task<IResult> Handle(
    GetPatientQuery query,
    AppDbContext db,
    IStringLocalizer<AuthMessages> localizer,
    ICurrentUserService currentUser,
    CancellationToken cancellationToken)
{
    if (!currentUser.IsInRole("practitioner"))
    {
        return Results.Forbid();
    }

    var patient = await db.Patients.FindAsync(query.PatientId, cancellationToken);
    if (patient is null)
    {
        return Results.NotFound(new { Message = localizer["PatientNotFound"].Value });
    }

    return Results.Ok(patient.ToDto());
}
```

### Dans un endpoint minimal

```csharp
app.MapPost("/api/consent", async (
    RecordConsentRequest request,
    ConsentService service,
    IStringLocalizer<AuthMessages> localizer) =>
{
    var result = await service.RecordConsentAsync(request.PatientId, request.Type);

    return result.IsSuccess
        ? Results.Ok(new { Message = localizer["ConsentRecorded"].Value })
        : Results.BadRequest(new { Message = result.Error });
});
```

## Détection de la culture

ASP.NET Core détecte la culture de la requête depuis plusieurs sources dans cet ordre :

| Source | Exemple | Usage |
| --- | --- | --- |
| Query string | `?culture=nl-BE` | Tests, liens directs |
| Cookie | `c=fr-BE` | Préférence utilisateur persistée |
| Header `Accept-Language` | `Accept-Language: nl-BE,nl;q=0.9` | Navigateurs |
| Défaut | `fr-BE` | Fallback si rien n'est détecté |

Pour une API REST sans UI, l'en-tête `Accept-Language` est la méthode standard.

## Culture dans les modules Foundation

La culture courante est accessible via `CultureInfo.CurrentUICulture` partout dans
le code, sans injection de dépendances. `UseRequestLocalization` l'initialise
automatiquement à chaque requête :

```csharp
// Culture courante (initialisée par le middleware)
var culture = CultureInfo.CurrentUICulture.Name;  // "fr-BE", "nl-BE", "en"
```

## Fallback de culture

Quand une clé n'existe pas dans la culture demandée, ASP.NET Core cherche dans cet ordre :

```text
nl-BE  →  nl  →  (culture neutre)  →  [ResourceNotFound]
```

Pour garantir un fallback vers l'anglais quand une traduction `nl-BE` est absente :
ne pas créer de fichier `nl.resx` — la clé sera retournée telle quelle (son nom),
ce qui est acceptable pour une API REST.

## Localisation des réponses d'erreur

Pour les API REST, les messages d'erreur localisés peuvent être inclus dans les
réponses `ProblemDetails` :

```csharp
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IStringLocalizer<AuthMessages>>();

        if (context.ProblemDetails.Status == StatusCodes.Status403Forbidden)
        {
            context.ProblemDetails.Detail = localizer["AccessDenied"];
        }
    };
});
```

## Tests

```csharp
// Option 1 : NSubstitute
var localizer = Substitute.For<IStringLocalizer<AuthMessages>>();
localizer["ConsentRequired"].Returns(new LocalizedString("ConsentRequired",
    "Consent required"));

var service = new ConsentService(localizer, NullLogger<ConsentService>.Instance);

// Option 2 : StringLocalizer réel avec ressources de test
// Utiliser Microsoft.Extensions.Localization dans un IServiceCollection de test
var services = new ServiceCollection();
services.AddLocalization(o => o.ResourcesPath = "Resources");
var provider = services.BuildServiceProvider();
var localizer = provider.GetRequiredService<IStringLocalizer<AuthMessages>>();
```

## Bonnes pratiques

1. **Clés descriptives** — utiliser des clés en PascalCase décrivant le concept métier
   (`ConsentRequired`, `PatientNotFound`), pas des phrases entières
2. **Anglais par défaut dans les logs** — les messages de log restent en anglais
   (voir [logging.md](logging.md)) ; seuls les messages utilisateur sont localisés
3. **Pas de PII dans les ressources** — les fichiers `.resx` ne contiennent que des
   templates (pas de données patient)
4. **Un namespace de ressource par module** — `Guava.Modules.Auth.AuthMessages`,
   `Guava.Modules.Fhir.FhirMessages`, etc.
5. **Fallback gracieux** — si `IStringLocalizer` ne trouve pas une clé, il retourne
   la clé elle-même ; les clés doivent donc être lisibles comme texte de fallback
6. **`fr-BE` comme culture par défaut** — obligation légale HDS EXI-07/08 pour les
   interfaces utilisateur destinées aux professionnels de santé en Belgique
