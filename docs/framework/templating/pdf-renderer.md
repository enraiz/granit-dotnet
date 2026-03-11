# Rendu PDF via PuppeteerSharp

## Vue d'ensemble

Le package `Granit.DocumentGeneration.Pdf` fournit un `IDocumentRenderer` basé sur
[PuppeteerSharp](https://github.com/nickolay/puppeteer-sharp) (Chromium headless, licence MIT).
Il convertit le HTML produit par le pipeline de templating en documents PDF avec un rendu
CSS3 pixel-perfect.

**Souveraineté** : Chromium tourne on-premise sur infrastructure souveraine — aucun appel réseau externe
pendant le rendu.

## Installation

```bash
dotnet add package Granit.DocumentGeneration.Pdf
```

## Enregistrement des services

```csharp
// Avec le système de modules Granit
[DependsOn(typeof(GranitDocumentGenerationPdfModule))]
public sealed class MyAppModule : GranitModule { }

// Ou manuellement via IServiceCollection
services.AddGranitDocumentGeneration();
services.AddGranitDocumentGenerationPdf();
```

Services enregistrés :

| Service             | Implémentation            | Lifetime  |
| ------------------- | ------------------------- | --------- |
| `IDocumentRenderer` | `PuppeteerSharpRenderer`  | Singleton |
| `IHostedService`    | `ChromiumLifetimeService` | Singleton |

## Configuration

Section `DocumentGeneration:Pdf` dans `appsettings.json` :

```json
{
  "DocumentGeneration": {
    "Pdf": {
      "PaperFormat": "A4",
      "Landscape": false,
      "MarginTop": "10mm",
      "MarginBottom": "10mm",
      "MarginLeft": "10mm",
      "MarginRight": "10mm",
      "PrintBackground": true,
      "HeaderTemplate": "<div style='font-size:8px;text-align:center;width:100%'>Acme Corp — Page <span class='pageNumber'></span>/<span class='totalPages'></span></div>",
      "FooterTemplate": null,
      "ChromiumExecutablePath": null,
      "MaxConcurrentPages": 4
    }
  }
}
```

### Options détaillées

| Option                         | Type      | Défaut   | Description                                              |
| ------------------------------ | --------- | -------- | -------------------------------------------------------- |
| `PaperFormat`                  | `string`  | `"A4"`   | Format du papier (A0–A6, Letter, Legal, Tabloid, Ledger) |
| `Landscape`                    | `bool`    | `false`  | Orientation paysage                                      |
| `MarginTop/Bottom/Left/Right`  | `string`  | `"10mm"` | Marges en unités CSS (mm, cm, px)                        |
| `PrintBackground`              | `bool`    | `true`   | Imprimer les fonds et images d'arrière-plan              |
| `HeaderTemplate`               | `string?` | `null`   | Template HTML pour l'en-tête de page                     |
| `FooterTemplate`               | `string?` | `null`   | Template HTML pour le pied de page                       |
| `ChromiumExecutablePath`       | `string?` | `null`   | Chemin vers un Chromium personnalisé                     |
| `MaxConcurrentPages`           | `int`     | `4`      | Nombre maximum de pages Chromium simultanées             |

### Classes CSS pour les en-têtes et pieds de page

PuppeteerSharp injecte automatiquement les valeurs suivantes dans les templates :

- `pageNumber` — numéro de page courant
- `totalPages` — nombre total de pages
- `date` — date de génération
- `title` — titre du document
- `url` — URL de la page

## Utilisation

```csharp
// Génération via le pipeline complet (recommandé)
DocumentResult result = await documentGenerator.GenerateAsync(
    InvoiceTemplate.Instance,
    invoiceData,
    DocumentFormat.Pdf);

// Utilisation directe du renderer (avancé)
IDocumentRenderer renderer = serviceProvider
    .GetServices<IDocumentRenderer>()
    .First(r => r.CanRender(DocumentFormat.Pdf));

DocumentResult result = await renderer.RenderAsync(
    "<h1>Facture</h1><p>Total : 100 €</p>",
    DocumentFormat.Pdf);

// Accès au résultat
byte[] pdfBytes = result.Content.ToArray();
// Les magic bytes %PDF confirment un PDF valide
```

## Architecture

### Cycle de vie de Chromium

`ChromiumLifetimeService` implémente `IHostedService` et gère le navigateur Chromium :

1. **Démarrage** (`StartAsync`) : télécharge Chromium si nécessaire, lance le navigateur
   headless
2. **Rendu** : chaque appel à `RenderAsync` ouvre un nouvel onglet (page), génère le PDF,
   puis ferme l'onglet
3. **Arrêt** (`StopAsync`) : ferme proprement le navigateur

### Contrôle de la concurrence

Un `SemaphoreSlim` limite le nombre d'onglets Chromium ouverts simultanément
(`MaxConcurrentPages`). Cela évite la surcharge mémoire lors de pics de demandes.

## Déploiement Docker

Chromium nécessite des dépendances système. Exemple de `Dockerfile` :

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Installer les dépendances Chromium
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    libappindicator3-1 \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libx11-xcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    xdg-utils \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Utiliser le Chromium installé par le système
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
```

Puis dans `appsettings.Production.json` :

```json
{
  "DocumentGeneration": {
    "Pdf": {
      "ChromiumExecutablePath": "/usr/bin/chromium"
    }
  }
}
```

## PDF/A-3b (Factur-X)

Le package fournit l'abstraction `IPdfAConverter` pour la conversion en PDF/A-3b,
nécessaire pour :

- **Factur-X** (NF Z 55-140) : facturation électronique obligatoire en France
  à partir de septembre 2026
- **Archivage ISO 27001** : conservation long terme de documents médicaux

```csharp
public interface IPdfAConverter
{
    Task<DocumentResult> ConvertToPdfAAsync(
        DocumentResult pdfResult,
        PdfAConversionOptions options,
        CancellationToken cancellationToken = default);
}
```

Niveaux de conformité supportés :

| Niveau   | Usage                                    |
| -------- | ---------------------------------------- |
| `PdfA3b` | Factur-X (PDF/A-3b + XML ZUGFeRD inclus) |
| `PdfA2a` | Archivage long terme documents ISO 27001       |

L'implémentation concrète nécessite une bibliothèque PDF/A (ex. iText7).
La question de licence (AGPL vs commerciale) est en cours de validation.

## Observabilité

Le renderer utilise `ILogger<PuppeteerSharpRenderer>` pour journaliser :

- Taille du PDF généré (niveau Debug)
- Format de papier utilisé (niveau Debug)

Aucun contenu HTML n'est journalisé (conformité RGPD/ISO 27001).
