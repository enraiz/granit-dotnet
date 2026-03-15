# gh command reference

All commands use `$REPO` (owner/repo), injected by the skill.

## Issues -- Read

```bash
# List open issues
gh issue list -R "$REPO"

# Filter by label
gh issue list -R "$REPO" --label "Type: Story"

# Filter by multiple labels
gh issue list -R "$REPO" --label "Type: Story" --label "Area: Vault"

# Search by keyword
gh issue list -R "$REPO" --search "keyword"

# View an issue (text)
gh issue view {number} -R "$REPO"

# View an issue (JSON for parsing)
gh issue view {number} -R "$REPO" --json title,body,labels,state

# Extract description
gh issue view {number} -R "$REPO" --json body -q .body

# List comments
gh issue view {number} -R "$REPO" --comments
```

## Issues -- Write

```bash
# Create an issue
gh issue create -R "$REPO" \
  --title "[TYPE] Titre" \
  --label "Type: XYZ" \
  --body "$(cat <<'EOF'
content
EOF
)"

# Update description
gh issue edit {number} -R "$REPO" --body "$(cat <<'EOF'
new description
EOF
)"

# Update title
gh issue edit {number} -R "$REPO" --title "[TYPE] New title"

# Add a label
gh issue edit {number} -R "$REPO" --add-label "Priority: High"

# Close an issue
gh issue close {number} -R "$REPO"

# Reopen an issue
gh issue reopen {number} -R "$REPO"

# Add a comment
gh issue comment {number} -R "$REPO" --body "comment"

# Multi-line comment
gh issue comment {number} -R "$REPO" --body "$(cat <<'EOF'
multi-line
comment
EOF
)"
```

## Issue linking

GitHub does not have a native issue links API. Use description-based linking:

```bash
# Add a reference in the parent issue description
# Read current body, append the reference, update
BODY=$(gh issue view {parent_number} -R "$REPO" --json body -q .body)
gh issue edit {parent_number} -R "$REPO" --body "$(cat <<EOF
$BODY

- #{child_number} - short description
EOF
)"
```

## Pull Requests

```bash
# List open PRs
gh pr list -R "$REPO"

# View a PR
gh pr view {number} -R "$REPO"

# Create a PR
gh pr create -R "$REPO" \
  --title "feat: description" \
  --body "Closes #{issue_number}" \
  --base develop \
  --head "feature/name"
```

## Labels

```bash
# List labels
gh label list -R "$REPO"

# Create a label
gh label create "Name: Label" --color "color" --description "description" -R "$REPO"
```
