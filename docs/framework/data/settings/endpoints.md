# Settings — Endpoints

`Granit.Settings.Endpoints` fournit des endpoints Minimal API pour lire et écrire
les paramètres applicatifs à trois niveaux : utilisateur, tenant et global.
Inclut un middleware qui hydrate automatiquement `CultureInfo` et `ICurrentTimezoneProvider`
depuis les préférences utilisateur.

## Installation

```bash
dotnet add package Granit.Settings.Endpoints
```

## Module

```csharp
[DependsOn(typeof(GranitSettingsEndpointsModule))]
public sealed class AppModule : GranitModule { }
```

Le module enregistre automatiquement :

- Les définitions de settings `Granit.Localization.PreferredCulture` et `Granit.Timing.PreferredTimezone`
- Les permissions pour les endpoints d'administration
- Les validators FluentValidation

## Enregistrement des endpoints

```csharp
// Toute application — préférences utilisateur
app.MapGranitUserSettings();

// Administration — settings globaux (nécessite Settings.Global.Read/Manage)
app.MapGranitGlobalSettings();

// Administration — settings tenant (nécessite Settings.Tenant.Read/Manage)
// Disponible uniquement si GranitMultiTenancyModule est activé.
app.MapGranitTenantSettings();
```

Personnalisation des routes :

```csharp
app.MapGranitUserSettings(opts =>
{
    opts.UserRoutePrefix = "my/preferences";
    opts.TagName = "Préférences";
});
```

## Middleware de culture

`SettingsCultureMiddleware` hydrate `CultureInfo.CurrentUICulture` et
`ICurrentTimezoneProvider.Timezone` à partir des settings de l'utilisateur
authentifié. Il doit être enregistré **après** l'authentification :

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SettingsCultureMiddleware>();
```

Pour les requêtes anonymes, le middleware est un no-op. Si la locale stockée
est invalide, elle est ignorée silencieusement.

> **Impact sur `GET /localization`** : ce endpoint utilise déjà
> `CultureInfo.CurrentUICulture` comme fallback quand aucun `cultureName`
> n'est passé en query string. Le middleware rend donc la préférence
> utilisateur effective sans aucune modification de `Granit.Localization.Endpoints`.

## Endpoints — utilisateur

Tous les endpoints utilisateur nécessitent une authentification (pas de
permission spécifique). Le setting doit avoir `IsVisibleToClients = true`.

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/{prefix}` | Tous les settings visibles résolus pour l'utilisateur (cascade) |
| `GET` | `/{prefix}/{name}` | Un setting spécifique |
| `PUT` | `/{prefix}/{name}` | Définir une valeur au niveau utilisateur |
| `DELETE` | `/{prefix}/{name}` | Supprimer la valeur utilisateur (retombe sur Tenant → Global) |

### Exemples

```http
GET /settings/user
→ 200
{
  "Granit.Localization.PreferredCulture": "fr",
  "Granit.Timing.PreferredTimezone": "Europe/Brussels",
  "Guava.Admin.Theme": "dark"
}
```

```http
GET /settings/user/Granit.Localization.PreferredCulture
→ 200
{ "name": "Granit.Localization.PreferredCulture", "value": "fr" }
```

```http
PUT /settings/user/Granit.Localization.PreferredCulture
Content-Type: application/json
{ "value": "nl" }
→ 204
```

```http
DELETE /settings/user/Granit.Localization.PreferredCulture
→ 204
```

## Endpoints — administration globale

Nécessite les permissions `Settings.Global.Read` / `Settings.Global.Manage`.

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/settings/global` | Tous les settings au scope global |
| `PUT` | `/settings/global/{name}` | Définir une valeur globale |

## Endpoints — administration tenant

Nécessite les permissions `Settings.Tenant.Read` / `Settings.Tenant.Manage`.
Le tenant courant est résolu via `ICurrentTenant`. Si aucun contexte tenant
n'est disponible (multi-tenancy non activée), les endpoints retournent `400`.

| Méthode | Route | Description |
| --- | --- | --- |
| `GET` | `/settings/tenant` | Tous les settings au scope tenant courant |
| `PUT` | `/settings/tenant/{name}` | Définir une valeur pour le tenant courant |

## Permissions

| Permission | Description |
| --- | --- |
| `Settings.Global.Read` | Consulter les paramètres globaux |
| `Settings.Global.Manage` | Modifier les paramètres globaux |
| `Settings.Tenant.Read` | Consulter les paramètres du tenant |
| `Settings.Tenant.Manage` | Modifier les paramètres du tenant |

Les permissions sont localisées dans les 17 cultures du framework.

## Options

| Option | Par défaut | Description |
| --- | --- | --- |
| `UserRoutePrefix` | `settings/user` | Préfixe pour les endpoints utilisateur |
| `GlobalRoutePrefix` | `settings/global` | Préfixe pour les endpoints globaux |
| `TenantRoutePrefix` | `settings/tenant` | Préfixe pour les endpoints tenant |
| `TagName` | `Settings` | Tag OpenAPI |

## DTOs

### SettingValueResponse

```csharp
record SettingValueResponse(string Name, string? Value);
```

### UpdateSettingValueRequest

```csharp
record UpdateSettingValueRequest(string? Value);
```

La valeur est validée par `UpdateSettingValueRequestValidator` :

- `Value` : maximum 4 000 caractères
- `null` pour supprimer la valeur (équivalent à `DELETE`)

## Codes d'erreur

| Code | Condition |
| --- | --- |
| `401` | Non authentifié |
| `403` | Permission insuffisante (endpoints admin) |
| `404` | Setting inconnu ou non visible (`IsVisibleToClients = false`) |
| `400` | Tenant non disponible, ou scope non autorisé pour ce setting |
| `422` | Validation échouée (valeur trop longue) |

## Extensibilité — ajouter des settings applicatifs

Pour exposer un setting applicatif via les mêmes endpoints,
il suffit de l'enregistrer avec `IsVisibleToClients = true` :

```csharp
public sealed class GuavaSettingDefinitionProvider : ISettingDefinitionProvider
{
    public void Define(ISettingDefinitionContext context)
    {
        context.Add(new SettingDefinition("Guava.Admin.Theme")
        {
            DefaultValue = "light",
            IsVisibleToClients = true,
            Providers = { "U", "T", "G" }
        });
    }
}
```

Le setting apparaît automatiquement dans `GET /settings/user` et est
modifiable via `PUT /settings/user/Guava.Admin.Theme`.

## Cascade de résolution

Les endpoints utilisateur retournent la valeur **résolue** (cascade complète).
Les endpoints admin retournent la valeur **résolue** au scope demandé.

```text
1. Query explicite : GET /settings/user/App.Theme
2. Cascade : User (U) → Tenant (T) → Global (G) → Config (C) → Default (D)
3. Le premier non-null gagne.
```

Le `DELETE /settings/user/{name}` supprime uniquement la valeur au scope User.
Si le tenant ou le global ont une valeur, elle prend le relais automatiquement.

## Dépendances Granit

```text
GranitSettingsEndpointsModule
  ├── GranitSettingsModule     (ISettingProvider, ISettingManager)
  └── GranitAuthorizationModule (permissions admin)
```

Le middleware utilise aussi `Granit.Timing.ICurrentTimezoneProvider` (soft dependency).

## Voir aussi

- [Settings — Socle](../../core/configuration/settings.md) — définitions, cascade, cache,
  chiffrement
- [Localisation — Endpoints](../../utilities/localization/endpoints.md) — GET /localization
  (bénéficie du middleware de culture)
