# GitHub Workflows

Detailed workflows for issue management.
In all examples, `$REPO` is injected dynamically by SKILL.md.

## Create a Pull Request -- GitFlow rules

**Default target branch:**

| Source branch | Target | Exception |
| ------------- | ------ | --------- |
| `feature/*` | `develop` | Only if user explicitly says "target main" |
| `fix/*` | `develop` | Only if user explicitly says "target main" |
| `hotfix/*` | `main` **and** `develop` | Always both |
| `release/*` | `main` **and** `develop` | Always both |

**NEVER** create a PR targeting `main` for a `feature/*` or `fix/*` branch unless
the user explicitly requests it. If the target is ambiguous, ask before creating.

```bash
# Standard feature PR
gh pr create -R "$REPO" \
  --title "feat: ..." \
  --base develop \
  --head feature/my-feature
```

## Create a Story

1. Read any existing related issues or the parent Feature before creating, to avoid
   duplicates and align with decisions already made:

   ```bash
   gh issue list -R "$REPO" --label "Type: Story" --search "keyword"
   ```

2. Create the issue:

   ```bash
   gh issue create -R "$REPO" \
     --title "[STORY] Titre de la story" \
     --label "Type: Story" \
     --body "$(cat <<'EOF'
   ## User Story

   - **En tant que** [persona canonique],
   - **je souhaite** [action/fonctionnalite],
   - **afin de** [benefice/valeur].

   ## Contexte

   [Pourquoi cette story ? Quel probleme technique resout-elle ?]

   ## Implementation

   - Fichiers concernes :
   - Dependances :
   - Sequence de deploiement :

   ## Criteres d'acceptation

   ### Scenario : [description]

   - Given [contexte initial]
   - When [action]
   - Then [resultat attendu]

   ## Definition of Done

   - [ ] Code review approuvee (1 SRE minimum)
   - [ ] Tests unitaires >= 80 % de couverture
   - [ ] Documentation mise a jour
   - [ ] Aucun secret en clair
   EOF
   )"
   ```

3. Attach as sub-issue of the parent Feature **and** update the description:

   ```bash
   # Attach via sub-issues API
   STORY_ID=$(gh api repos/$REPO/issues/{story_number} --jq .id)
   gh api repos/$REPO/issues/{feature_number}/sub_issues \
     -X POST -F sub_issue_id="$STORY_ID"

   # Also update the Feature description for readability
   BODY=$(gh issue view {feature_number} -R "$REPO" --json body -q .body)
   gh issue edit {feature_number} -R "$REPO" --body "$(cat <<EOF
   $BODY

   - #{story_number} - short description
   EOF
   )"
   ```

## Create a Feature with its Stories

1. Read the parent Epic first to understand scope and constraints already defined.

2. Create the Feature:

   ```bash
   gh issue create -R "$REPO" \
     --title "[FEATURE] Titre de la feature" \
     --label "Type: Feature" \
     --body "$(cat <<'EOF'
   ## Description

   ### Probleme / Besoin

   [Quel probleme cette feature resout-elle ?]

   ### Solution proposee

   [Approche technique retenue]

   ### Alternatives considerees

   - **[Alternative]** : rejetee car [raison]

   ## User Stories

   <!-- Stories liees ci-dessous -->

   ## Configuration technique

   | Parametre | Staging | Production |
   | --------- | ------- | ---------- |
   |           |         |            |

   ## Livrables attendus

   - [ ] Code dans le bon package
   - [ ] Tests unitaires + integration
   - [ ] Documentation mise a jour
   - [ ] Aucun secret hardcode

   ## Compliance

   - [ ] Chiffrement au repos
   - [ ] Audit trail preserve
   EOF
   )"
   ```

3. Create each Story (see workflow above).
4. Attach the Feature as sub-issue of the Epic:

   ```bash
   FEATURE_ID=$(gh api repos/$REPO/issues/{feature_number} --jq .id)
   gh api repos/$REPO/issues/{epic_number}/sub_issues \
     -X POST -F sub_issue_id="$FEATURE_ID"
   ```

5. Update the Epic description (section `## Features`) with the Feature reference.

## Create an Epic

```bash
gh issue create -R "$REPO" \
  --title "[EPIC] Titre de l'epic" \
  --label "Type: Epic" \
  --body "$(cat <<'EOF'
## Objectif

[Objectif global et valeur metier]

## Features

<!-- Features liees ci-dessous -->

## Architecture

[Description de l'architecture cible et des composants concernes]

## Contraintes

- [ ] RGPD / ISO 27001 : [contraintes specifiques]

## Criteres de succes

- [ ] [critere 1]
- [ ] [critere 2]
EOF
)"
```

## Close an issue

Before closing, read the full issue history to ensure nothing was missed:

```bash
gh issue view {number} -R "$REPO" --json body -q .body
```

1. Add a closing comment:

   ```bash
   gh issue comment {number} -R "$REPO" --body "$(cat <<'EOF'
   ## Cloture

   **Livre** :
   - [fichiers crees / decisions prises / PR associee]

   **Verification** :
   - [tests effectues / validation]
   EOF
   )"
   ```

2. Close the issue:

   ```bash
   gh issue close {number} -R "$REPO"
   ```

3. If last Story of a Feature: check whether the Feature can be closed.
