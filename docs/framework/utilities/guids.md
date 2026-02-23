# Génération de GUID

`Granit.Guids` fournit une abstraction pour la génération de GUID,
avec support des GUID séquentiels optimisés pour les index clustered des bases de données.

Inspiré du module [`Volo.Abp.Guids`](https://abp.io/docs/latest/framework/infrastructure/guid-generation)
d'ABP Framework.

## Pourquoi utiliser des GUID comme clés primaires ?

Le GUID est le type de clé primaire par défaut pour les modules Digital Dynamics
Foundation. Ce choix repose sur plusieurs avantages :

- **Compatibilité universelle** : utilisable avec tous les fournisseurs de bases de
  données (PostgreSQL, SQL Server, Oracle, MySQL)
- **Génération côté client** : la clé primaire est déterminée avant l'insertion en base,
  sans aller-retour supplémentaire — on connaît l'identifiant avant même le
  `SaveChanges()`
- **Unicité naturelle** : simplifie l'intégration entre systèmes, le partitionnement de
  tables et les architectures distribuées
- **Sécurité** : contrairement aux identifiants auto-incrémentés, les GUID ne sont pas
  devinables — un attaquant ne peut pas énumérer les ressources

## Pourquoi ne pas utiliser `Guid.NewGuid()` ?

Le problème principal des GUID est qu'ils ne sont **pas séquentiels par défaut**.
`Guid.NewGuid()` génère des GUID v4 totalement aléatoires. Quand ces GUID sont utilisés
comme clés primaires avec un index clustered (le comportement par défaut des bases de
données), les insertions peuvent nécessiter une réorganisation des enregistrements
existants :

- **Fragmentation d'index** : les insertions aléatoires provoquent des page splits
  fréquents dans le B-tree
- **Cache misses** : les nouvelles lignes sont dispersées sur des pages non contiguës
- **Dégradation progressive** : les performances d'insertion se dégradent avec le volume

> **Ne jamais utiliser `Guid.NewGuid()` pour créer les identifiants des entités !**
>
> Utiliser `IGuidGenerator.Create()` à la place pour garantir la génération de GUID
> séquentiels, compatibles avec les index clustered.

Les **GUID séquentiels** résolvent ce problème en plaçant un composant temporel (timestamp)
dans le GUID, garantissant que les insertions successives sont ordonnées et adjacentes
dans l'index.

## Installation

```bash
dotnet add package Granit.Guids
```

## Configuration

### Avec le système de modules (recommandé)

Le module `FoundationGuidsModule` est automatiquement chargé via `[DependsOn]` quand
un module dépendant (ex : Persistence) en a besoin. Il suffit d'utiliser
`AddFoundation<T>()` dans `Program.cs` (voir [modularity.md](../core/modularity.md)).

### Enregistrement direct

Pour les projets qui n'utilisent pas le système de modules :

```csharp
builder.Services.AddFoundationGuids();
```

Par défaut, le générateur utilise `SequentialGuidType.SequentialAsString` (optimisé
pour PostgreSQL). Pour changer le type :

```csharp
builder.Services.AddFoundationGuids(options =>
{
    options.DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd; // SQL Server
});
```

> Le package `Granit.Persistence` configure automatiquement le type
> séquentiel adapté au fournisseur de base de données utilisé. Dans la plupart des cas,
> il n'est pas nécessaire de définir cette option manuellement si le package Persistence
> est utilisé.

## IGuidGenerator

Interface définie dans le package `Granit.Guids` :

```csharp
namespace Granit.Guids;

public interface IGuidGenerator
{
    Guid Create();
}
```

Un seul contrat : `Create()` retourne un nouveau `Guid`. Toute la logique de génération
(séquentiel, aléatoire) est un détail d'implémentation.

## SequentialGuidType

Chaque base de données trie les GUID différemment. Le type séquentiel doit correspondre
au moteur utilisé :

```csharp
public enum SequentialGuidType
{
    SequentialAsString,
    SequentialAsBinary,
    SequentialAtEnd
}
```

| Type | Base de données | Tri basé sur |
| --- | --- | --- |
| `SequentialAsString` | PostgreSQL, MySQL | Représentation string (`ToString()`) |
| `SequentialAsBinary` | Oracle | Tableau d'octets (`ToByteArray()`) |
| `SequentialAtEnd` | SQL Server | 6 derniers octets du bloc Data4 |

**Pour Digital Dynamics** : PostgreSQL est la base standard, donc le défaut est
`SequentialAsString`.

## SequentialGuidGenerator

Implémentation par défaut enregistrée par `AddFoundationGuids()`. Génère des GUID
séquentiels en combinant un timestamp milliseconde et des octets aléatoires
cryptographiquement sûrs.

### Algorithme

1. **10 octets aléatoires** via `RandomNumberGenerator` (cryptographiquement sûr)
2. **Timestamp** : `DateTime.UtcNow.Ticks / 10000` (millisecondes depuis l'an 0001)
3. **6 octets de timestamp** extraits (48 bits, couvrant ~8 900 ans)
4. **Assemblage** selon le `SequentialGuidType` :

```text
SequentialAsString / SequentialAsBinary :
  [timestamp 6 octets] [random 10 octets]

SequentialAtEnd (SQL Server) :
  [random 10 octets] [timestamp 6 octets]
```

### Endianness

Sur les systèmes little-endian (x86/x64), l'algorithme inverse les octets du timestamp
et corrige les blocs Data1/Data2 du GUID pour garantir un tri correct quelle que soit
la plateforme.

### Propriétés

- **Ordonnancement** : les GUID générés successivement sont croissants dans l'index
- **Unicité** : 10 octets aléatoires (80 bits d'entropie) garantissent l'unicité
  même en environnement distribué
- **Sécurité** : `RandomNumberGenerator` (pas `System.Random`)

## SimpleGuidGenerator

Wrapper autour de `Guid.NewGuid()` pour les cas où la séquentialité n'est pas nécessaire
(tests, identifiants temporaires) :

```csharp
public class SimpleGuidGenerator : IGuidGenerator
{
    public static SimpleGuidGenerator Instance { get; } = new();

    public Guid Create() => Guid.NewGuid();
}
```

`SimpleGuidGenerator.Instance` est disponible statiquement pour les contextes sans
injection de dépendances (tests unitaires, helpers statiques).

## GuidGeneratorOptions

```csharp
public sealed class GuidGeneratorOptions
{
    public SequentialGuidType? DefaultSequentialGuidType { get; set; }

    public SequentialGuidType GetDefaultSequentialGuidType()
    {
        return DefaultSequentialGuidType ?? SequentialGuidType.SequentialAsString;
    }
}
```

| Propriété | Défaut | Description |
| --- | --- | --- |
| `DefaultSequentialGuidType` | `SequentialAsString` | Type de GUID séquentiel selon la base de données |

Le défaut est `SequentialAsString` (PostgreSQL) contrairement à ABP qui utilise
`SequentialAtEnd` (SQL Server).

## Usage

### Définition d'entité avec GUID

Passer le GUID dans le constructeur de l'entité garantit qu'aucune entité n'existe
sans identifiant valide :

```csharp
public class Patient
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    private Patient() { } // Constructeur pour EF Core

    public Patient(Guid id, string firstName, string lastName)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
    }
}
```

### Dans un handler Wolverine (method injection)

```csharp
public static async Task<PatientCreated> Handle(
    CreatePatient command,
    AppDbContext db,
    IGuidGenerator guidGenerator,
    IClock clock,
    CancellationToken cancellationToken)
{
    var patient = new Patient(
        guidGenerator.Create(),
        command.FirstName,
        command.LastName);

    db.Patients.Add(patient);
    await db.SaveChangesAsync(cancellationToken);

    return new PatientCreated(patient.Id);
}
```

### Dans un service avec injection par constructeur

```csharp
public class PatientService
{
    private readonly AppDbContext _db;
    private readonly IGuidGenerator _guidGenerator;

    public PatientService(AppDbContext db, IGuidGenerator guidGenerator)
    {
        _db = db;
        _guidGenerator = guidGenerator;
    }

    public async Task<Guid> CreateAsync(
        string firstName,
        string lastName,
        CancellationToken cancellationToken)
    {
        var patient = new Patient(
            _guidGenerator.Create(),
            firstName,
            lastName);

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync(cancellationToken);

        return patient.Id;
    }
}
```

### Dans l'intercepteur AuditedEntityInterceptor

L'intercepteur `AuditedEntityInterceptor` du package Persistence peut utiliser
`IGuidGenerator` pour assigner les `Id` des nouvelles entités au lieu de
`Guid.NewGuid()` :

```csharp
if (entry.Entity is Entity entity)
{
    if (entry.State == EntityState.Added && entity.Id == Guid.Empty)
    {
        entity.Id = _guidGenerator.Create();
    }
}
```

## Bonnes pratiques

1. **Toujours injecter `IGuidGenerator`** — ne jamais appeler `Guid.NewGuid()`
   directement pour les identifiants d'entités
2. **Assigner l'identifiant à la création** — passer le GUID dans le constructeur
   de l'entité plutôt que de le définir après coup
3. **Laisser Persistence configurer le type** — le package Persistence sélectionne
   automatiquement le `SequentialGuidType` adapté au fournisseur de base de données ;
   une configuration manuelle n'est nécessaire que si Persistence n'est pas utilisé
4. **Utiliser `SimpleGuidGenerator` dans les tests** — pour les tests unitaires où
   la séquentialité n'a pas d'importance, ou mocker `IGuidGenerator` pour contrôler
   les valeurs exactes
5. **Un seul `IGuidGenerator` par application** — le générateur est enregistré en
   Singleton, thread-safe ; ne pas créer d'instances manuellement

## Architecture

```text
Granit.Guids
├── IGuidGenerator.cs                 (interface, contrat public)
├── SequentialGuidGenerator.cs
├── SimpleGuidGenerator.cs
├── SequentialGuidType.cs
├── GuidGeneratorOptions.cs
├── FoundationGuidsModule.cs          (module Foundation)
└── Extensions/
    └── GuidsServiceCollectionExtensions.cs  (AddFoundationGuids)
```

## Services enregistrés

| Service | Implémentation | Lifetime |
| --- | --- | --- |
| `IGuidGenerator` | `SequentialGuidGenerator` | Singleton |

`SequentialGuidGenerator` est thread-safe (`RandomNumberGenerator` est statique et
thread-safe). Le lifetime Singleton évite les allocations inutiles.

## Tests

```csharp
// Utiliser SimpleGuidGenerator dans les tests unitaires
var guidGenerator = SimpleGuidGenerator.Instance;
var id = guidGenerator.Create();
id.Should().NotBe(Guid.Empty);

// Ou mocker IGuidGenerator pour des assertions exactes
var guidGenerator = Substitute.For<IGuidGenerator>();
var fixedId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
guidGenerator.Create().Returns(fixedId);
```

## Comparaison avec Guid.NewGuid()

| Critère | `Guid.NewGuid()` | `IGuidGenerator` (séquentiel) |
| --- | --- | --- |
| Performance index clustered | Fragmentation élevée | Insertions ordonnées |
| Testabilité | Non mockable | Mockable via interface |
| Centralisation | Appels dispersés | Point d'injection unique |
| Sécurité (entropie) | 122 bits (v4) | 80 bits (suffisant) |
| Ordonnancement temporel | Aucun | Croissant par timestamp |
| Génération côté client | Oui | Oui |
| Systèmes distribués | Collision improbable | Collision improbable |

## Dépendances

| Package | Rôle |
| --- | --- |
| `Microsoft.Extensions.Options` | `IOptions<GuidGeneratorOptions>` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Registration DI |
