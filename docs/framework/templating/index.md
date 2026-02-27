# Templating et génération documentaire — Granit.Templating

Moteur de rendu de templates et génération de documents (PDF, Excel) pour les applications
Digital Dynamics. La source est toujours du HTML produit par le moteur Scriban ; les formats
binaires sont créés par des renderers dédiés.

| Package | Rôle |
| --- | --- |
| `Granit.Templating` | Socle générique : interfaces, pipeline, enrichisseurs, store |
| `Granit.Templating.Scriban` | Moteur Scriban sandboxé + contextes globaux (`now.*`, `context.*`) |
| `Granit.Templating.EntityFrameworkCore` | `IDocumentTemplateStore` EF Core — cycle de vie Draft/Published/Deprecated |
| `Granit.DocumentGeneration` | Façade `IDocumentGenerator`, `IDocumentRenderer`, `DocumentResult` |
| `Granit.DocumentGeneration.Pdf` | `PuppeteerSharpRenderer` — HTML → PDF via Chromium sans tête |

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
| `{{ context.culture }}` | Culture courante | `fr-BE` |
| `{{ context.culture_name }}` | Nom de la culture | `français (Belgique)` |
| `{{ context.tenant_id }}` | ID du tenant courant | `3fa85f64-…` |
| `{{ context.tenant_name }}` | Nom du tenant | `Hôpital Saint-Luc` |

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

Seuls les brouillons (`Draft`) peuvent être supprimés. Les révisions dépréciées sont
conservées 3 ans (obligation HDS article L. 1111-8 CSP).

## Installation

### 1 — Module

```csharp
// Avec moteur Scriban (recommandé)
[DependsOn(
    typeof(GranitTemplatingScribanModule),
    typeof(GranitDocumentGenerationModule))]
public sealed class MyAppModule : GranitModule { }
```

> `GranitTemplatingScribanModule` dépend déjà de `GranitTemplatingModule`.
> `GranitDocumentGenerationModule` dépend déjà de `GranitTemplatingModule`.

### 2 — Enregistrement des services

```csharp
// Templates embarqués dans l'assembly (résolution fallback)
builder.Services.AddEmbeddedTemplates(typeof(MyAppModule).Assembly);

// Enrichisseur de données
builder.Services.AddTemplateDataEnricher<InvoiceData, QrCodeEnricher>();

// Contexte global personnalisé (optionnel)
builder.Services.AddTemplateGlobalContext<MyCustomContext>();

// Renderer PDF (nécessite Granit.DocumentGeneration.Pdf)
builder.Services.AddDocumentRenderer<PuppeteerSharpRenderer>();
```

### 3 — Ressources embarquées

Convention de nommage pour les templates embarqués dans l'assembly :

```text
{AssemblyName}.Templates.{TemplateName}.html        (neutre)
{AssemblyName}.Templates.{TemplateName}.fr-BE.html  (spécifique culture)
```

Marquer le fichier comme ressource embarquée dans le `.csproj` :

```xml
<ItemGroup>
  <EmbeddedResource Include="Templates\*.html" />
</ItemGroup>
```

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

## Exceptions

| Classe | Déclencheur |
| --- | --- |
| `TemplateNotFoundException` | Aucun resolver n'a trouvé le template pour la clé et la culture demandées |
| `TemplateParseException` | Le source du template contient des erreurs de syntaxe Scriban |
| `DocumentRendererNotFoundException` | Aucun `IDocumentRenderer` enregistré pour le `DocumentFormat` demandé |

## Architecture interne

```text
ITextTemplateRenderer (TextTemplateRenderer — interne, scoped)
  ├── IEnumerable<ITemplateDataEnricher<TData>>   (résolution via IServiceProvider)
  ├── IEnumerable<ITemplateResolver>               (ordonnés par Priority)
  │     ├── EmbeddedTemplateResolver (Priority = -100)
  │     └── StoreTemplateResolver    (Priority = 100, via EntityFrameworkCore)
  ├── ITemplateEngine (ScribanTemplateEngine — singleton)
  └── IEnumerable<ITemplateGlobalContext>          (singletons)
        ├── NowGlobalContext            → now.*
        └── ExecutionContextGlobalContext → context.*

IDocumentGenerator (DocumentGenerator — interne, scoped)
  ├── ITextTemplateRenderer        (rendu HTML)
  └── IEnumerable<IDocumentRenderer>
        └── PuppeteerSharpRenderer (singleton, via Granit.DocumentGeneration.Pdf)
```

## Roadmap

| Story | Statut | Description |
| --- | --- | --- |
| #327 | ✅ Terminé | `TemplateType<TData>` et `TextTemplateType<TData>` — typage fort |
| #328 | ✅ Terminé | `ITemplateResolver` — chaîne de résolution par priorité |
| #329 | ✅ Terminé | `ScribanTemplateEngine` — rendu sandboxé, contextes globaux |
| #333 | ✅ Terminé | `ITemplateDataEnricher<TData>` — pipeline d'enrichissement immutable |
| #334 | ✅ Terminé | `EmbeddedTemplateResolver` — résolution depuis les ressources embarquées |
| #335 | ✅ Terminé | `IDocumentGenerator` et `IDocumentRenderer` — génération documentaire |
| #331 | 🔜 Planifié | `IDocumentTemplateStore` EF Core — Draft/Published/Deprecated |
| #330 | 🔜 Planifié | `PuppeteerSharpRenderer` — HTML → PDF |
| #336 | 🔜 Planifié | `Granit.DocumentGeneration.Excel` — ClosedXML |
| #340 | ⏸ Différé | PDF/A-3b — Factur-X (loi e-facture sept. 2026, licence iText7 en attente) |

## Conformité HDS / RGPD

- **Piste d'audit** : `TemplateRevision.RevisionId` est propagé dans `RenderedContent.RevisionId`,
  permettant de tracer quelle version du template a produit chaque document.
- **Immutabilité** : les révisions `Deprecated` ne sont jamais supprimées (3 ans HDS).
- **Données personnelles** : ne jamais exposer de PII dans les `ITemplateGlobalContext`.
  Les données sensibles (nom du patient, numéro de SS) transitent exclusivement via `TData`.
- **Sandbox Scriban** : un template compromis ne peut pas accéder au système de fichiers,
  au réseau ou à la réflexion .NET.

## Dépendances Granit

| Package | Dépend de |
| --- | --- |
| `Granit.Templating` | `Granit.Core`, `Granit.Timing` |
| `Granit.Templating.Scriban` | `Granit.Templating`, `Granit.Timing`, `Scriban 5.*` |
| `Granit.Templating.EntityFrameworkCore` | `Granit.Templating` |
| `Granit.DocumentGeneration` | `Granit.Templating` |
| `Granit.DocumentGeneration.Pdf` | `Granit.DocumentGeneration`, `PuppeteerSharp` |

> Voir le [graphe de dépendances complet](../dependencies.md).
