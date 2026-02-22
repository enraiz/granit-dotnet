# glab command reference

All commands use `$PROJECT` (project path) and `$PROJECT_ENCODED` (URL-encoded),
injected by the skill.

## Issues — Read

```bash
# List open issues
glab -R "$PROJECT" issue list

# Filter by label
glab -R "$PROJECT" issue list --label "Type::Story"

# Filter by multiple labels
glab -R "$PROJECT" issue list --label "Type::Story" --label "Area::Vault"

# Exclude a label
glab -R "$PROJECT" issue list --label "Type::Story" --not-label "Area::GitOps"

# Search by keyword
glab -R "$PROJECT" issue list --search "keyword"

# View an issue (text)
glab -R "$PROJECT" issue view {iid}

# View an issue (JSON for parsing)
glab -R "$PROJECT" issue view {iid} --output json

# Extract description
glab -R "$PROJECT" issue view {iid} --output json | jq -r '.description'

# List comments
glab -R "$PROJECT" issue view {iid} --comments
```

## Issues — Write

```bash
# Create an issue
glab -R "$PROJECT" issue create \
  --title "[TYPE] Titre" \
  --label "Type::XYZ" \
  --description "$(cat <<'EOF'
content
EOF
)"

# Update description
glab -R "$PROJECT" issue update {iid} --description "$(cat <<'EOF'
new description
EOF
)"

# Update title
glab -R "$PROJECT" issue update {iid} --title "[TYPE] New title"

# Add a label
glab -R "$PROJECT" issue update {iid} --label "Priority::High"

# Close an issue
glab -R "$PROJECT" issue close {iid}

# Reopen an issue
glab -R "$PROJECT" issue reopen {iid}

# Add a comment
glab -R "$PROJECT" issue note {iid} -m "comment"

# Multi-line comment
glab -R "$PROJECT" issue note {iid} -m "$(cat <<'EOF'
multi-line
comment
EOF
)"
```

## Issue links (API)

```bash
# Create a relates_to link
glab api --method POST "projects/$PROJECT_ENCODED/issues/{source_iid}/links" \
  -f target_project_id="$PROJECT_ENCODED" -f target_issue_iid={target_iid} \
  -f link_type=relates_to

# List links for an issue
glab api "projects/$PROJECT_ENCODED/issues/{iid}/links"

# Delete a link
glab api --method DELETE "projects/$PROJECT_ENCODED/issues/{iid}/links/{link_id}"
```

## Merge Requests

```bash
# List open MRs
glab -R "$PROJECT" mr list

# View an MR
glab -R "$PROJECT" mr view {iid}

# Create an MR
glab -R "$PROJECT" mr create \
  --title "feat: description" \
  --description "Closes #{issue_iid}" \
  --source-branch "feature/name" \
  --target-branch "main"
```

## Labels

```bash
# List labels
glab -R "$PROJECT" label list

# Create a label
glab -R "$PROJECT" label create "Name::Label" --color "#color" --description "description"
```
