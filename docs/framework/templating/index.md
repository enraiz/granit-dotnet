# Templating et génération documentaire — Granit.Templating

Moteur de rendu de templates et génération de documents (PDF, Excel) pour les applications
Digital Dynamics. La source est toujours du HTML produit par le moteur Scriban ; les formats
binaires sont créés par des renderers dédiés.

| Package | Rôle |
| --- | --- |
| `Granit.Templating` | Socle générique : interfaces, pipeline, enrichisseurs |
| `Granit.Templating.Scriban` | Moteur Scriban 6 sandboxé + contextes globaux (`now.*`, `context.*`) |
| `Granit.Templating.EntityFrameworkCore` | `IDocumentTemplateStore` EF Core — cycle de vie Draft/Published/Deprecated |
| `Granit.DocumentGeneration` | Façade `IDocumentGenerator`, `IDocumentRenderer`, `DocumentResult` |
| `Granit.DocumentGeneration.Pdf` | `PuppeteerSharpRenderer` — HTML → PDF via Chromium sans tête *(à venir)* |
| `Granit.DocumentGeneration.Excel` | `ClosedXmlTemplateEngine` — génération de tableurs *.xlsx* *(à venir)* |

## Pipeline complet

```text
TData (brut)
  → ITemplateDataEnricher<TData>[]   (enrichissement ordonné, immutable)
  → ITemplateResolver[]              (chaîne par Priority, fallback culture)
  → ITemplateEngine (Scriban)        (rendu HTML sandboxé)
  → IDocumentRenderer                (optionnel — HTML → PDF/Excel)
  → DocumentResult
```

## Concepts clés

### Types de templates

```csharp
// Template textuel (email, SMS, notification)
public abstract class TextTemplateType<TData> : TemplateType<TData> where TData : notnull
{
    public abstract string Name { get; }
}

// Template documentaire — source HTML, sortie binaire
public abstract class DocumentTemplateType<TData> : TextTemplateType<TData> where TData : notnull
{
    public virtual DocumentFormat DefaultFormat => DocumentFormat.Pdf;
}
```

Définir ses propres types :

```csharp
public sealed class WelcomeEmailType : TextTemplateType<WelcomeEmailData>
{
    public override string Name => "Notifications.WelcomeEmail";
}

public sealed class InvoiceTemplateType : DocumentTemplateType<InvoiceData>
{
    public override string Name => "Billing.Invoice";
    // DefaultFormat = Pdf par défaut
}
```

### Résolution de template

Les templates sont résolus par une chaîne d'`ITemplateResolver`, ordonnée par `Priority`
décroissante. La résolution tente d'abord la clé avec la culture courante, puis la clé neutre :

```text
Résolution de "Billing.Invoice" avec culture "fr-BE" :
  1. Tous les resolvers → clé (name="Billing.Invoice", culture="fr-BE")
  2. Si non trouvé → clé (name="Billing.Invoice", culture=null)
  3. Si non trouvé → TemplateNotFoundException
```

| Resolver | Priorité | Source |
| --- | --- | --- |
| `StoreTemplateResolver` | 100 | `IDocumentTemplateStore` (EF Core) — templates publiés |
| `EmbeddedTemplateResolver` | -100 | Ressources embarquées dans l'assembly — fallback code |

### Enrichissement des données

`ITemplateDataEnricher<TData>` permet d'enrichir le modèle avant le rendu (ex. : génération
de QR code, pré-chargement d'un blob distant, calcul de signature). Le modèle est immutable :
les enrichisseurs retournent un nouveau `TData` via `record with`.

```csharp
public sealed class QrCodeEnricher : ITemplateDataEnricher<InvoiceData>
{
    public int Order => 10;

    public Task<InvoiceData> EnrichAsync(InvoiceData data, CancellationToken ct = default)
    {
        string svg = QrCodeGenerator.Generate(data.InvoiceNumber);
        return Task.FromResult(data with { QrCodeSvg = svg });
    }
}
```

### Moteur Scriban

Le moteur Scriban expose `TData` sous la variable `model` avec des noms en snake\_case.

```html
<!-- Template HTML Scriban -->
<h1>Facture {{ model.invoice_number }}</h1>
<p>Client : {{ model.customer_name }}</p>
<p>Date : {{ now.date }}</p>
<p>Culture : {{ context.culture }}</p>
```

Variables globales disponibles sans configuration :

| Variable | Valeur | Exemple |
| --- | --- | --- |
| `{{ now.date }}` | Date locale | `27/02/2026` |
| `{{ now.datetime }}` | Date et heure locale | `27/02/2026 14:35` |
| `{{ now.iso }}` | Format ISO 8601 | `2026-02-27T14:35:00+00:00` |
| `{{ now.year }}` | Année | `2026` |
| `{{ now.month }}` | Mois | `02` |
| `{{ now.day }}` | Jour | `27` |
| `{{ now.time }}` | Heure | `14:35` |
| `{{ context.culture }}` | Culture courante (BCP 47) | `fr-BE` |
| `{{ context.culture_name }}` | Nom de la culture | `français (Belgique)` |
| `{{ context.tenant_id }}` | ID du tenant courant | `3fa85f64-…` ou vide |
| `{{ context.tenant_name }}` | Nom du tenant | `Hôpital Saint-Luc` ou vide |

> **Sécurité :** les templates s'exécutent dans un `TemplateContext` sandboxé
> (`EnableRelaxedMemberAccess = false`). Aucun accès I/O, réseau ou réflexion .NET
> n'est possible depuis un template.

### Cycle de vie des templates (IDocumentTemplateStore)

```text
Draft → Published → Deprecated
```

| État | Règle |
| --- | --- |
| `Draft` | Éditable ; jamais utilisé par le pipeline de rendu |
| `Published` | Version active ; une seule par clé à un instant donné |
| `Deprecated` | Conservé pour la piste d'audit HDS — **jamais supprimé physiquement** |

Seuls les brouillons (`Draft`) peuvent être supprimés physiquement. Les révisions dépréciées
sont conservées sans limite de durée (obligation HDS, article L. 1111-8 CSP — 3 ans minimum).

## Installation

### 1 — Modules

```csharp
// Avec moteur Scriban et store EF Core (configuration complète recommandée)
[DependsOn(
    typeof(GranitTemplatingScribanModule),
    typeof(GranitTemplatingEntityFrameworkCoreModule),
    typeof(GranitDocumentGenerationModule))]
public sealed class MyAppModule : GranitModule { }
```

> `GranitTemplatingScribanModule` dépend déjà de `GranitTemplatingModule`.
> `GranitDocumentGenerationModule` dépend déjà de `GranitTemplatingModule`.

### 2 — Enregistrement des services

```csharp
// Moteur Scriban (ITemplateEngine + contextes globaux now.* et context.*)
builder.Services.AddGranitTemplatingWithScriban();

// Store EF Core (IDocumentTemplateStore + StoreTemplateResolver, Priority = 100)
builder.AddGranitTemplatingEntityFrameworkCore(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Templates embarqués dans l'assembly (résolution fallback, Priority = -100)
builder.Services.AddEmbeddedTemplates(typeof(MyAppModule).Assembly);

// Enrichisseur de données (optionnel)
builder.Services.AddTemplateDataEnricher<InvoiceData, QrCodeEnricher>();

// Contexte global personnalisé (optionnel)
builder.Services.AddTemplateGlobalContext<MyCustomContext>();

// Façade de génération documentaire
builder.Services.AddGranitDocumentGeneration();

// Renderer PDF (nécessite Granit.DocumentGeneration.Pdf — à venir)
builder.Services.AddDocumentRenderer<PuppeteerSharpRenderer>();
```

### 3 — Ressources embarquées

Convention de nommage pour les templates embarqués dans l'assembly :

```text
{AssemblyName}.Templates.{TemplateName}.html        (neutre)
{AssemblyName}.Templates.{TemplateName}.fr.html     (spécifique culture)
```

Marquer les fichiers comme ressources embarquées dans le `.csproj` :

```xml
<ItemGroup>
  <!-- Fichier neutre -->
  <EmbeddedResource Include="Templates/Billing.Invoice.html"
      LogicalName="$(RootNamespace).Templates.Billing.Invoice.html" />

  <!-- Fichier spécifique à une culture -->
  <!-- WithCulture="false" empêche MSBuild de le placer dans une assembly satellite -->
  <EmbeddedResource Include="Templates/Billing.Invoice.fr.html"
      LogicalName="$(RootNamespace).Templates.Billing.Invoice.fr.html"
      WithCulture="false" />
</ItemGroup>
```

> **Important :** tout fichier dont le nom contient un code de culture reconnu
> (ex. `.fr.html`, `.fr-BE.html`) est automatiquement placé par MSBuild dans une
> assembly satellite (`fr/MyApp.resources.dll`) et devient **invisible** à
> `GetManifestResourceStream`. Les attributs `LogicalName` + `WithCulture="false"`
> sont obligatoires pour les templates culture-spécifiques embarqués.

## Utilisation

### Rendu d'un template textuel (email)

```csharp
public sealed class WelcomeEmailService(ITextTemplateRenderer renderer)
{
    public async Task<string> GetHtmlAsync(
        WelcomeEmailData data, CancellationToken ct)
    {
        RenderedTextResult result = await renderer.RenderAsync(
            new WelcomeEmailType(), data, ct);

        return result.Html;
    }
}
```

### Génération d'un document (PDF)

```csharp
public sealed class InvoiceService(IDocumentGenerator generator)
{
    public async Task<DocumentResult> GenerateInvoiceAsync(
        InvoiceData data, CancellationToken ct)
    {
        return await generator.GenerateAsync(
            new InvoiceTemplateType(), data, ct: ct);
    }
}
```

Forcer un format différent du défaut :

```csharp
DocumentResult excel = await generator.GenerateAsync(
    new InvoiceTemplateType(), data,
    targetFormat: DocumentFormat.Excel,
    ct: ct);
```

### Administration des templates (store EF Core)

```csharp
public sealed class TemplateAdminService(IDocumentTemplateStore store)
{
    // Créer ou mettre à jour un brouillon
    public Task SaveDraftAsync(TemplateKey key, string html, CancellationToken ct)
        => store.SaveDraftAsync(key, html, "text/html", "admin@digitaldynamics.be", ct);

    // Publier le brouillon courant
    public Task PublishAsync(TemplateKey key, CancellationToken ct)
        => store.PublishAsync(key, "admin@digitaldynamics.be", ct);

    // Consulter l'historique complet (audit HDS)
    public Task<IReadOnlyList<TemplateDescriptor>> GetHistoryAsync(
        TemplateKey key, CancellationToken ct)
        => store.GetHistoryAsync(key, ct);
}
```

## Exceptions

| Classe | Déclencheur |
| --- | --- |
| `TemplateNotFoundException` | Aucun resolver n'a trouvé le template pour la clé et la culture demandées |
| `TemplateParseException` | Le source du template contient des erreurs de syntaxe Scriban |
| `DocumentRendererNotFoundException` | Aucun `IDocumentRenderer` enregistré pour le `DocumentFormat` demandé |

## Architecture interne

```text
ITextTemplateRenderer (TextTemplateRenderer — internal, scoped)
  ├── IEnumerable<ITemplateDataEnricher<TData>>   (résolution via IServiceProvider)
  ├── IEnumerable<ITemplateResolver>               (ordonnés par Priority décroissante)
  │     ├── EmbeddedTemplateResolver  (Priority = -100)
  │     └── StoreTemplateResolver     (Priority = 100, via Granit.Templating.EntityFrameworkCore)
  ├── ITemplateEngine (ScribanTemplateEngine — singleton)
  └── IEnumerable<ITemplateGlobalContext>          (singletons)
        ├── NowGlobalContext              → now.*
        └── ExecutionContextGlobalContext → context.*

IDocumentGenerator (DocumentGenerator — internal, scoped)
  ├── ITextTemplateRenderer        (rendu HTML)
  └── IEnumerable<IDocumentRenderer>
        ├── PuppeteerSharpRenderer (singleton, via Granit.DocumentGeneration.Pdf — à venir)
        └── ClosedXmlRenderer      (singleton, via Granit.DocumentGeneration.Excel — à venir)
```

## Roadmap

| Story | Statut | Description |
| --- | --- | --- |
| #327 | ✅ Terminé | Interfaces du pipeline (`ITextTemplateRenderer`, `ITemplateEngine`, `ITemplateResolver`) |
| #328 | ✅ Terminé | Types de template fortement typés (`TextTemplateType<TData>`, `DocumentTemplateType<TData>`) |
| #329 | ✅ Terminé | Rendu Scriban 6 sandboxé — scalaires, collections, conditions, snake_case |
| #331 | ✅ Terminé | Store EF Core — cycle de vie Draft/Published/Deprecated + historique audit HDS |
| #333 | ✅ Terminé | Traçabilité HDS — `RevisionId` propagé du store jusqu'au `DocumentResult` |
| #335 | ✅ Terminé | Documentation complète |
| #336 | ✅ Terminé | Variables globales Scriban : `NowGlobalContext` et `ExecutionContextGlobalContext` |
| #338 | ✅ Terminé | Façade `IDocumentGenerator` et pipeline d'orchestration binaire |
| #339 | ✅ Terminé | Pipeline d'enrichissement `ITemplateDataEnricher<TData>` |
| #330 | 🔜 Planifié | `PuppeteerSharpRenderer` — HTML → PDF via Chromium sans tête |
| #332 | 🔜 Planifié | Cache hybride des templates résolus avec invalidation sur publication |
| #334 | 🔜 Planifié | `Granit.DocumentGeneration.Excel` — tableurs *.xlsx* via ClosedXML |
| #340 | ⏸ Différé | PDF/A-3b — Factur-X (loi e-facture sept. 2026, licence iText7 en attente) |

## Conformité HDS / RGPD

- **Piste d'audit** : `TemplateRevision.RevisionId` est propagé dans `RenderedContent.RevisionId`,
  permettant de tracer quelle version du template a produit chaque document.
- **Immutabilité** : les révisions `Deprecated` ne sont jamais supprimées physiquement.
- **Données personnelles** : ne jamais exposer de PII dans les `ITemplateGlobalContext`.
  Les données sensibles (nom du patient, numéro de SS) transitent exclusivement via `TData`.
- **Sandbox Scriban** : un template compromis ne peut pas accéder au système de fichiers,
  au réseau ou à la réflexion .NET.

## Dépendances Granit

| Package | Dépend de |
| --- | --- |
| `Granit.Templating` | `Granit.Core`, `Granit.Timing` |
| `Granit.Templating.Scriban` | `Granit.Templating`, `Granit.Timing`, `Scriban 6.*` |
| `Granit.Templating.EntityFrameworkCore` | `Granit.Templating`, EF Core 10 |
| `Granit.DocumentGeneration` | `Granit.Templating` |
| `Granit.DocumentGeneration.Pdf` | `Granit.DocumentGeneration`, `PuppeteerSharp` |
| `Granit.DocumentGeneration.Excel` | `Granit.DocumentGeneration`, `ClosedXML` |

> Voir le [graphe de dépendances complet](../dependencies.md).
