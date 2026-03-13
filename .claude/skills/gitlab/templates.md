# Description templates

## Existing templates

GitLab templates are in `.gitlab/issue_templates/`.
Read the relevant template file before creating an issue to align with its structure.

| Template | File | Usage |
| -------- | ---- | ----- |
| Story | `Story.md` | User stories with Given/When/Then format |
| Feature | `Feature.md` | Features with compliance checklist |
| Epic | `Epic.md` | Epics with architecture and constraints |
| Bug | `Bug.md` | Bugs with severity and security impact |
| Spike | `Spike.md` | Technical research with timebox |
| Tech Debt | `Tech_Debt.md` | Technical debt with impact and risks |
| Infrastructure | `Infrastructure.md` | Infra changes with deployment plan and rollback |
| Incident | `Incident.md` | Production incidents with timeline and RCA |
| Vault Secret | `VaultSecret.md` | Secret management with RGPD/ISO 27001 compliance |

## Usage with glab

`glab issue create` does not directly support GitLab templates.
To use a template:

1. Read the template with `Read` to understand its structure
2. Adapt the content for the specific context
3. Pass via HEREDOC:

```bash
glab -R "$PROJECT" issue create \
  --title "[TYPE] Titre" \
  --label "Type::XYZ" \
  --description "$(cat <<'EOF'
# Contenu adapté du template
EOF
)"
```

## Required sections by type

### Story — required sections

- `## User Story` (En tant que / Je souhaite / Afin de)
- `## Contexte`
- `## Critères d'acceptation` (Given/When/Then format)
- `## Definition of Done`

### Feature — required sections

- `## Description` (Problème / Solution / Alternatives)
- `## User Stories` (placeholder for links)
- `## Livrables attendus`
- `## Compliance` (Europe/encryption/audit checklist)

### Epic — required sections

- `## Objectif`
- `## Features` (placeholder for links)
- `## Contraintes` (RGPD/ISO 27001)
- `## Critères de succès`

### Bug — required sections

- `## Description du bug`
- `## Étapes pour reproduire`
- `## Comportement attendu` / `## Comportement actuel`
- `## Environnement`
- `## Sévérité`
- `## Impact sécurité`

### Spike — required sections

- `## Objectif`
- `## Questions à répondre`
- `## Timebox`
- `## Livrables attendus`
- `## Résultat` (to be filled after)

### Tech Debt — required sections

- `## Description`
- `## Impact` (performance, maintenabilité, sécurité, compliance)
- `## Risques si non traité`
- `## Solution proposée`
- `## Fichiers / Modules concernés`
