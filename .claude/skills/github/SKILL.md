---
name: github
description: "GitHub operations: create, edit, link, list issues and manage Epic > Feature > Story hierarchy. Invoke before any GitHub operation."
argument-hint: "[operation] [args]"
---

# GitHub Operations

## Initialization

Before any command, detect the current repository:

```bash
REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
```

Use `$REPO` in `gh -R "$REPO"` commands.

## Issue types

| Type | Label | Title prefix | Parent | Parent section |
| ---- | ----- | ------------ | ------ | -------------- |
| Epic | `Type: Epic` | `[EPIC]` | - | - |
| Feature | `Type: Feature` | `[FEATURE]` | Epic | `## Features` |
| Story | `Type: Story` | `[STORY]` | Feature | `## User Stories` |
| Bug | `Type: Bug` | `[BUG]` | - | - |
| Spike | `Type: Spike` | `[SPIKE]` | - | - |
| Tech Debt | `Type: Tech Debt` | `[TECH DEBT]` | - | - |

Title rules: **no emoji**, prefix in brackets, concise.

## Hierarchy (GitHub Free)

No native sub-tasks. Hierarchy is managed by:

1. **`#N` references in the parent description** (dedicated section)
2. **Cross-references in comments** when linking issues

```text
Epic
 └── Feature (reference in ## Features)
      └── Story (reference in ## User Stories)
```

GitHub does not have a native `relates_to` link API like GitLab. Use description
references (`#N`) and comments to establish relationships.

## Understand before acting

Before creating, modifying, or closing an issue, always read the existing context:

- Read the full issue (description + all comments) before editing or closing
- For bugs or tech debt: check linked issues and related PRs to understand prior
  decisions and constraints that led to the current implementation
- If a fix seems obvious but the code looks intentionally written that way, search
  for the original issue or PR that introduced it before overriding
- Never close an issue as "duplicate" or "won't fix" without reading its full history

When investigating a bug or refactoring request, search for related issues:

```bash
gh issue list -R "$REPO" --search "keyword"
gh issue view {number} -R "$REPO" --json body -q .body
```

## Behavioral rules

### After creating an issue

1. **Always update** the parent description to add the reference
2. Format in the parent description: `- #N - short description`

### After closing an issue

1. Add a **closing comment** explaining what was delivered (files, decisions)
2. If it's the last Story of a Feature: check whether the Feature can be closed

### Descriptions

Always use a HEREDOC for multi-line descriptions:

```bash
--body "$(cat <<'EOF'
content here
EOF
)"
```

Templates are in `.github/ISSUE_TEMPLATE/`.
See [templates.md](templates.md) for structure per issue type.

### Security

- NEVER put tokens in plain text (gh uses its local config)
- NEVER put secrets in issue descriptions
- NEVER put PII in titles or descriptions

## Personas (user stories)

Two persona registries:

- **Infrastructure & governance (15 personas)**: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
- **Application-level (5 personas)**: `granit-dotnet/docs/guide/personas-applicatifs.md`

NEVER invent a new persona.

**Infra/governance personas:** SRE, Ingenieur DevOps, Developpeur, Architecte, DBA,
RSSI, DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe,
Utilisateur, Professionnel de sante, Product Owner

**Application personas:** Visiteur, Utilisateur authentifie, Administrateur d'application,
Approbateur, Gestionnaire de contenu

NEVER use hybrid roles ("slash roles" like `SRE / DevOps`).
Context (on-call, audit) goes in the story body, not in the persona.

## Resources

- Detailed workflows (create, link, close): [workflows.md](workflows.md)
- Description templates per type: [templates.md](templates.md)
- gh command reference: [reference.md](reference.md)
- Persona registry: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
