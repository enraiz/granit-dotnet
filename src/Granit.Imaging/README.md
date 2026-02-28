# Granit.Imaging

Image processing abstraction layer for Granit applications. Provides `IImageProcessor`
and `IImagePipeline` — a fluent API for resize, crop, compress, convert, watermark
and metadata stripping operations.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Imaging
```

This package contains interfaces only. Install an implementation package such as
`Granit.Imaging.MagickNet` for concrete image processing.

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/imaging/index.md).
