# Imaging — Manipulation d'images

## Présentation

Le module **Granit.Imaging** fournit une couche d'abstraction fluide pour la manipulation
d'images dans les applications Granit. Il permet de redimensionner, recadrer, compresser,
convertir et nettoyer les métadonnées des images via une API chaînable.

L'architecture suit le pattern Granit habituel :

| Package | Rôle |
| ------- | ---- |
| `Granit.Imaging` | Interfaces, DTOs, enums (zéro dépendance tierce) |
| `Granit.Imaging.MagickNet` | Implémentation via Magick.NET (Apache 2.0) |

## Installation

```bash
dotnet add package Granit.Imaging.MagickNet
```

Le package d'implémentation référence automatiquement `Granit.Imaging`.

## Configuration

Enregistrez le module dans votre application :

```csharp
[DependsOn(typeof(GranitImagingMagickNetModule))]
public sealed class MyAppModule : GranitModule { }
```

Ou via les extensions DI :

```csharp
services.AddGranitImagingMagickNet();
```

## Utilisation

### API fluide

```csharp
await using IImagePipeline pipeline = imageProcessor.Load(stream);
ImageResult result = await pipeline
    .Resize(800, 600, ResizeMode.Crop)
    .Compress(quality: 75)
    .StripMetadata()
    .SaveAsWebPAsync();

// result.Content     → ReadOnlyMemory<byte>
// result.Format      → ImageFormat.WebP
// result.Width       → 800
// result.Height      → 600
```

### Méthodes de commodité

Des extensions simplifient la conversion vers les formats courants :

```csharp
ImageResult jpeg = await pipeline.SaveAsJpegAsync();
ImageResult png  = await pipeline.SaveAsPngAsync();
ImageResult webp = await pipeline.SaveAsWebPAsync();
ImageResult avif = await pipeline.SaveAsAvifAsync();
```

### Écriture vers un Stream

```csharp
await using IImagePipeline pipeline = imageProcessor.Load(inputStream);
await pipeline
    .Resize(400, 400, ResizeMode.Max)
    .ConvertTo(ImageFormat.WebP)
    .SaveToStreamAsync(outputStream);
```

### Watermark

```csharp
await using IImagePipeline pipeline = imageProcessor.Load(photoStream);
ImageResult result = await pipeline
    .Watermark(logoBytes, WatermarkPosition.BottomRight, opacity: 0.3f)
    .SaveAsJpegAsync();
```

## Modes de redimensionnement

| Mode | Comportement | Aspect ratio |
| ---- | ------------ | ------------ |
| `Max` | S'inscrit dans les bornes (peut être plus petit) | Préservé |
| `Pad` | S'inscrit dans les bornes + padding transparent | Préservé |
| `Crop` | Remplit exactement les bornes, recadrage centré | Préservé |
| `Stretch` | Remplit exactement les bornes | Déformé |
| `Min` | Couvre au minimum les bornes | Préservé |

## Formats supportés

| Format | Extension | Lossy | Transparency | Usage typique |
| ------ | --------- | ----- | ------------ | ------------- |
| JPEG | `.jpg` | Oui | Non | Photos, avatars |
| PNG | `.png` | Non | Oui | Logos, captures d'écran |
| WebP | `.webp` | Les deux | Oui | Web (remplacement JPEG/PNG) |
| AVIF | `.avif` | Les deux | Oui | Web (meilleure compression) |
| GIF | `.gif` | Non | Limitée | Animations simples |
| BMP | `.bmp` | Non | Non | Compatibilité legacy |
| TIFF | `.tiff` | Non | Oui | Impression, imagerie médicale |

## Conformité RGPD

La méthode `StripMetadata()` supprime toutes les métadonnées EXIF, IPTC et XMP de l'image.
Cela est essentiel pour la **pseudonymisation** dans le cadre du RGPD : les métadonnées
EXIF peuvent contenir des coordonnées GPS, des informations sur l'appareil photo, la date
et l'heure de la prise de vue, et même le nom du propriétaire.

```csharp
// Toujours stripper les métadonnées des images uploadées par les utilisateurs
ImageResult safe = await imageProcessor.Load(uploadedFile)
    .StripMetadata()
    .SaveAsWebPAsync();
```

## Performance

- **Traitement en mémoire** : aucune écriture temporaire sur disque
- **`ReadOnlyMemory<byte>`** : résultat immutable, compatible zero-copy
- **`IAsyncDisposable`** : libération déterministe des ressources natives
- **`CancellationToken`** : annulation supportée sur toutes les opérations terminales

## Licence

Le package `Granit.Imaging.MagickNet` utilise **Magick.NET** sous licence
[Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0). Cette licence est compatible
avec un usage propriétaire et commercial sans restriction.

Le package natif `Magick.NET-Q8-AnyCPU` (8 bits par canal) est utilisé par défaut.
Pour des cas nécessitant une précision supérieure (imagerie médicale haute fidélité),
`Magick.NET-Q16-AnyCPU` (16 bits par canal) peut être substitué.
