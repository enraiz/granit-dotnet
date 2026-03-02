# Definition of Done (DoD)

[← Conventions](index.md)

La Definition of Done est **bloquante**. Aucun push ni création de MR ne peut avoir
lieu tant que ces vérifications ne sont pas satisfaites.

## Vérifications obligatoires — backend (.NET)

1. **Tests unitaires** — chaque package modifié a son projet `*.Tests` mis à jour.
   `dotnet test` passe avec zéro échec.
2. **Documentation** — tout changement d'API publique, nouvelle fonctionnalité ou
   modification de comportement est reflété dans `docs/**/*.md` (français).
3. **Format** — `dotnet format --verify-no-changes` sort avec le code 0.
4. **Markdownlint** — chaque fichier `.md` modifié passe
   `npx markdownlint-cli2 "<fichier>"`.
5. **THIRD-PARTY-NOTICES.md** — toute dépendance ajoutée, supprimée ou mise à jour
   est reflétée dans `THIRD-PARTY-NOTICES.md` (voir [workflow](workflow.md)).

## Vérifications obligatoires — frontend (TypeScript / React)

1. **Tests** — `pnpm test` passe avec zéro échec.
2. **Lint** — `pnpm lint` (ESLint `--max-warnings 0`) passe sans erreur.
3. **TypeScript** — `pnpm exec tsc --noEmit` compile sans erreur.
4. **Prettier** — `npx prettier --check "src/**/*.{ts,tsx,css}"` passe.
5. **Markdownlint** — chaque fichier `.md` modifié passe
   `npx markdownlint-cli2 "<fichier>"`.
6. **Storybook** — chaque nouveau composant visible a sa story.
7. **THIRD-PARTY-NOTICES.md** — toute dépendance ajoutée, supprimée ou mise à jour
   est reflétée dans `THIRD-PARTY-NOTICES.md` (voir [workflow](workflow.md)).

## Règle de refus

Si un développeur (ou Claude) demande à pousser sans avoir satisfait la DoD,
**rappeler les vérifications manquantes et refuser** jusqu'à ce que la DoD soit
satisfaite ou que l'utilisateur explicitement déroge à chaque point.
