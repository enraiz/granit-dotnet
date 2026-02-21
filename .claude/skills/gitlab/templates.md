# Templates de description

## Templates existants

Les templates GitLab sont dans `.gitlab/issue_templates/`.
Utiliser leur structure comme base pour les descriptions passees via `glab`.

| Template | Fichier | Usage |
| -------- | ------- | ----- |
| Story | `Story.md` | User stories avec format Given/When/Then |
| Feature | `Feature.md` | Features avec compliance checklist |
| Epic | `Epic.md` | Epics avec architecture et contraintes |
| Bug | `Bug.md` | Bugs avec severite et impact securite |
| Spike | `Spike.md` | Recherche technique avec timebox |
| Tech Debt | `Tech_Debt.md` | Dette technique avec impact et risques |
| Infrastructure | `Infrastructure.md` | Changements infra avec plan de deploiement et rollback |
| Incident | `Incident.md` | Incidents production avec timeline et RCA |
| Vault Secret | `VaultSecret.md` | Gestion secrets avec compliance HDS/RGPD |

## Utilisation avec glab

`glab issue create` ne supporte pas directement les templates GitLab.
Pour utiliser un template :

1. Lire le template avec `Read` pour connaitre la structure
2. Adapter le contenu pour le contexte specifique
3. Passer via HEREDOC :

```bash
glab -R "$PROJECT" issue create \
  --title "[TYPE] Titre" \
  --label "Type::XYZ" \
  --description "$(cat <<'EOF'
# Contenu adapte du template
EOF
)"
```

## Sections specifiques par type

### Story - Sections obligatoires

- `## User Story` (En tant que / Je souhaite / Afin de)
- `## Contexte`
- `## Criteres d'acceptation` (format Given/When/Then)
- `## Definition of Done`

### Feature - Sections obligatoires

- `## Description` (Probleme / Solution / Alternatives)
- `## User Stories` (placeholder pour liens)
- `## Livrables attendus`
- `## Compliance` (checklist Europe/chiffrement/audit)

### Epic - Sections obligatoires

- `## Objectif`
- `## Features` (placeholder pour liens)
- `## Contraintes` (HDS/RGPD/ISO)
- `## Criteres de succes`

### Bug - Sections obligatoires

- `## Description du bug`
- `## Etapes pour reproduire`
- `## Comportement attendu` / `## Comportement actuel`
- `## Environnement`
- `## Severite`
- `## Impact securite`

### Spike - Sections obligatoires

- `## Objectif`
- `## Questions a repondre`
- `## Timebox`
- `## Livrables attendus`
- `## Resultat` (a completer apres)

### Tech Debt - Sections obligatoires

- `## Description`
- `## Impact` (performance, maintenabilite, securite, compliance)
- `## Risques si non traite`
- `## Solution proposee`
- `## Fichiers / Modules concernes`
