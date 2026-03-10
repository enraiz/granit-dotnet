# Garde-fous qualité — Référence complète

Ce document recense **tous les mécanismes automatisés** qui détectent les
anti-patterns, les erreurs de codage et les violations d'architecture dans
les projets Granit. Trois couches complémentaires assurent la couverture.

| Couche | Quand | Outil | Portée |
| ------ | ----- | ----- | ------ |
| **Analyseurs Roslyn** | Build + IDE | `Granit.Analyzers` + `BannedApiAnalyzers` | Ligne de code |
| **Tests d'architecture** | `dotnet test` | `Granit.ArchitectureTests` (ArchUnitNET) | Assemblies + source |
| **SonarQube** | CI/CD | SonarScanner | Analyse statique complète |

---

## 1. Analyseurs Roslyn — Granit.Analyzers

Détails complets : [analyzers.md](../framework/diagnostics/analyzers.md)

### Sécurité (GRSEC)

| ID | Sévérité | Détecte | Correction |
| -- | -------- | ------- | ---------- |
| GRSEC001 | Warning | `DateTime.Now/UtcNow`, `DateTimeOffset.Now/UtcNow` | CodeFix → `IClock` |
| GRSEC002 | Warning | `Guid.NewGuid()` | CodeFix → `IGuidGenerator.Create()` |
| GRSEC003 | Error | Secret codé en dur (`password`, `token`, `apiKey`, etc.) | Manuel |
| GRSEC004 | Warning | `IResponseCookies.Append/Delete` direct | CodeFix → `IGranitCookieManager` |

### Entity Framework (GREF)

| ID | Sévérité | Détecte | Correction |
| -- | -------- | ------- | ---------- |
| GREF001 | Warning | `SaveChanges()` synchrone | CodeFix → `await SaveChangesAsync()` |

### Migrations — Expand & Contract (GRMIGA)

| ID | Sévérité | Détecte | Correction |
| -- | -------- | ------- | ---------- |
| GRMIGA001 | Error | `DropColumn` sans annotation `[MigrationCycle(Contract)]` | Annoter la migration |
| GRMIGA002 | Error | `RenameColumn` (interdit, jamais zero-downtime safe) | `AddColumn` → data migration → `DropColumn` |
| GRMIGA003 | Warning | `AddColumn` NOT NULL sans `defaultValue`/`defaultValueSql` | Ajouter la valeur par défaut |
| GRMIGA004 | Warning | `AlterColumn` avec changement de type sans annotation Contract | Annoter la migration |

---

## 2. API interdites — BannedApiAnalyzers (RS0030)

Fichier : [`BannedSymbols.txt`](../../BannedSymbols.txt) — appliqué via
`Microsoft.CodeAnalysis.BannedApiAnalyzers` à tous les projets (déclaré dans
`Directory.Build.props`). Détection au build et dans l'IDE (sévérité : Error).

| API interdite | Alternative | Raison |
| ------------- | ----------- | ------ |
| `new HttpClient()` (3 constructeurs) | `IHttpClientFactory` | Socket exhaustion sous charge |
| `new Regex(...)` (3 constructeurs) | `[GeneratedRegex]` | Compilation AOT, performance |
| `Thread.Sleep(int/TimeSpan)` | `Task.Delay()` ou `TimeProvider` | Bloque le thread pool |
| `Task.Result`, `Task.Wait()` (5 surcharges) | `await` | Deadlock sync-over-async |
| `GC.Collect()` (3 surcharges) | — | Interdit en code bibliothèque |
| `Console.Write/WriteLine` (3 surcharges) | `ILogger` + `[LoggerMessage]` | Observabilité structurée |
| `Environment.GetEnvironmentVariable` (2 surcharges) | `IConfiguration` | Injection de configuration |

---

## 3. Tests d'architecture — Granit.ArchitectureTests

Tests exécutés par `dotnet test`. Deux catégories : analyses ArchUnitNET sur les
assemblies compilées, et scans de code source via regex.

### Conception des classes (ClassDesignTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `DbContext_classes_should_be_sealed` | `DbContext` non `sealed` | ArchUnitNET |
| `EfStore_implementations_should_not_be_public` | `Ef*Store` public | ArchUnitNET |
| `No_MVC_controllers_allowed` | Héritage de `ControllerBase`/`Controller` | ArchUnitNET |
| `Options_classes_should_be_sealed` | `*Options` non `sealed` | ArchUnitNET |
| `EntityTypeConfigurations_should_be_in_EfCore_layer` | `IEntityTypeConfiguration<T>` hors persistence | ArchUnitNET |
| `Entity_configurations_should_not_be_public` | `*Configuration` public dans EF Core | ArchUnitNET |
| `Public_types_should_not_reside_in_Internal_namespaces` | Type `public` dans `*.Internal.*` | ArchUnitNET |

### Dépendances entre couches (LayerDependencyTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `Core_types_should_not_depend_on_EntityFrameworkCore` | Core → EF Core | ArchUnitNET |
| `Timing_types_should_not_depend_on_EntityFrameworkCore` | Timing → EF Core | ArchUnitNET |
| `Guids_types_should_not_depend_on_EntityFrameworkCore` | Guids → EF Core | ArchUnitNET |
| `Endpoint_types_should_not_depend_on_EntityFrameworkCore` | Endpoints → EF Core | ArchUnitNET |
| `IQueryable_should_not_appear_in_non_persistence_types` | `IQueryable<T>` hors persistence | ArchUnitNET |
| `Endpoint_types_should_not_inherit_from_domain_entities` | DTO qui hérite de `Entity`/`AggregateRoot` | ArchUnitNET |

### Nommage et CQRS (CqrsConventionTests, DtoConventionTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `Interfaces_should_start_with_I_prefix` | Interface sans préfixe `I` | ArchUnitNET |
| `Reader_interfaces_should_end_with_Reader` | `*Reader*` sans suffixe `Reader` | ArchUnitNET |
| `Writer_interfaces_should_end_with_Writer` | `*Writer*` sans suffixe `Writer` | ArchUnitNET |
| `Endpoint_types_should_not_use_Dto_suffix` | Suffixe `Dto` dans les endpoints | ArchUnitNET |

### Modules Granit (ModuleConventionTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `Modules_should_be_sealed` | `GranitModule` non `sealed` | ArchUnitNET |
| `Module_class_names_should_start_with_Granit_and_end_with_Module` | Nommage incorrect | ArchUnitNET |

### Isolated DbContext (IsolatedDbContextTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `OnModelCreating_should_call_ApplyGranitConventions` | `ApplyGranitConventions` manquant | Scan source |
| `No_manual_HasQueryFilter_in_entity_configurations` | `HasQueryFilter()` manuel | Scan source |
| `EfCore_projects_should_reference_GranitPersistence` | Référence `.csproj` manquante | Scan source |
| `EfCore_modules_should_DependOn_GranitPersistenceModule` | `[DependsOn]` manquant | Scan source |
| `EfCore_extension_methods_should_use_interceptor_DI_pattern` | `ServiceLifetime.Scoped` manquant | Scan source |

### Anti-patterns source (SourceCodeAntiPatternTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `Async_void_methods_should_not_exist_in_src` | `async void` (crash du process) | Scan source |
| `Throw_ex_should_not_be_used_in_src` | `throw ex;` (perte de stack trace) | Scan source |

### Dépendances projet (ProjectDependencyTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `No_circular_project_references` | Cycle A → B → A dans les `.csproj` | Scan source |

### Structure projet (TestProjectConventionTests)

| Test | Détecte | Mécanisme |
| ---- | ------- | --------- |
| `Every_src_package_should_have_a_test_project` | Package sans `*.Tests` | Scan filesystem |
| `Every_src_package_should_have_a_README` | Package sans `README.md` | Scan filesystem |

---

## Récapitulatif par catégorie

| Catégorie | Nb règles | Couche |
| --------- | --------- | ------ |
| Sécurité & HDS | 4 GRSEC + 7 BannedAPIs | Compile-time |
| Entity Framework | 1 GREF + 5 DbContext | Compile-time + Test |
| Migrations zero-downtime | 4 GRMIGA | Compile-time |
| Architecture en couches | 6 rules | Test (ArchUnitNET) |
| Nommage & CQRS | 4 rules | Test (ArchUnitNET) |
| Conception des classes | 7 rules | Test (ArchUnitNET) |
| Anti-patterns C# | 2 rules + 4 BannedAPIs | Test + Compile-time |
| Modules & projets | 5 rules | Test |
| **Total** | **49 règles** | |

---

## Ajout d'une nouvelle règle

### Analyser Roslyn (compile-time)

Utiliser si la détection doit être immédiate dans l'IDE et bloquer le build.

1. Créer un `DiagnosticAnalyzer` dans `src/Granit.Analyzers/`
2. Créer un `CodeFixProvider` dans `src/Granit.Analyzers.CodeFixes/` (si applicable)
3. Ajouter des tests dans `tests/Granit.Analyzers.Tests/`
4. Documenter dans [analyzers.md](analyzers.md)

### API interdite (compile-time)

Utiliser pour interdire un appel d'API spécifique (constructeur, méthode, propriété).

1. Ajouter une ligne dans [`BannedSymbols.txt`](../../../BannedSymbols.txt)
2. Format : `M:Namespace.Type.Method; Message d'erreur` (voir syntaxe RS0030)

### Test d'architecture (test-time)

Utiliser pour des contraintes structurelles (dépendances, nommage, patterns).

1. Règle réutilisable → `src/Granit.ArchitectureTests.Abstractions/Rules/`
2. Test Granit-specific → `tests/Granit.ArchitectureTests/`
3. Deux mécanismes : ArchUnitNET (analyse des assemblies) ou scan source (regex)
