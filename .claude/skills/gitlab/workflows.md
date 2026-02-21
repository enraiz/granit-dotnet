# Workflows GitLab

Workflows detailles pour la gestion des issues.
Dans tous les exemples, `$PROJECT` et `$PROJECT_ENCODED` sont les valeurs
injectees dynamiquement par le SKILL.md.

## Creer une Story

1. Creer l'issue avec label et prefixe :

   ```bash
   glab -R "$PROJECT" issue create \
     --title "[STORY] Titre de la story" \
     --label "Type::Story" \
     --description "$(cat <<'EOF'
   ## User Story

   - **En tant que** [SRE / DevOps / Developpeur],
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
   - [ ] `terraform validate` passe sans erreur
   - [ ] Documentation mise a jour
   - [ ] Aucun secret en clair
   EOF
   )"
   ```

2. Lier a la Feature parente :

   ```bash
   glab api --method POST "projects/$PROJECT_ENCODED/issues/{story_iid}/links" \
     -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={feature_iid} \
     -f link_type=relates_to
   ```

3. Mettre a jour la description de la Feature (section `## User Stories`) :

   ```bash
   # Lire la description actuelle
   CURRENT=$(glab -R "$PROJECT" issue view {feature_iid} --output json | jq -r '.description')
   # Ajouter la reference et mettre a jour
   glab -R "$PROJECT" issue update {feature_iid} --description "..."
   ```

   Ajouter la ligne : `- #{story_iid} - description courte`

## Creer une Feature avec ses Stories

1. Creer la Feature :

   ```bash
   glab -R "$PROJECT" issue create \
     --title "[FEATURE] Titre de la feature" \
     --label "Type::Feature" \
     --description "$(cat <<'EOF'
   ## Description

   ### Probleme / Besoin

   [Quel probleme cette feature resout-elle ?]

   ### Solution proposee

   [Approche technique retenue]

   ### Alternatives considerees

   - **[Alternative]** : rejete car [raison]

   ## User Stories

   <!-- Stories liees ci-dessous -->

   ## Configuration technique

   | Parametre | Staging | Production |
   | --------- | ------- | ---------- |
   |           |         |            |

   ## Livrables attendus

   - [ ] Code Terraform dans `modules/`
   - [ ] Variables typees avec description et validation
   - [ ] Outputs documentes
   - [ ] Documentation dans `docs/`
   - [ ] Aucun secret hardcode

   ## Compliance

   - [ ] Donnees restent en Europe (OVHcloud FR)
   - [ ] Chiffrement au repos
   - [ ] Audit trail preserve
   EOF
   )"
   ```

2. Creer chaque Story (voir workflow ci-dessus)
3. Lier la Feature a son Epic parent + mettre a jour la description de l'Epic (section `## Features`)

## Creer un Epic

```bash
glab -R "$PROJECT" issue create \
  --title "[EPIC] Titre de l'epic" \
  --label "Type::Epic" \
  --description "$(cat <<'EOF'
## Objectif

[Objectif global et valeur metier]

## Features

<!-- Features liees ci-dessous -->

## Architecture

[Description de l'architecture cible et des composants concernes]

## Contraintes

- [ ] HDS / RGPD / ISO 27001 / ISO 9001 : [contraintes specifiques]
- [ ] Souverainete : OVHcloud FR uniquement

## Criteres de succes

- [ ] [critere 1]
- [ ] [critere 2]
EOF
)"
```

## Lier deux issues

```bash
glab api --method POST "projects/$PROJECT_ENCODED/issues/{source_iid}/links" \
  -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={target_iid} \
  -f link_type=relates_to
```

## Lier plusieurs Stories a une Feature (batch)

```bash
FEATURE_IID=123
for STORY_IID in 124 125 126; do
  glab api --method POST "projects/$PROJECT_ENCODED/issues/$STORY_IID/links" \
    -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid=$FEATURE_IID \
    -f link_type=relates_to
done
```

## Fermer une issue

1. Ajouter un commentaire de cloture :

   ```bash
   glab -R "$PROJECT" issue note {iid} -m "$(cat <<'EOF'
   ## Cloture

   **Livre** :
   - [fichiers crees / decisions prises / MR associee]

   **Verification** :
   - [tests effectues / validation]
   EOF
   )"
   ```

2. Fermer l'issue :

   ```bash
   glab -R "$PROJECT" issue close {iid}
   ```

3. Si derniere Story d'une Feature : verifier si la Feature peut etre fermee
