# ADR-016 : Transports de notifications — MailKit + Lib.Net.Http.WebPush

- **Statut** : Accepté
- **Date** : 2026-02-28
- **Issue** : [#375](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/375)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet (Granit.Notifications.Email.Smtp, Granit.Notifications.Push)

> Voir aussi : [IAC ADR-001 — Choix OVHcloud](https://gitlab.digitaldynamics.be/digital-dynamics/guava-platform/infrastructure/iac/-/blob/main/docs/ADR/ADR-001-choix-fournisseur-cloud-ovhcloud.md)
> (SMTP self-hosted sur OVHcloud — pas de SendGrid / Amazon SES)

## Contexte

Le module `Granit.Notifications` implémente un système de notifications multi-canal.
Deux canaux nécessitent des bibliothèques tierces :

- **Email (SMTP)** : envoi d'emails transactionnels (confirmations, alertes, rapports)
  via un serveur SMTP self-hosted sur OVHcloud
- **Web Push** : notifications push navigateur (RFC 8030, VAPID) pour les
  notifications temps réel sans application native

Les autres canaux (InApp, SignalR) utilisent des composants Microsoft natifs.

## Décision

- **MailKit** pour l'envoi SMTP
- **Lib.Net.Http.WebPush** pour les notifications Web Push

## Alternatives évaluées

### Email — SMTP

#### MailKit (retenu)

- **Licence** : MIT
- **Avantage** : recommandé par Microsoft comme remplacement de `System.Net.Mail`,
  support SMTP/IMAP/POP3, OAuth2, TLS/STARTTLS, DKIM, pool de connexions,
  API async, très mature (10+ ans)
- **Auteur** : Jeffrey Stedfast (ancien Xamarin/Microsoft)

#### System.Net.Mail (SmtpClient)

- **Avantage** : natif .NET, zéro dépendance
- **Inconvénient** : **déprécié** par Microsoft (CA2000, `SmtpClient` marqué obsolète),
  pas de support OAuth2, TLS limité, pas d'async natif, bugs connus non corrigés

#### FluentEmail

- **Licence** : MIT
- **Avantage** : API fluent pour la composition d'emails, support templates Razor
- **Inconvénient** : abstraction au-dessus de SmtpClient (mêmes limitations),
  maintenance irrégulière, dernier commit 2022

#### SendGrid SDK

- **Avantage** : API HTTP REST, dashboard, analytics
- **Inconvénient** : **incompatible souveraineté** — SaaS US (Twilio),
  données email transitent par l'infrastructure US, coût par email

### Web Push

#### Lib.Net.Http.WebPush (retenu)

- **Licence** : MIT
- **Avantage** : implémentation complète du protocole Web Push (RFC 8030),
  support VAPID (RFC 8292), API .NET moderne, HttpClient-based
- **Maturité** : seule implémentation .NET mature et maintenue

#### WebPush.Net

- **Licence** : MIT
- **Avantage** : API simple
- **Inconvénient** : projet moins maintenu (dernière release 2021),
  pas de support des dernières spécifications, API moins moderne

#### Implémentation custom

- **Avantage** : contrôle total
- **Inconvénient** : le protocole Web Push implique de la cryptographie
  (ECDH, HKDF, AES-128-GCM) — réimplémenter est risqué et coûteux

## Justification

### Email

| Critère | MailKit | System.Net.Mail | FluentEmail | SendGrid |
| ------- | ------- | --------------- | ----------- | -------- |
| Licence | MIT | Natif (.obsolète) | MIT | SaaS US |
| Souveraineté | SMTP self-hosted | SMTP self-hosted | SMTP self-hosted | Non (Twilio) |
| OAuth2 | Oui | Non | Non | N/A |
| TLS/STARTTLS | Complet | Limité | Via SmtpClient | TLS |
| Async natif | Oui | Non | Partiel | Oui |
| Maintenance | Active (MS recommandé) | Déprécié | Inactive (2022) | Active |

### Web Push

| Critère | Lib.Net.Http.WebPush | WebPush.Net | Custom |
| ------- | -------------------- | ----------- | ------ |
| Licence | MIT | MIT | N/A |
| RFC 8030 complet | Oui | Partiel | Manuel |
| VAPID (RFC 8292) | Oui | Oui | Manuel |
| Maintenance | Active | Inactive (2021) | N/A |
| Cryptographie | Correcte (ECDH) | Correcte | Risque |

## Conséquences

### Positives

- MailKit : recommandé par Microsoft, TLS/OAuth2 complet, SMTP self-hosted (souveraineté)
- Lib.Net.Http.WebPush : implémentation complète RFC 8030/8292, cryptographie correcte
- Pas de dépendance SaaS US pour l'envoi d'emails ou de notifications push

### Négatives

- MailKit est une dépendance de communication réseau (surface d'attaque — mitigé
  par le TLS et les mises à jour régulières)
- Lib.Net.Http.WebPush : communauté restreinte (bibliothèque de niche)
- SMTP self-hosted nécessite la gestion de la délivrabilité (SPF, DKIM, DMARC)
