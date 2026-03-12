# Security Policy

## Supported Versions

| Version | Supported |
| ------- | --------- |
| latest  | Yes       |

Only the latest released version receives security updates. We recommend
staying up to date.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public issues.**

Instead, send an email to **<jf.meyers@digitaldynamics.be>** with:

- A description of the vulnerability
- Steps to reproduce (proof of concept if possible)
- The affected package(s) and version(s)
- Any potential impact assessment

### What to expect

- **Acknowledgment** within 48 hours
- **Status update** within 7 days with an assessment and expected timeline
- **Fix or mitigation** as soon as reasonably possible, depending on severity

We follow responsible disclosure practices. We ask that you:

- Allow reasonable time to investigate and address the issue before public
  disclosure
- Avoid exploiting the vulnerability beyond what is necessary to demonstrate it
- Do not access or modify other users' data

## Security Design

Granit is designed with security in mind:

- **No plaintext secrets** — all secrets managed via Vault or environment variables
- **Encryption** — data encrypted at rest (AES-256) and in transit (TLS)
- **Audit trail** — all sensitive operations are logged with full traceability
- **GDPR compliance** — data minimization, right to erasure, pseudonymization
- **Dependency scanning** — automated vulnerability scanning in CI/CD

## Acknowledgments

We appreciate the security research community and will acknowledge reporters
(with their permission) once the vulnerability is resolved.
