# ADR-006 : Stack de test — xUnit v3, NSubstitute et Bogus

- **Statut** : Accepté
- **Date** : 2026-02-21
- **Issue** : [#4](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/4)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet, guava-backend

## Contexte

Le framework Granit applique le principe « tests are part of the DoD » : chaque
package possède un projet `*.Tests` et aucun code ne peut être livré sans couverture
de test. Le choix de la stack de test est donc structurant pour toute la plateforme.

Les besoins sont :

- **Framework de test** : parallélisme, CancellationToken natif, DI dans les tests,
  support xUnit.v3 pour les nouvelles API
- **Mocking** : substitution des dépendances (services, repositories, clients HTTP)
  avec une API claire et sans problème de licence
- **Données de test** : génération de données réalistes et localisées (FR)
- **Couverture** : collecte du code coverage pour la CI (Cobertura/OpenCover)
- **CI** : export des résultats au format JUnit XML pour GitLab

## Décision

| Rôle | Bibliothèque | Licence |
| ---- | ------------ | ------- |
| Framework de test | xUnit v3 | Apache-2.0 |
| Mocking | NSubstitute | BSD-3-Clause |
| Données de test | Bogus | MIT |
| Couverture | coverlet.collector | MIT |
| Rapport CI | JunitXml.TestLogger | MIT |

## Alternatives évaluées

### Framework de test

#### xUnit v3 (retenu)

- CancellationToken natif (`TestContext.Current.CancellationToken`)
- Parallélisme par défaut (test collections)
- Adoption majoritaire dans l'écosystème .NET open source
- Support natif de `IAsyncLifetime` pour le setup/teardown async

#### NUnit

- Framework mature, riche en attributs (`[TestCase]`, `[SetUp]`, `[TearDown]`)
- Inconvénient : parallélisme moins naturel, communauté .NET moderne plus orientée
  xUnit, pas de CancellationToken natif dans les tests

#### MSTest

- Framework Microsoft officiel
- Inconvénient : fonctionnalités limitées par rapport à xUnit/NUnit, adoption
  faible dans l'open source .NET, API moins expressive

#### TUnit

- Framework récent basé sur les source generators (pas de réflexion au runtime)
- Inconvénient : projet jeune (v1.x), mainteneur unique, écosystème restreint
  (Testcontainers, Verify ciblent principalement xUnit/NUnit)
- Réévaluation prévue via un ADR futur quand le projet aura atteint une maturité
  suffisante (cf. [ADR-018](ADR-018-migration-shouldly.md))

### Mocking

#### NSubstitute (retenu)

- API claire et lisible (`service.Method().Returns(value)`)
- BSD-3-Clause — aucun problème de licence
- Pas de syntaxe `Setup`/`Verify` verbeuse

#### Moq

- Bibliothèque historique la plus populaire
- **Problème de licence** : SponsorLink (v4.20+) a injecté du code de télémétrie
  dans les builds, créant un risque de conformité et une crise de confiance
  communautaire. Incompatible avec la politique de sécurité HDS/RGPD

#### FakeItEasy

- API fluent agréable (`A.CallTo(() => …).Returns(…)`)
- Inconvénient : syntaxe plus verbeuse que NSubstitute, communauté plus restreinte

### Données de test

#### Bogus (retenu)

- Génération de données réalistes avec locales (fr, fr_BE, en, etc.)
- API fluent (`new Faker<T>().RuleFor(…)`)
- Support des types complexes et des règles métier

#### AutoFixture

- Génération automatique sans configuration
- Inconvénient : données peu réalistes (strings aléatoires), moins de contrôle
  sur les valeurs métier, syntaxe moins intuitive

#### Faker.NET

- Port de Faker.js
- Inconvénient : API moins riche que Bogus, moins maintenu

## Justification

### Framework de test

| Critère | xUnit v3 | NUnit | MSTest | TUnit |
| ------- | -------- | ----- | ------ | ----- |
| CancellationToken natif | Oui | Non | Non | Oui |
| Parallélisme par défaut | Oui | Partiel | Partiel | Oui |
| Adoption .NET OSS | Dominante | Forte | Faible | Naissante |
| Maturité | 15+ ans | 20+ ans | 20+ ans | < 2 ans |
| Licence | Apache-2.0 | MIT | MIT | Apache-2.0 |

### Mocking

| Critère | NSubstitute | Moq | FakeItEasy |
| ------- | ----------- | --- | ---------- |
| Licence | BSD-3-Clause | MIT (+ SponsorLink) | Apache-2.0 |
| Risque SponsorLink | Non | Oui | Non |
| Concision API | Excellente | Bonne | Moyenne |
| Communauté | Large | Très large | Moyenne |

### Données de test

| Critère | Bogus | AutoFixture | Faker.NET |
| ------- | ----- | ----------- | --------- |
| Locales FR | Oui (fr, fr_BE) | Non | Partiel |
| Données réalistes | Oui | Non (aléatoire) | Oui |
| API fluent | Oui | Partiel | Non |
| Maintenance active | Oui | Oui | Faible |

## Conséquences

### Positives

- Stack cohérente et moderne, adoptée par la majorité de l'écosystème .NET
- Zéro risque de licence (pas de SponsorLink, pas de licence commerciale)
- CancellationToken natif xUnit v3 : tests interruptibles, CI plus rapide
- Données de test réalistes et localisées (noms français, SIRET, etc.)
- Coverage Cobertura pour SonarQube et GitLab CI

### Négatives

- Migration depuis un autre framework de test serait coûteuse si nécessaire
- xUnit v3 est récent : certains outils tiers peuvent avoir un retard de support
- Bogus génère des données pseudo-aléatoires (non déterministes par défaut —
  utiliser `Seed` pour la reproductibilité)
