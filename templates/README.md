# Granit.Templates

Project templates for the Granit framework, installable via `dotnet new`.

## Installation

```bash
dotnet new install Granit.Templates
```

## Available templates

| Template | Short name | Description |
| --- | --- | --- |
| Granit Minimal API | `granit-api` | Minimal API with Bundle.Essentials and a sample endpoint |
| Granit Full API | `granit-api-full` | Complete API with Bundle.Api, Keycloak, Identity, Notifications |
| Granit Module Library | `granit-module` | Scaffolding for a new Granit module (csproj, module class, extension) |

## Usage

### Minimal API

```bash
dotnet new granit-api -n MyApi
cd MyApi
dotnet run
```

### Full API

```bash
dotnet new granit-api-full -n MyApi
cd MyApi
# Configure appsettings.json (ConnectionStrings, Keycloak)
dotnet run
```

### Module library

```bash
dotnet new granit-module -n Granit.Billing
```

This generates:

- `Granit.Billing.csproj`
- `GranitBillingModule.cs`
- `Extensions/BillingServiceCollectionExtensions.cs`
- `README.md`

## Local development

To test templates locally without publishing:

```bash
dotnet new install ./templates
```

To uninstall:

```bash
dotnet new uninstall ./templates
```

## Publishing

```bash
cd templates
dotnet pack -c Release -o ../nupkgs
```
