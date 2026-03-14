# Granit.Templating.Workflow

Optional bridge between `Granit.Templating` and `Granit.Workflow`. Replaces the
default `NullTemplateTransitionHook` with a Workflow-backed implementation that provides
FSM validation, approval routing, unified ISO 27001 audit trail (`WorkflowTransitionRecord`)
and domain events.

Part of the [granit](https://granit-fx.dev) framework.

## Installation

```bash
dotnet add package Granit.Templating.Workflow
```

## Dependencies

- `Granit.Templating`
- `Granit.Workflow`

## Documentation

See the [full documentation](https://granit-fx.dev).
