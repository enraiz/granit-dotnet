---
name: gitlab
description: "GitLab operations: create, edit, link, list issues and manage Epic > Feature > Story hierarchy. Invoke before any GitLab operation."
argument-hint: "[operation] [args]"
---

# GitLab Operations

## Initialization

Before any command, detect the current project:

```bash
PROJECT=$(git remote get-url origin | sed -E 's|ssh://git@[^/]+:[0-9]+/||;s|https?://[^/]+/||;s|git@[^:]+:||;s|\.git$||')
PROJECT_ENCODED=$(echo "$PROJECT" | sed 's|/|%2F|g')
```

Use `$PROJECT` in `glab -R "$PROJECT"` and `$PROJECT_ENCODED` in `glab api`.

## Issue types

| Type | Label | Title prefix | Parent | Parent section |
| ---- | ----- | ------------ | ------ | -------------- |
| Epic | `Type::Epic` | `[EPIC]` | - | - |
| Feature | `Type::Feature` | `[FEATURE]` | Epic | `## Features` |
| Story | `Type::Story` | `[STORY]` | Feature | `## User Stories` |
| Bug | `Type::Bug` | `[BUG]` | - | - |
| Spike | `Type::Spike` | `[SPIKE]` | - | - |
| Tech Debt | `Type::Tech Debt` | `[TECH DEBT]` | - | - |
| Infrastructure | `infrastructure` | - | - | - |
| Incident | `Priority::High` | - | - | - |
| Vault Secret | `vault`, `secret`, `security` | - | - | - |

Title rules: **no emoji**, prefix in brackets, concise.

## Hierarchy (GitLab Free)

No native sub-tasks. Hierarchy is managed by:

1. **`relates_to` links** between issues via API
2. **References in the parent description** (dedicated section)

```text
Epic
 └── Feature (relates_to link + reference in ## Features)
      └── Story (relates_to link + reference in ## User Stories)
```

## Understand before acting

Before creating, modifying, or closing an issue, always read the existing context:

- Read the full issue (description + all comments) before editing or closing
- For bugs or tech debt: check linked issues and related MRs to understand prior
  decisions and constraints that led to the current implementation
- If a fix seems obvious but the code looks intentionally written that way, search
  for the original issue or MR that introduced it before overriding
- Never close an issue as "duplicate" or "won't fix" without reading its full history

When investigating a bug or refactoring request, search for related issues:

```bash
glab -R "$PROJECT" issue list --search "keyword"
glab -R "$PROJECT" issue view {iid} --output json | jq -r '.description'
```

## Behavioral rules

### After creating an issue

1. **Always link** it to its parent (Story → Feature, Feature → Epic) via API
2. **Always update** the parent description to add the reference
3. Format in the parent description: `- #N - short description`

### After closing an issue

1. Add a **closing comment** explaining what was delivered (files, decisions)
2. If it's the last Story of a Feature: check whether the Feature can be closed

### Descriptions

Always use a HEREDOC for multi-line descriptions:

```bash
--description "$(cat <<'EOF'
content here
EOF
)"
```

Templates are in `.gitlab/issue_templates/`.
See [templates.md](templates.md) for structure per issue type.

### Security

- NEVER put tokens in plain text (glab uses its local config)
- NEVER put secrets in issue descriptions
- NEVER put PII in titles or descriptions

## Personas (user stories)

Two persona registries:

- **Infrastructure & governance (15 personas)**: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
- **Application-level (5 personas)**: `granit-dotnet/docs/guide/personas-applicatifs.md`

NEVER invent a new persona.

**Infra/governance personas:** SRE, Ingénieur DevOps, Développeur, Architecte, DBA,
RSSI, DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe,
Utilisateur, Professionnel de santé, Product Owner

**Application personas:** Visiteur, Utilisateur authentifié, Administrateur d'application,
Approbateur, Gestionnaire de contenu

NEVER use hybrid roles ("slash roles" like `SRE / DevOps`).
Context (on-call, audit) goes in the story body, not in the persona.

## Resources

- Detailed workflows (create, link, close): [workflows.md](workflows.md)
- Description templates per type: [templates.md](templates.md)
- glab command reference: [reference.md](reference.md)
- Persona registry: `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
