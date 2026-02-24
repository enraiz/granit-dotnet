# Multi-Tenancy

`Granit.MultiTenancy` fournit l'isolation de tenant par requête HTTP via
`ICurrentTenant`. La résolution du tenant est automatique : le middleware lit
le claim JWT ou l'en-tête HTTP, active le contexte, puis le restaure en fin de requête.

> **`ICurrentTenant` dans `Granit.Core`** : l'interface `ICurrentTenant` est définie
> dans `Granit.Core.MultiTenancy` et accessible dans **tous** les modules Granit sans
> référencer `Granit.MultiTenancy`. Un `NullTenantContext` (`IsAvailable = false`) est
> enregistré par défaut. `Granit.MultiTenancy` remplace cet enregistrement par
> l'implémentation réelle à l'initialisation. Voir
> [core.md — Dépendance optionnelle sur le multi-tenancy](../core/core.md#dépendance-optionnelle-sur-le-multi-tenancy).
> **Référence Microsoft** :
> [Middleware ASP.NET Core](https://learn.microsoft.com/fr-fr/aspnet/core/fundamentals/middleware)

## Architecture

```text
Requête HTTP
    │
    ▼
TenantResolutionMiddleware
    │
    ├─ TenantResolverPipeline (exécute les résolveurs par Order croissant)
    │       ├─ HeaderTenantResolver   (Order=100) → lit X-Tenant-Id
    │       └─ JwtClaimTenantResolver (Order=200) → lit claim "tenant_id"
    │
    ▼
ICurrentTenant.Change(id, name)  ←─ AsyncLocal, scope = durée de la requête
    │
    ▼
Handlers, Services, Intercepteurs EF Core (lisent ICurrentTenant)
```

**Stratégie first-wins** : le premier résolveur qui retourne un tenant non-nul
l'emporte. L'en-tête HTTP est prioritaire sur le claim JWT.

## Installation

```bash
dotnet add package Granit.MultiTenancy
```

## Configuration dans Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

await builder.AddGranitAsync<GuavaHostModule>();

var app = builder.Build();

// Ordre obligatoire dans le pipeline
app.UseAuthentication();
app.UseGranitMultiTenancy();   // après Authentication, avant Authorization
app.UseAuthorization();

await app.UseGranitAsync();
await app.RunAsync();
```

> `UseGranitMultiTenancy()` doit être placé **après** `UseAuthentication()` :
> `JwtClaimTenantResolver` lit `HttpContext.User`, qui n'est rempli qu'après la
> validation du token JWT.

## Options de configuration

```json
{
  "MultiTenancy": {
    "IsEnabled": true,
    "TenantIdClaimType": "tenant_id",
    "TenantIdHeaderName": "X-Tenant-Id"
  }
}
```

| Propriété | Défaut | Description |
| --- | --- | --- |
| `IsEnabled` | `true` | Active ou désactive la résolution de tenant |
| `TenantIdClaimType` | `"tenant_id"` | Nom du claim JWT contenant l'identifiant de tenant |
| `TenantIdHeaderName` | `"X-Tenant-Id"` | Nom de l'en-tête HTTP contenant l'identifiant de tenant |

Pour désactiver dans les tests d'intégration :

```json
{
  "MultiTenancy": {
    "IsEnabled": false
  }
}
```

## Résolveurs de tenant

### HeaderTenantResolver (Order = 100)

Lit l'en-tête HTTP `X-Tenant-Id` (configurable). Utilisé pour les appels
service-à-service où le tenant est transmis explicitement.

```http
GET /api/patients HTTP/1.1
Authorization: Bearer <token>
X-Tenant-Id: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### JwtClaimTenantResolver (Order = 200)

Lit le claim `tenant_id` (configurable) du token JWT. Utilisé pour les requêtes
utilisateur authentifiées via Keycloak — le tenant est encodé dans le token.

```json
{
  "sub": "user-uuid",
  "tenant_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "realm_access": { "roles": ["practitioner"] }
}
```

### Ajouter un résolveur personnalisé

```csharp
public sealed class SubdomainTenantResolver : ITenantResolver
{
    public int Order => 50;   // prioritaire sur Header (100) et JWT (200)

    public Task<TenantInfo?> ResolveAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        string host = context.Request.Host.Host;  // ex: "tenant-abc.guava.health"
        string subdomain = host.Split('.')[0];

        if (Guid.TryParse(subdomain, out Guid tenantId))
        {
            return Task.FromResult<TenantInfo?>(new TenantInfo(tenantId));
        }

        return Task.FromResult<TenantInfo?>(null);
    }
}
```

Enregistrement dans le module :

```csharp
services.AddSingleton<ITenantResolver, SubdomainTenantResolver>();
```

## Utilisation

### Dans un service (injection par constructeur)

```csharp
public class PatientService
{
    private readonly ICurrentTenant _currentTenant;
    private readonly AppDbContext _db;

    public PatientService(ICurrentTenant currentTenant, AppDbContext db)
    {
        _currentTenant = currentTenant;
        _db = db;
    }

    public async Task<IReadOnlyList<Patient>> GetPatientsAsync(
        CancellationToken cancellationToken)
    {
        if (!_currentTenant.IsAvailable)
        {
            throw new InvalidOperationException("No tenant context.");
        }

        return await _db.Patients
            .Where(p => p.TenantId == _currentTenant.Id)
            .ToListAsync(cancellationToken);
    }
}
```

### Dans un handler Wolverine (method injection)

```csharp
public static async Task<IResult> Handle(
    GetPatientsQuery query,
    AppDbContext db,
    ICurrentTenant currentTenant,
    CancellationToken cancellationToken)
{
    if (!currentTenant.IsAvailable)
    {
        return Results.BadRequest(new { Error = "Tenant context required." });
    }

    var patients = await db.Patients
        .Where(p => p.TenantId == currentTenant.Id)
        .ToListAsync(cancellationToken);

    return Results.Ok(patients);
}
```

### Override temporaire (tâches de fond, tests)

```csharp
// Activer un tenant pour un bloc de code
using IDisposable scope = _currentTenant.Change(tenantId, tenantName);
await ProcessTenantDataAsync();
// Le tenant précédent est restauré automatiquement à la sortie du using
```

Cas d'usage :

- Tâches de fond (`IHostedService`) qui traitent plusieurs tenants séquentiellement
- Tests d'intégration qui simulent une requête tenant-specifique
- Workers multi-tenant qui iterent sur une liste de tenants

## ICurrentTenant — API complète

```csharp
// Namespace : Granit.Core.MultiTenancy (package Granit.Core)
public interface ICurrentTenant
{
    bool IsAvailable { get; }      // true si Id est non-nul
    Guid? Id { get; }              // identifiant du tenant courant
    string? Name { get; }          // nom du tenant (optionnel)
    IDisposable Change(Guid? id, string? name = null);
}
```

L'interface est définie dans `Granit.Core.MultiTenancy`. L'implémentation `CurrentTenant`
fournie par `Granit.MultiTenancy` repose sur `AsyncLocal<T>` — le contexte est propagé
automatiquement dans les chaînes `async/await` et isolé entre les requêtes parallèles.

## Intégration EF Core

`Granit.Persistence` lit `ICurrentTenant` dans ses intercepteurs pour
filtrer et affecter automatiquement `TenantId` sur les entités `ITenantAware` :

```csharp
// AuditableEntityInterceptor affecte TenantId à la création
entity.TenantId = _currentTenant.Id ?? throw new InvalidOperationException("...");
```

Voir [persistence.md](persistence.md) pour les détails.

## Enrichissement des logs

`TenantResolutionMiddleware` enrichit automatiquement les logs Serilog avec
`TenantId` quand un tenant est résolu :

```csharp
if (tenant is not null)
{
    using (LogContext.PushProperty("TenantId", tenant.Id))
    {
        await next(context);
    }
}
```

Chaque log émis pendant la requête contient la propriété `TenantId`, consultable
dans Grafana/Loki :

```json
{
  "Level": "Information",
  "Message": "Patient record created: {PatientId}",
  "TenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "PatientId": "...",
  "TraceId": "abc123"
}
```

## Tests

### Simuler un tenant actif

```csharp
// NSubstitute
var tenant = Substitute.For<ICurrentTenant>();
tenant.IsAvailable.Returns(true);
tenant.Id.Returns(Guid.NewGuid());

var service = new PatientService(tenant, db);
```

### Utiliser ICurrentTenant réel avec override

```csharp
var currentTenant = new CurrentTenant();
Guid tenantId = Guid.NewGuid();

using IDisposable _ = currentTenant.Change(tenantId, "test-tenant");

// currentTenant.IsAvailable == true
// currentTenant.Id == tenantId
```

### Désactiver dans les tests d'intégration

```json
// appsettings.Test.json
{
  "MultiTenancy": {
    "IsEnabled": false
  }
}
```

Ou via `WebApplicationFactory` :

```csharp
factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureAppConfiguration(config =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MultiTenancy:IsEnabled"] = "false"
        });
    });
});
```

## Bonnes pratiques

1. **Vérifier `IsAvailable`** avant d'utiliser `Id` dans les services métier —
   certains endpoints peuvent être appelés sans contexte de tenant (health checks,
   endpoints publics)
2. **Ne pas caster vers `CurrentTenant`** — toujours injecter `ICurrentTenant`
   pour permettre le mock dans les tests
3. **Order = 100 → 200** : l'en-tête HTTP est prioritaire sur le claim JWT —
   utile pour les tests d'intégration et les appels service-à-service
4. **AsyncLocal, pas HttpContext** : `ICurrentTenant` est utilisable hors du
   contexte HTTP (workers, tâches de fond, EF Core intercepteurs)
5. **Isolation en parallèle** : chaque tâche `Task.Run(...)` hérite du contexte
   tenant de son parent, mais les modifications ultérieures sont indépendantes

## Voir aussi

- [Isolation Tenant-per-Database](isolation-tenant-per-database.md)
- [Isolation Tenant-per-Schema](isolation-tenant-per-schema.md)
- [Sélection de stratégie d'isolation](isolation-strategie.md)
- [Persistence — intercepteurs EF Core](persistence.md)
