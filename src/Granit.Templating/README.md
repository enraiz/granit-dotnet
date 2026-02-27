# Granit.Templating

Generic templating pipeline for Granit. Provides the resolution → rendering pipeline with
`ITemplateResolver`, `ITemplateEngine`, `ITemplateEnricher`, and `RenderedContent`.
Concrete engines and renderers are provided separately (`Granit.Templating.Scriban`,
`Granit.DocumentGeneration.*`).

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Templating
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/templating/index.md).
