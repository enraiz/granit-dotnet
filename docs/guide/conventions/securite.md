# Sécurité dans le code

[← Conventions](index.md)

Ces règles de sécurité s'appliquent à **tous les projets** (backend et frontend).

## Règles absolues

### Secrets

- **Jamais** de secrets en dur — ni dans le code, ni en commentaire, ni dans les exemples
- `sensitive = true` sur toutes les variables secrètes
- Rotation obligatoire des secrets
- En Kubernetes : `ExternalSecret` + Vault (jamais de `Secret` en clair dans les manifests)

### Données personnelles (PII)

- **Pas de PII dans les logs** — noms, emails, numéros de sécurité sociale,
  données de santé
- Minimisation des données (RGPD) — ne collecter que ce qui est strictement nécessaire
- Pseudonymisation quand possible

### Chiffrement (ISO 27001)

- Chiffrement obligatoire **au repos et en transit** (exigence ISO 27001)
- Utilisez `ITransitEncryptionService` (Granit.Vault) pour le chiffrement des
  données sensibles côté backend
- TLS obligatoire pour toutes les communications

### Souveraineté (Cloud Act)

- Infrastructure **obligatoirement en Europe** sur infrastructure souveraine
- **Ne jamais proposer** AWS, Azure ou GCP pour les données de santé
- Images Docker depuis le registre privé souverain uniquement

## Signaux d'alerte à remonter

- Tokens codés en dur
- Fichier `.env` commité
- Données patient dans les logs
- Appels API non authentifiés
- Dépendance avec licence non-permissive (GPL, AGPL, SSPL)

## Voir aussi

- [Tutoriel sécurité](../demarrage-rapide/06-securite.md)
- [Référence authentification](../../framework/security/authentication.md)
