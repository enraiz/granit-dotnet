# ADR-016 : Magick.NET — Traitement d'images

- **Statut** : Accepté
- **Date** : 2026-02-28
- **Issue** : [#363](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/363)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Imaging.MagickNet)

## Contexte

Le module `Granit.Imaging` fournit un pipeline de traitement d'images pour la
plateforme : redimensionnement, conversion de format, compression, ajout de
watermark, et extraction de métadonnées.

Les cas d'usage incluent : photos de profil, documents scannés, images médicales
(hors DICOM), et filigranes de conformité.

Les exigences sont :

- **Formats** : JPEG, PNG, WebP, TIFF, BMP, GIF (minimum)
- **Transformations** : resize, crop, rotate, watermark, format conversion
- **Cross-platform** : Linux (production K8s) et Windows (développement)
- **Licence** : compatible usage commercial

## Décision

**Magick.NET** (wrapper .NET d'ImageMagick, variante Q8) pour le traitement
d'images.

## Alternatives évaluées

### Option 1 : Magick.NET-Q8-AnyCPU (retenue)

- **Licence** : Apache-2.0
- **Avantage** : 200+ formats supportés, transformations complètes (resize, crop,
  watermark, compression), cross-platform (native wrapper), variante Q8
  (8 bits/canal — mémoire optimisée), API .NET fluent, très mature
  (ImageMagick : 25+ ans)

### Option 2 : ImageSharp (SixLabors)

- **Licence** : **Six Labors Split License** (v3+) — usage commercial nécessite
  une **licence payante** (enterprise $2,500/an)
- **Avantage** : 100 % managed .NET (pas de dépendance native), API moderne,
  performances excellentes, pipeline composable
- **Inconvénient** : changement de licence en 2023 (v3), coût récurrent pour
  les fonctionnalités de production, moins de formats que Magick.NET

### Option 3 : SkiaSharp

- **Licence** : MIT
- **Avantage** : moteur Skia (Google), performant pour le rendu 2D, cross-platform
- **Inconvénient** : orienté rendu/dessin (pas traitement d'images), API bas niveau
  pour les transformations courantes (resize, watermark), dépendance native
  Skia (~10 Mo), pas de support de tous les formats (pas de TIFF, WebP partiel)

### Option 4 : System.Drawing.Common

- **Licence** : MIT (Microsoft)
- **Avantage** : natif .NET Framework, API familière
- **Inconvénient** : **Windows-only** depuis .NET 6 (pas de support Linux — bloquant
  pour la production K8s), déprécié par Microsoft, fuites mémoire connues,
  pas thread-safe

## Justification

| Critère | Magick.NET | ImageSharp | SkiaSharp | System.Drawing |
| ------- | ---------- | ---------- | --------- | -------------- |
| Licence | Apache-2.0 | Commercial (v3+) | MIT | MIT |
| Coût | Gratuit | $2,500/an | Gratuit | Gratuit |
| Formats | 200+ | ~30 | ~15 | ~10 |
| Cross-platform | Oui (native) | Oui (managed) | Oui (native) | Windows only |
| Watermark | Natif | Natif | Manuel | Manuel |
| Maturité | 25+ ans (IM) | 7+ ans | 10+ ans | 20+ ans (déprécié) |
| Thread-safe | Oui | Oui | Partiel | Non |

## Conséquences

### Positives

- 200+ formats supportés, couvrant tous les cas d'usage actuels et futurs
- Apache-2.0 : pas de coût de licence (ImageSharp serait ~$2,500/an)
- Cross-platform : fonctionne sur Linux (K8s production) et Windows (dev)
- Variante Q8 : mémoire optimisée (8 bits/canal suffisent pour le web)
- Watermark natif pour les filigranes de conformité

### Négatives

- Dépendance native ImageMagick (libMagickWand) — gérée par le package
  AnyCPU mais peut poser des problèmes dans certains environnements Docker
  (nécessite les librairies système)
- API moins moderne que ImageSharp (wrapper d'une API C)
- Taille du package plus importante que les alternatives managed (~15 Mo)
- Vulnérabilités ImageMagick historiques (ImageTragick 2016) — mitigées par
  les policies de sécurité intégrées et les mises à jour régulières
