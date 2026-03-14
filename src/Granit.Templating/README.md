# Granit.Templating

Generic templating pipeline for Granit. Provides the resolution → rendering pipeline with
`ITemplateResolver`, `ITemplateEngine`, `ITemplateEnricher`, and `RenderedContent`.
Concrete engines and renderers are provided separately (`Granit.Templating.Scriban`,
`Granit.DocumentGeneration.*`).

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Templating
```

## Dependencies

- `Granit.Timing`

## Documentation

See the [full documentation](https://granit-fx.dev).
