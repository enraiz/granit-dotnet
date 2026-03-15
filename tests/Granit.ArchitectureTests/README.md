# Granit.ArchitectureTests

Architecture tests for the Granit framework using [ArchUnitNET](https://github.com/TngTech/ArchUnitNET).
Validates layered architecture, module conventions, class design, DTO naming, and CQRS patterns.

Part of the [granit](https://github.com/granit-fx/granit-dotnet) framework.

## Test categories

| File | Rules |
| ---- | ----- |
| `ModuleConventionTests` | `GranitModule` subclasses are `sealed`, naming `Granit*Module` |
| `LayerDependencyTests` | Core/Timing/Guids/Endpoints do not depend on EF Core |
| `ClassDesignTests` | `DbContext` subclasses are `sealed`, `Ef*Store` are not public |
| `DtoConventionTests` | No `*Dto` suffix in endpoint namespaces |
| `CqrsConventionTests` | `I*Reader`/`I*Writer` naming conventions |

## Usage

```bash
dotnet test tests/Granit.ArchitectureTests/
```
