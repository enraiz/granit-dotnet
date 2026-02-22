# GitLab Workflows

Detailed workflows for issue management.
In all examples, `$PROJECT` and `$PROJECT_ENCODED` are injected dynamically by SKILL.md.

## Create a Story

1. Read any existing related issues or the parent Feature before creating, to avoid
   duplicates and align with decisions already made:

   ```bash
   glab -R "$PROJECT" issue list --label "Type::Story" --search "keyword"
   ```

2. Create the issue:

   ```bash
   glab -R "$PROJECT" issue create \
     --title "[STORY] Titre de la story" \
     --label "Type::Story" \
     --description "$(cat <<'EOF'
   ## User Story

   - **En tant que** [SRE / DevOps / Développeur],
   - **je souhaite** [action/fonctionnalité],
   - **afin de** [bénéfice/valeur].

   ## Contexte

   [Pourquoi cette story ? Quel problème technique résout-elle ?]

   ## Implémentation

   - Fichiers concernés :
   - Dépendances :
   - Séquence de déploiement :

   ## Critères d'acceptation

   ### Scénario : [description]

   - Given [contexte initial]
   - When [action]
   - Then [résultat attendu]

   ## Definition of Done

   - [ ] Code review approuvée (1 SRE minimum)
   - [ ] Tests unitaires ≥ 80 % de couverture
   - [ ] Documentation mise à jour
   - [ ] Aucun secret en clair
   EOF
   )"
   ```

3. Link to the parent Feature:

   ```bash
   glab api --method POST "projects/$PROJECT_ENCODED/issues/{story_iid}/links" \
     -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={feature_iid} \
     -f link_type=relates_to
   ```

4. Update the Feature description (section `## User Stories`):

   ```bash
   CURRENT=$(glab -R "$PROJECT" issue view {feature_iid} --output json | jq -r '.description')
   glab -R "$PROJECT" issue update {feature_iid} --description "..."
   ```

   Add the line: `- #{story_iid} - short description`

## Create a Feature with its Stories

1. Read the parent Epic first to understand scope and constraints already defined.

2. Create the Feature:

   ```bash
   glab -R "$PROJECT" issue create \
     --title "[FEATURE] Titre de la feature" \
     --label "Type::Feature" \
     --description "$(cat <<'EOF'
   ## Description

   ### Problème / Besoin

   [Quel problème cette feature résout-elle ?]

   ### Solution proposée

   [Approche technique retenue]

   ### Alternatives considérées

   - **[Alternative]** : rejetée car [raison]

   ## User Stories

   <!-- Stories liées ci-dessous -->

   ## Configuration technique

   | Paramètre | Staging | Production |
   | --------- | ------- | ---------- |
   |           |         |            |

   ## Livrables attendus

   - [ ] Code dans le bon package
   - [ ] Tests unitaires + intégration
   - [ ] Documentation mise à jour
   - [ ] Aucun secret hardcodé

   ## Compliance

   - [ ] Données restent en Europe (OVHcloud FR)
   - [ ] Chiffrement au repos
   - [ ] Audit trail préservé
   EOF
   )"
   ```

3. Create each Story (see workflow above).
4. Link the Feature to its parent Epic + update the Epic description (section `## Features`).

## Create an Epic

```bash
glab -R "$PROJECT" issue create \
  --title "[EPIC] Titre de l'epic" \
  --label "Type::Epic" \
  --description "$(cat <<'EOF'
## Objectif

[Objectif global et valeur métier]

## Features

<!-- Features liées ci-dessous -->

## Architecture

[Description de l'architecture cible et des composants concernés]

## Contraintes

- [ ] HDS / RGPD / ISO 27001 / ISO 9001 : [contraintes spécifiques]
- [ ] Souveraineté : OVHcloud FR uniquement

## Critères de succès

- [ ] [critère 1]
- [ ] [critère 2]
EOF
)"
```

## Link two issues

```bash
glab api --method POST "projects/$PROJECT_ENCODED/issues/{source_iid}/links" \
  -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={target_iid} \
  -f link_type=relates_to
```

## Link multiple Stories to a Feature (batch)

```bash
FEATURE_IID=123
for STORY_IID in 124 125 126; do
  glab api --method POST "projects/$PROJECT_ENCODED/issues/$STORY_IID/links" \
    -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid=$FEATURE_IID \
    -f link_type=relates_to
done
```

## Close an issue

Before closing, read the full issue history to ensure nothing was missed:

```bash
glab -R "$PROJECT" issue view {iid} --output json | jq -r '.description'
```

1. Add a closing comment:

   ```bash
   glab -R "$PROJECT" issue note {iid} -m "$(cat <<'EOF'
   ## Clôture

   **Livré** :
   - [fichiers créés / décisions prises / MR associée]

   **Vérification** :
   - [tests effectués / validation]
   EOF
   )"
   ```

2. Close the issue:

   ```bash
   glab -R "$PROJECT" issue close {iid}
   ```

3. If last Story of a Feature: check whether the Feature can be closed.
