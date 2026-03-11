# Vault

`Granit.Vault` fournit l'intégration HashiCorp Vault pour les
applications .NET Granit : credentials dynamiques PostgreSQL, chiffrement
Transit et gestion automatique des leases.

## Installation

```bash
dotnet add package Granit.Vault
```

## Configuration

### appsettings.json

```json
{
  "Vault": {
    "Address": "https://vault.example.com",
    "AuthMethod": "Kubernetes",
    "KubernetesRole": "my-backend",
    "DatabaseMountPoint": "database",
    "DatabaseRoleName": "readwrite",
    "TransitMountPoint": "transit"
  }
}
```

### Program.cs

Avec le système de modules (recommandé), `GranitVaultModule` est chargé
automatiquement via `AddGranit<T>()`. Le module skip l'enregistrement en
environnement Development (voir [modularity.md](../core/modularity.md)).

Pour un enregistrement direct :

```csharp
// Vault n'est activé qu'en production (credentials dynamiques, Transit)
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddGranitVault();
}
```

## VaultOptions

```csharp
public sealed class VaultOptions
{
    public const string SectionName = "Vault";

    public string Address { get; set; }
    public string AuthMethod { get; set; } = "Kubernetes";
    public string? Token { get; set; }                    // Dev uniquement
    public string KubernetesRole { get; set; } = "my-backend";
    public string KubernetesTokenPath { get; set; }       // Défaut : ServiceAccount token K8s
    public string DatabaseMountPoint { get; set; } = "database";
    public string DatabaseRoleName { get; set; } = "readwrite";
    public string TransitMountPoint { get; set; } = "transit";
    public double LeaseRenewalThreshold { get; set; } = 0.75;
}
```

## Authentification

Deux méthodes d'authentification sont supportées :

| Méthode | Usage | Configuration |
| --- | --- | --- |
| `Kubernetes` | Production | ServiceAccount token + rôle K8s |
| `Token` | Développement local | Token statique (jamais en production) |

La `VaultClientFactory` sélectionne automatiquement la méthode selon
`VaultOptions.AuthMethod`.

## Credentials dynamiques PostgreSQL

Le `VaultCredentialLeaseManager` est un `BackgroundService` qui :

1. Obtient un credential dynamique via Vault Database Engine
2. Renouvelle le lease avant expiration (seuil configurable, défaut 75% du TTL)
3. Demande un nouveau credential si le renouvellement échoue

Les credentials sont exposés via `IDatabaseCredentialProvider` :

```csharp
public interface IDatabaseCredentialProvider
{
    string Username { get; }
    string Password { get; }
    bool IsReady { get; }
}
```

Le `DbContext` utilise ce provider pour construire sa connection string dynamiquement.

### Conformité ISO 27001

Aucun mot de passe statique en production. Les credentials sont :

- Générés dynamiquement par Vault
- Valides pour une durée limitée (TTL)
- Renouvelés automatiquement
- Révoqués à l'arrêt du service

## Chiffrement Transit

`TransitEncryptionService` implémente `ITransitEncryptionService` pour le chiffrement
des données sensibles via Vault Transit Engine (AES-256-GCM96).

```csharp
// Chiffrer des données sensibles
var encrypted = await transitService.EncryptAsync("sensitive-data", payload);
// encrypted == "vault:v1:..."

// Déchiffrer
var decrypted = await transitService.DecryptAsync("sensitive-data", encrypted);
```

### Avantages

- Les clés de chiffrement ne quittent jamais Vault
- Rotation des clés gérée centralement par Vault
- Aucune clé stockée dans l'application
- Versioning des clés (rekeying transparent)

## Architecture

```text
Granit.Vault
├── ITransitEncryptionService.cs             (interface, contrat public)
├── Options/
│   └── VaultOptions.cs
├── Services/
│   ├── VaultClientFactory.cs               (création du client VaultSharp)
│   ├── VaultCredentialLeaseManager.cs      (BackgroundService, credentials dynamiques)
│   └── TransitEncryptionService.cs         (chiffrement/déchiffrement Transit)
├── GranitVaultModule.cs                (module Granit)
└── Extensions/
    └── VaultServiceCollectionExtensions.cs  (AddGranitVault)
```

## Services enregistrés

| Service | Implementation | Lifetime |
| --- | --- | --- |
| `IVaultClient` | Via `VaultClientFactory` | Singleton |
| `VaultClientFactory` | - | Singleton |
| `IDatabaseCredentialProvider` | `VaultCredentialLeaseManager` | Singleton |
| `VaultCredentialLeaseManager` | - | Singleton (+ HostedService) |
| `ITransitEncryptionService` | `TransitEncryptionService` | Scoped |

## Sécurité

- Le token Vault (`VaultOptions.Token`) ne doit **jamais** être utilisé en production
- L'authentification Kubernetes est **obligatoire** en production
- Les logs ne doivent **jamais** exposer de secrets (les credentials sont masqués)
- Le `LeaseRenewalThreshold` (75%) garantit le renouvellement avant expiration

## Dépendances Granit

| Direction | Modules |
|-----------|---------|
| **Dépend de** | `Granit.Core`, `Granit.Encryption` |
| **Utilisé par** | Module feuille (consommé par les applications, pas par d'autres modules) |

> `VaultConfigurationException` implémente `IHasErrorCode` — les messages
> d'erreur sont localisés par `GranitExceptionHandler` à la frontière HTTP
> via les JSON embarqués (`Localization/Vault/{culture}.json`), découverts
> automatiquement par `LocalizationAutoDiscovery`.
>
> Voir le [graphe de dépendances complet](../dependencies.md).
