# Granit.Templating.Workflow

Bridge optionnel entre `Granit.Templating` et `Granit.Workflow`. Remplace le
`NullTemplateTransitionHook` par défaut par une implémentation Workflow qui fournit
la validation FSM, le routage d'approbation, l'audit ISO 27001 unifié (`WorkflowTransitionRecord`)
et les domain events.

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Templating.Workflow
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/templating/workflow-integration.md).
