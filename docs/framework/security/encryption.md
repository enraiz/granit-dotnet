# Encryption

`Granit.Encryption` fournit un service de chiffrement/déchiffrement
de chaînes avec provider AES-256-CBC par défaut et support optionnel du Transit Engine
de HashiCorp Vault pour les opérations haute sécurité.

## Installation

```bash
dotnet add package Granit.Encryption
```

## Configuration rapide

```csharp
[DependsOn(typeof(GranitEncryptionModule))]
public sealed class AppModule : GranitModule { }
```

## appsettings.json

```json
{
  "Encryption": {
    "PassPhrase": "<clé-depuis-vault>",
    "ProviderName": "Aes"
  }
}
```

> **Sécurité** : `PassPhrase` ne doit **jamais** être stockée en clair dans la config.
> Utiliser `Granit.Vault` comme fournisseur de configuration (voir [vault.md](vault.md)).

## Providers disponibles

| Provider | `ProviderName` | Latence | Quand l'utiliser |
| --- | --- | --- | --- |
| `AesStringEncryptionProvider` | `"Aes"` | < 1 ms | Settings, cache, opérations fréquentes |
| `VaultStringEncryptionProvider` | `"Vault"` | 10-20 ms | Opérations rares, haute sécurité (enregistré par `Granit.Vault`) |

## Utilisation

```csharp
public sealed class PatientService(IStringEncryptionService encryption)
{
    public string EncryptNir(string nir) =>
        encryption.Encrypt(nir);

    public string? DecryptNir(string cipherText) =>
        encryption.Decrypt(cipherText);
}
```

Le service est injecté via `IStringEncryptionService`. Il délègue au provider
sélectionné par `StringEncryptionOptions.ProviderName`.

## Sécurité HDS (CWE-329)

L'IV (vecteur d'initialisation) est **généré aléatoirement** à chaque chiffrement
via `RandomNumberGenerator.GetBytes(16)`. Il n'est **pas** configurable.

Format du ciphertext retourné :

```text
Base64( IV_bytes[16] || CipherText_bytes[N] )
```

L'IV est relu automatiquement lors du déchiffrement depuis les 16 premiers octets.
Un IV statique permettrait à un attaquant d'identifier des motifs dans les données
de santé (violation HDS).

## Dérivation de clé

La `PassPhrase` est dérivée en clé AES-256 via **PBKDF2** (`Rfc2898DeriveBytes`,
SHA-256, 1 000 itérations). La phrase de passe n'est pas utilisée directement comme
clé.

## Options

### `StringEncryptionOptions` (section `Encryption`)

| Propriété | Type | Défaut | Description |
| --- | --- | --- | --- |
| `PassPhrase` | `string` | `""` | Phrase de passe (obligatoire, depuis Vault) |
| `KeySize` | `int` | `256` | Taille de la clé AES en bits (256 recommandé) |
| `ProviderName` | `string` | `"Aes"` | Provider actif (`"Aes"` ou `"Vault"`) |

## Architecture

```text
src/Granit.Encryption/
├── IStringEncryptionService.cs            (interface publique principale)
├── IStringEncryptionProvider.cs           (interface provider)
├── StringEncryptionOptions.cs             (options : PassPhrase, KeySize, ProviderName)
├── Providers/
│   └── AesStringEncryptionProvider.cs     (AES-256-CBC + PBKDF2, IV aléatoire)
├── Services/
│   └── DefaultStringEncryptionService.cs  (délègue au provider actif)
├── GranitEncryptionModule.cs
└── Extensions/
    └── EncryptionServiceCollectionExtensions.cs
```

## Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IStringEncryptionService` | `DefaultStringEncryptionService` | Singleton |
| `IStringEncryptionProvider` | `AesStringEncryptionProvider` | Singleton |

`Granit.Vault` enregistre `VaultStringEncryptionProvider` en tant que
`IStringEncryptionProvider` supplémentaire lors de son chargement.

## Intégration avec Granit.Settings

Les paramètres déclarés avec `IsEncrypted = true` utilisent automatiquement
`IStringEncryptionService` à la couche `ISettingStore` (chiffrement au repos).
Le cache stocke le **plaintext** — seule la persistance en base chiffre/déchiffre.
