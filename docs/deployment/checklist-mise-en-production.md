# Checklist de mise en production

Cette checklist couvre les vérifications obligatoires avant le déploiement
d'une application Granit en production ISO 27001.

## Sécurité

- [ ] Aucun secret en clair dans le code, les fichiers de configuration
  ou les variables d'environnement non chiffrées
- [ ] HashiCorp Vault configuré et accessible (authentification Kubernetes)
- [ ] Credentials PostgreSQL dynamiques via Vault (pas de mot de passe statique)
- [ ] HTTPS enforced (pas de HTTP en production)
- [ ] JWT Bearer configuré avec `RequireHttpsMetadata: true`
- [ ] Permissions RBAC définies et attribuées par tenant
- [ ] Pas d'endpoint de debug ou de diagnostic exposé publiquement

## Conformité ISO 27001

- [ ] Audit trail activé : `AuditedEntityInterceptor` enregistré
- [ ] `CreatedBy`, `ModifiedBy` remplis automatiquement via `ICurrentUserService`
- [ ] Rétention des logs configurée à **3 ans minimum** dans Loki
- [ ] Chiffrement at rest activé pour les données de santé (Vault Transit)
- [ ] Chiffrement in transit : TLS entre tous les composants
- [ ] Traçabilité des accès : chaque requête associée à un `UserId` et `TenantId`

## Conformité RGPD

- [ ] Soft delete activé pour les entités concernées (`FullAuditedEntity`)
- [ ] `SoftDeleteInterceptor` enregistré dans le `DbContext`
- [ ] Global query filter EF Core actif (entités supprimées masquées par défaut)
- [ ] Minimisation des données : pas de champ superflu dans les entités
- [ ] Pseudonymisation : données nominatives chiffrées via Transit Vault

## Base de données

- [ ] Migrations EF Core appliquées et testées
- [ ] Index créés sur les colonnes filtrées (`TenantId`, `IsDeleted`, `CreatedAt`)
- [ ] Connection pooling configuré (PgBouncer si multi-tenant)
- [ ] Backup automatique configuré et testé (restauration vérifiée)
- [ ] Credentials dynamiques Vault fonctionnels (lease renewal vérifié)

## Observabilité

- [ ] `Observability.OtlpEndpoint` configuré vers le collector OTLP
- [ ] `Observability.ServiceName` renseigné
- [ ] Dashboards Grafana provisionnés (HTTP, DB, cache, Wolverine)
- [ ] Alertes configurées (taux d'erreur, latence, Vault failures)
- [ ] Corrélation logs/traces Loki ↔ Tempo fonctionnelle

## Kubernetes

- [ ] Liveness probe configurée (`/health/live`)
- [ ] Readiness probe configurée (`/health/ready`)
- [ ] Startup probe configurée (`/health/startup`)
- [ ] Resource limits définis (CPU et mémoire)
- [ ] `terminationGracePeriodSeconds` ajusté (60s minimum)
- [ ] Rolling update : `maxUnavailable: 0`
- [ ] Secrets injectés via Vault Agent (pas de `Secret` K8s en clair)

## Cache

- [ ] Redis accessible et protégé par mot de passe (via Vault)
- [ ] `[CacheEncrypted]` appliqué sur les types contenant des données sensibles
- [ ] TTL configurés pour éviter la saturation mémoire
- [ ] Stampede protection activée (configuration par défaut de Granit)

## Messaging (Wolverine)

- [ ] Transport PostgreSQL configuré (`Granit.Wolverine.Postgresql`)
- [ ] Table outbox créée (`wolverine_outbox_*`)
- [ ] Context propagation vérifié (TenantId, UserId, traceparent)
- [ ] Dead Letter Queue surveillée (alerte si non vide)
- [ ] Graceful shutdown vérifié : handlers terminent avant SIGKILL

## Performance

- [ ] Tests de charge exécutés avec profil réaliste
- [ ] Baseline de performance établi (p50, p95, p99 latence)
- [ ] Pas de N+1 queries identifié dans les traces EF Core
- [ ] Cache hit ratio acceptable (> 80% pour les données fréquentes)

## Documentation

- [ ] Runbook opérationnel rédigé (procédures d'incident)
- [ ] Architecture de déploiement documentée (diagramme réseau)
- [ ] Contacts d'astreinte définis et testés

## Liens

- [Observabilité en production](observabilite-production.md)
- [Configuration Vault](configuration-vault.md)
- [Déploiement Kubernetes](kubernetes.md)
