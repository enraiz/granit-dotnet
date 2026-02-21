# Reference des commandes glab

Toutes les commandes utilisent `$PROJECT` (chemin du projet) et
`$PROJECT_ENCODED` (URL-encoded) injectes par le skill.

## Issues - Lecture

```bash
# Lister les issues ouvertes
glab -R "$PROJECT" issue list

# Filtrer par label
glab -R "$PROJECT" issue list --label "Type::Story"

# Filtrer par plusieurs labels
glab -R "$PROJECT" issue list --label "Type::Story" --label "Area::Vault"

# Exclure un label
glab -R "$PROJECT" issue list --label "Type::Story" --not-label "Area::GitOps"

# Voir une issue (texte)
glab -R "$PROJECT" issue view {iid}

# Voir une issue (JSON pour parsing)
glab -R "$PROJECT" issue view {iid} --output json

# Extraire la description
glab -R "$PROJECT" issue view {iid} --output json | jq -r '.description'
```

## Issues - Ecriture

```bash
# Creer une issue
glab -R "$PROJECT" issue create \
  --title "[TYPE] Titre" \
  --label "Type::XYZ" \
  --description "$(cat <<'EOF'
contenu
EOF
)"

# Modifier la description
glab -R "$PROJECT" issue update {iid} --description "$(cat <<'EOF'
nouvelle description
EOF
)"

# Modifier le titre
glab -R "$PROJECT" issue update {iid} --title "[TYPE] Nouveau titre"

# Ajouter un label
glab -R "$PROJECT" issue update {iid} --label "Priority::High"

# Fermer une issue
glab -R "$PROJECT" issue close {iid}

# Rouvrir une issue
glab -R "$PROJECT" issue reopen {iid}

# Ajouter un commentaire
glab -R "$PROJECT" issue note {iid} -m "commentaire"

# Commentaire multi-lignes
glab -R "$PROJECT" issue note {iid} -m "$(cat <<'EOF'
commentaire
multi-lignes
EOF
)"
```

## Liens entre issues (API)

```bash
# Creer un lien relates_to
glab api --method POST "projects/$PROJECT_ENCODED/issues/{source_iid}/links" \
  -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={target_iid} \
  -f link_type=relates_to

# Lister les liens d'une issue
glab api "projects/$PROJECT_ENCODED/issues/{iid}/links"

# Supprimer un lien
glab api --method DELETE "projects/$PROJECT_ENCODED/issues/{iid}/links/{link_id}"
```

## Merge Requests

```bash
# Lister les MR ouvertes
glab -R "$PROJECT" mr list

# Voir une MR
glab -R "$PROJECT" mr view {iid}

# Creer une MR
glab -R "$PROJECT" mr create \
  --title "feat: description" \
  --description "Closes #{issue_iid}" \
  --source-branch "feature/nom" \
  --target-branch "main"
```

## Labels

```bash
# Lister les labels
glab -R "$PROJECT" label list

# Creer un label
glab -R "$PROJECT" label create "Nom::Label" --color "#color" --description "description"
```
