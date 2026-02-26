# Analyseurs Roslyn — Granit.Analyzers

`Granit.Analyzers` est un package Roslyn (`DevelopmentDependency`) qui détecte
au build et dans l'IDE les violations de conventions Granit. Il n'est jamais
déployé en production.

## Installation

```bash
dotnet add package Granit.Analyzers
```

## Règles

### Migrations — Expand & Contract (GRMIGA)

Ces règles imposent le pattern Expand & Contract pour les migrations EF Core
zero-downtime. Elles s'activent uniquement quand `Granit.Persistence.Migrations`
est référencé dans le projet (opt-in).

| Règle | Sévérité | Description |
| ----- | -------- | ----------- |
| GRMIGA001 | Error | `DropColumn` sans annotation `[MigrationCycle(MigrationPhase.Contract, ...)]` |
| GRMIGA002 | Error | `RenameColumn` interdit (toujours actif si EF Core est référencé) |
| GRMIGA003 | Warning | `AddColumn` NOT NULL sans `defaultValue` ni `defaultValueSql` |
| GRMIGA004 | Warning | `AlterColumn` avec changement de type sans annotation Contract |

#### Exemple — GRMIGA001

Code qui déclenche l'erreur :

```csharp
public class RemoveOldColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // GRMIGA001: DropColumn requires a Contract-phase annotation
        migrationBuilder.DropColumn(name: "old_col", table: "patients");
    }
}
```

Correction :

```csharp
[MigrationCycle(MigrationPhase.Contract, "patient-v2")]
public class RemoveOldColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "old_col", table: "patients");
    }
}
```

### Sécurité (GRSEC)

Ces règles renforcent les bonnes pratiques de sécurité et la conformité
HDS/RGPD. Elles sont toujours actives.

| Règle | Sévérité | Description |
| ----- | -------- | ----------- |
| GRSEC001 | Warning | `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow` — utiliser `IClock` |
| GRSEC002 | Warning | `Guid.NewGuid()` — utiliser `IGuidGenerator.Create()` |
| GRSEC003 | Error | Secret potentiellement codé en dur dans le code source |

#### GRSEC001 — Accès direct à l'horloge système

L'accès direct à `DateTime.Now` ou `DateTimeOffset.UtcNow` produit du code
non-déterministe et non-testable. L'abstraction `IClock` de `Granit.Timing`
permet d'injecter un `TimeProvider` et de contrôler le temps dans les tests.

```csharp
// Interdit
DateTimeOffset now = DateTimeOffset.UtcNow;

// Correct
DateTimeOffset now = clock.Now;
```

#### GRSEC002 — Guid.NewGuid()

`Guid.NewGuid()` génère des GUID aléatoires qui fragmentent les index clustered
en base de données. `IGuidGenerator.Create()` de `Granit.Guids` produit des
GUID séquentiels optimisés pour PostgreSQL.

```csharp
// Interdit
Guid id = Guid.NewGuid();

// Correct
Guid id = guidGenerator.Create();
```

#### GRSEC003 — Secret codé en dur

L'analyseur détecte les littéraux string assignés à des variables ou propriétés
dont le nom évoque un secret (`password`, `secret`, `apiKey`, `token`,
`connectionString`, `credential`, etc.).

```csharp
// GRSEC003: Potential hardcoded secret detected in 'password'
string password = "myP@ssw0rd!";

// Correct — injecter via Vault
string password = configuration["Vault:Database:Password"];
```

Exclusions automatiques :

- Chaînes vides ou de moins de 4 caractères
- Placeholders (`{vault:...}`, `<placeholder>`, `$ENV_VAR`)

Pour supprimer un faux positif intentionnel :

```csharp
#pragma warning disable GRSEC003
string tokenHeader = "Authorization";
#pragma warning restore GRSEC003
```

### Entity Framework (GREF)

| Règle | Sévérité | Description |
| ----- | -------- | ----------- |
| GREF001 | Warning | `SaveChanges()` synchrone — utiliser `SaveChangesAsync()` |

#### GREF001 — SaveChanges synchrone

`SaveChanges()` bloque le thread appelant et épuise le thread pool sous charge.
Cette règle s'active uniquement quand `DbContext` est présent dans la compilation.

```csharp
// Interdit
context.SaveChanges();

// Correct
await context.SaveChangesAsync();
```

## Suppression des diagnostics

Pour supprimer un diagnostic dans un fichier spécifique :

```csharp
#pragma warning disable GRSEC001
DateTimeOffset now = DateTimeOffset.UtcNow; // exception justifiée
#pragma warning restore GRSEC001
```

Pour supprimer globalement dans un projet, ajouter dans le `.editorconfig` :

```ini
[*.cs]
dotnet_diagnostic.GRSEC001.severity = none
```

## Conformité

| Exigence | Mécanisme |
| -------- | --------- |
| HDS — Traçabilité | GRSEC001 impose `IClock` pour un horodatage déterministe |
| HDS — Expand & Contract | GRMIGA001–004 empêchent les interruptions de service |
| RGPD — Pas de secrets exposés | GRSEC003 détecte les secrets codés en dur |
| Performance | GREF001 impose l'asynchrone pour `SaveChanges` |
| Qualité des index | GRSEC002 impose les GUID séquentiels |
