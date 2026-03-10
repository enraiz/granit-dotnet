# ADR-005 : AWSSDK.S3 — Stockage objet S3-compatible

- **Statut** : Accepté
- **Date** : 2026-02-24
- **Issue** : [#171](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/171)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.BlobStorage, Granit.BlobStorage.S3)

> Voir aussi : [IAC ADR-001 — Choix OVHcloud](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-001-choix-fournisseur-cloud-ovhcloud.md)
> (protocole S3 vers OVHcloud Object Storage — aucune donnée ne transite par AWS)

## Contexte

Le module `Granit.BlobStorage` fournit une abstraction pour le stockage de fichiers
(documents, images, exports) avec support multi-tenant et upload direct (presigned URLs).

Le choix du SDK client pour le stockage objet doit être compatible avec
OVHcloud Object Storage (API S3-compatible) tout en restant portable vers
d'autres fournisseurs S3-compatibles (MinIO, Scaleway, etc.).

**Note de souveraineté** : le SDK AWS est utilisé uniquement comme client du protocole S3.
Aucune donnée ne transite par l'infrastructure AWS. Le endpoint est configuré vers
OVHcloud (région GRA, Gravelines, France).

## Décision

**AWSSDK.S3** comme SDK client pour le protocole S3, ciblant OVHcloud Object Storage.

## Alternatives évaluées

### Option 1 : AWSSDK.S3 (retenue)

- **Licence** : Apache-2.0
- **Avantage** : SDK de référence du protocole S3, documentation exhaustive,
  support presigned URLs, multipart upload, retry policies, streaming
- **Compatibilité** : OVHcloud, MinIO, Scaleway, Ceph, tout provider S3-compatible
- **Maturité** : 10+ ans, maintenance Amazon

### Option 2 : MinIO .NET SDK

- **Licence** : Apache-2.0
- **Avantage** : léger, API simplifiée, orienté S3-compatible
- **Inconvénient** : API moins complète que AWSSDK.S3 (presigned URLs limitées,
  pas de TransferUtility), communauté .NET plus restreinte, documentation
  moins fournie

### Option 3 : OpenStack Swift SDK (.NET)

- **Avantage** : protocole natif OVHcloud (Swift est le backend d'Object Storage)
- **Inconvénient** : SDK .NET quasi inexistant (openstack.net abandonné),
  pas de presigned URLs standard, non portable vers d'autres providers,
  OVHcloud recommande l'API S3

### Option 4 : Client HTTP custom

- **Avantage** : zéro dépendance, contrôle total
- **Inconvénient** : réinvention de la roue (signature SigV4, multipart,
  retry, streaming), effort de maintenance considérable, risque de bugs
  de sécurité (signature cryptographique)

## Justification

| Critère | AWSSDK.S3 | MinIO SDK | Swift SDK | Custom |
| ------- | --------- | --------- | --------- | ------ |
| Licence | Apache-2.0 | Apache-2.0 | Abandonné | N/A |
| Presigned URLs | Complet | Partiel | Non | Manuel |
| Multipart upload | Oui (TransferUtility) | Oui | Non | Manuel |
| Portabilité S3 | Tout provider | Tout provider | OVHcloud seul | Manuel |
| Documentation | Exhaustive | Moyenne | Inexistante | N/A |
| Maturité | 10+ ans | 5+ ans | Abandonné | N/A |
| Taille du package | ~5 Mo | ~1 Mo | N/A | 0 |

## Conséquences

### Positives

- SDK de référence S3 : documentation, exemples, support communautaire
- Portabilité : changement de provider S3 par simple reconfiguration du endpoint
- Presigned URLs pour l'upload/download direct (pas de proxy applicatif)
- Streaming natif pour les fichiers volumineux

### Négatives

- Dépendance sur un SDK Amazon dans un contexte de souveraineté européenne
  (perception — le SDK est Apache-2.0 et ne communique pas avec AWS)
- Package relativement volumineux (~5 Mo) par rapport aux alternatives
- Certaines fonctionnalités AWS-spécifiques ne sont pas disponibles sur
  OVHcloud (S3 Select, Glacier, etc.)

## Conditions de réévaluation

Ce choix devrait être réévalué si :

- OVHcloud abandonne l'API S3-compatible au profit d'un autre protocole
- Le SDK AWSSDK.S3 change de licence ou devient incompatible avec le projet
- MinIO SDK atteint la parité fonctionnelle (presigned URLs, TransferUtility)

## Références

- Commit initial : `592956e` (2026-02-24)
- Issues : [#171](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/171), [#178](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/178)
- IAC ADR-001 : [Choix OVHcloud](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-001-choix-fournisseur-cloud-ovhcloud.md)
- AWSSDK.S3 : <https://github.com/aws/aws-sdk-net>
