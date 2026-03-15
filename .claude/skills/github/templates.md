# Description templates

## Existing templates

GitHub issue templates are in `.github/ISSUE_TEMPLATE/` (YAML format).

| Template | File | Usage |
| -------- | ---- | ----- |
| Bug report | `bug_report.yml` | Bug reports with reproduction steps |
| Feature request | `feature_request.yml` | Feature requests |

For issue types without a GitHub template (Story, Feature, Epic, Spike, Tech Debt),
create issues with `gh issue create` and pass the body via HEREDOC.

## Usage with gh

```bash
gh issue create -R "$REPO" \
  --title "[TYPE] Titre" \
  --label "Type: XYZ" \
  --body "$(cat <<'EOF'
# Contenu adapte du template
EOF
)"
```

## Required sections by type

### Story -- required sections

- `## User Story` (En tant que / Je souhaite / Afin de)
- `## Contexte`
- `## Criteres d'acceptation` (Given/When/Then format)
- `## Definition of Done`

### Feature -- required sections

- `## Description` (Probleme / Solution / Alternatives)
- `## User Stories` (placeholder for links)
- `## Livrables attendus`
- `## Compliance` (Europe/encryption/audit checklist)

### Epic -- required sections

- `## Objectif`
- `## Features` (placeholder for links)
- `## Contraintes` (RGPD/ISO 27001)
- `## Criteres de succes`

### Bug -- required sections

- `## Description du bug`
- `## Etapes pour reproduire`
- `## Comportement attendu` / `## Comportement actuel`
- `## Environnement`
- `## Severite`
- `## Impact securite`

### Spike -- required sections

- `## Objectif`
- `## Questions a repondre`
- `## Timebox`
- `## Livrables attendus`
- `## Resultat` (to be filled after)

### Tech Debt -- required sections

- `## Description`
- `## Impact` (performance, maintenabilite, securite, compliance)
- `## Risques si non traite`
- `## Solution proposee`
- `## Fichiers / Modules concernes`
