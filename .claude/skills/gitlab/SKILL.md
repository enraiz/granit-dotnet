---
name: gitlab
description: >
  Operations GitLab (issues, liens, workflows). Utiliser pour creer, modifier,
  lier, lister des issues, commenter, et gerer la hierarchie Epic > Feature > Story.
  Invoquer avant toute operation GitLab.
argument-hint: "[operation] [args]"
allowed-tools: Bash(glab *)
---

# GitLab Operations

## Initialisation

Avant toute commande, detecter le projet courant :

```bash
PROJECT=$(git remote get-url origin | sed -E 's|ssh://git@[^/]+:[0-9]+/||;s|https?://[^/]+/||;s|git@[^:]+:||;s|\.git$||')
PROJECT_ENCODED=$(echo "$PROJECT" | sed 's|/|%2F|g')
```

Utiliser `$PROJECT` dans `glab -R "$PROJECT"` et `$PROJECT_ENCODED` dans `glab api`.

## Types d'issues

| Type | Label | Prefixe titre | Parent | Section dans parent |
| ---- | ----- | ------------- | ------ | ------------------- |
| Epic | `Type::Epic` | `[EPIC]` | - | - |
| Feature | `Type::Feature` | `[FEATURE]` | Epic | `## Features` |
| Story | `Type::Story` | `[STORY]` | Feature | `## User Stories` |
| Bug | `Type::Bug` | `[BUG]` | - | - |
| Spike | `Type::Spike` | `[SPIKE]` | - | - |
| Tech Debt | `Type::Tech Debt` | `[TECH DEBT]` | - | - |
| Infrastructure | `infrastructure` | - | - | - |
| Incident | `Priority::High` | - | - | - |
| Vault Secret | `vault`, `secret`, `security` | - | - | - |

Regles titres : **pas d'emoji**, prefixe entre crochets, concis.

## Hierarchie (GitLab Free)

Pas de sous-taches natives. La hierarchie est geree par :

1. **Liens `relates_to`** entre issues via l'API
2. **References dans la description** du parent (section dediee)

```text
Epic
 └── Feature (lien relates_to + reference dans ## Features)
      └── Story (lien relates_to + reference dans ## User Stories)
```

## Regles comportementales

### Apres creation d'une issue

1. **TOUJOURS lier** l'issue a son parent (Story -> Feature, Feature -> Epic) via l'API
2. **TOUJOURS mettre a jour** la description du parent pour ajouter la reference
3. Format dans la description du parent : `- #N - description courte`

### Apres fermeture d'une issue

1. Ajouter un **commentaire** expliquant ce qui a ete livre (fichiers, decisions)
2. Si c'est la derniere Story d'une Feature : verifier si la Feature peut etre fermee

### Descriptions

- Toujours utiliser un HEREDOC pour les descriptions multi-lignes :

```bash
--description "$(cat <<'EOF'
contenu ici
EOF
)"
```

- Les templates de description sont dans `.gitlab/issue_templates/`
  Utiliser leur structure comme base pour les descriptions passees a `glab`

### Securite

- JAMAIS de token en clair (glab utilise sa config locale)
- JAMAIS de secrets dans les descriptions d'issues
- JAMAIS de PII dans les titres ou descriptions

## Personas (user stories)

Lors de la création d'une Story, utiliser **exclusivement** un persona du référentiel
`governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`. Ne JAMAIS inventer un nouveau persona.

Personas autorisés : SRE, Ingénieur DevOps, Développeur, Architecte, DBA, RSSI,
DPO, CTO, Direction, Directeur juridique, Auditeur interne, Auditeur externe,
Utilisateur, Professionnel de santé, Product Owner.

Ne JAMAIS utiliser de rôles hybrides ("slash roles" comme `SRE / DevOps`).
Le contexte (astreinte, on-call, audit) se précise dans la story, pas dans le persona.

## Ressources

- Pour les workflows detailles (creation, liaison, fermeture), voir [workflows.md](workflows.md)
- Pour les templates de description par type, voir [templates.md](templates.md)
- Pour la reference des commandes glab, voir [reference.md](reference.md)
- Pour le référentiel des personas, voir `governance-compliance/docs/03-organization/ORG-05-PERSONAS.md`
