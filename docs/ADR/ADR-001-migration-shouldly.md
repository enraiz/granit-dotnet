# ADR-001 : Migrer FluentAssertions vers Shouldly

- **Statut** : Accepté
- **Date** : 2026-02-28
- **Issue** : [#426](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/426)
- **Auteurs** : Équipe Digital Dynamics
- **Portée** : granit-dotnet, guava-backend

## Contexte

FluentAssertions est la bibliothèque d'assertions utilisée dans tous les projets de
test (`*.Tests`) de granit-dotnet et guava-backend.

À partir de la **version 7.x**, FluentAssertions a été acquis par **Xceed Software**
et a changé de licence : passage de **MIT** à la **Xceed Community License Agreement**.
Cette nouvelle licence **interdit l'usage commercial** sans achat d'une licence payante.

La plateforme Guava est un produit commercial (santé, certification HDS). L'utilisation
de FluentAssertions 8.x dans ce contexte constitue une **non-conformité de licence**.

## Décision

**Migrer vers Shouldly** comme bibliothèque d'assertions pour tous les projets de test.

## Alternatives évaluées

### Option 1 : Shouldly (retenue)

- **Licence** : Apache-2.0 (permissive, compatible usage commercial)
- **Maturité** : projet stable, maintenu activement, large communauté
- **Impact** : remplacement des assertions uniquement, le framework de test xUnit
  reste inchangé
- **API** : syntaxe concise et lisible (`actual.ShouldBe(expected)`)

### Option 2 : Rétrograder vers FluentAssertions 6.x (MIT)

- **Licence** : MIT (dernière version sous licence permissive)
- **Avantage** : aucune migration de code nécessaire
- **Inconvénient** : version en fin de vie, plus aucune mise à jour de sécurité
  ni correctif. Incompatible avec une stratégie de maintenance à long terme

### Option 3 : TUnit (remplacement complet xUnit + FluentAssertions)

- **Licence** : Apache-2.0
- **Avantage** : framework de test moderne avec assertions intégrées
  (`Assert.That(x).IsEqualTo(42)`), parallélisme natif via source generators
  (pas de réflexion au runtime), DI native dans les tests, lifecycle par attributs
  (`[Before]`, `[After]`) sans `IAsyncLifetime`
- **Performance** : significativement plus rapide que xUnit sur les grandes suites
  de tests grâce aux source generators et au parallélisme par défaut
- **Inconvénient** : implique le remplacement complet du framework de test
  (xUnit → TUnit), pas uniquement des assertions. Migration : attributs (`[Fact]` →
  `[Test]`, `[Theory]` → `[Test]` + `[Arguments]`), lifecycle, DI, runner CI

**Discussion (2026-02-28)** : le framework Granit n'étant pas encore en production
et le volume de tests étant faible, le coût de migration vers TUnit serait
actuellement minime. Cependant :

1. **xUnit v3 vient de sortir** avec des améliorations substantielles (parallélisme,
   `CancellationToken` natif, meilleure DI) qui réduisent l'écart de performance
2. **Risque asymétrique** : si TUnit stagne (projet jeune, mainteneur unique),
   le framework se retrouve sur un framework niche sans écosystème ni support
   communautaire large. L'écosystème .NET (Testcontainers, Verify, etc.) cible
   principalement xUnit/NUnit
3. **Séparation des préoccupations** : découpler le choix d'assertion (problème
   immédiat de licence) du choix de framework (décision architecturale distincte)
   permet de traiter l'urgence sans prendre de pari spéculatif

**Verdict** : TUnit reste une option à réévaluer quand le projet aura atteint une
maturité suffisante (v2+, adoption significative, documentation Testcontainers
officielle). Un ADR dédié pourra être ouvert à ce moment-là pour évaluer une
migration xUnit → TUnit sur base de données concrètes

### Option 4 : Acquérir une licence commerciale Xceed

- **Avantage** : aucune migration
- **Inconvénient** : coût récurrent, dépendance à un éditeur tiers pour une
  bibliothèque de test, risque de ré-augmentation tarifaire

## Justification

| Critère | Shouldly | FA 6.x | TUnit | Licence Xceed |
| ------- | -------- | ------ | ----- | ------------- |
| Licence permissive | Apache-2.0 | MIT (EOL) | Apache-2.0 | Payante |
| Effort de migration | Moyen | Nul | Très élevé | Nul |
| Pérennité | Maintenance active | Fin de vie | Récent | Dépendance vendor |
| Compatibilité xUnit | Totale | Totale | Incompatible | Totale |
| Conformité HDS/RGPD | Oui | Risque (EOL) | Oui | Oui |

Shouldly offre le meilleur rapport conformité / effort de migration / pérennité.

> **Note** : TUnit présente des avantages réels en performance et modernité, mais
> le risque lié à sa jeunesse (v1.x, écosystème restreint) ne justifie pas de
> coupler le problème de licence (urgent) à un changement de framework (stratégique).
> La migration vers TUnit pourra être réévaluée indépendamment via un ADR futur.

## Correspondance des assertions

| FluentAssertions | Shouldly |
| ---------------- | -------- |
| `x.Should().Be(42)` | `x.ShouldBe(42)` |
| `x.Should().NotBeNull()` | `x.ShouldNotBeNull()` |
| `x.Should().BeTrue()` | `x.ShouldBeTrue()` |
| `x.Should().BeFalse()` | `x.ShouldBeFalse()` |
| `x.Should().BeNull()` | `x.ShouldBeNull()` |
| `list.Should().HaveCount(3)` | `list.Count.ShouldBe(3)` |
| `list.Should().BeEmpty()` | `list.ShouldBeEmpty()` |
| `list.Should().Contain(item)` | `list.ShouldContain(item)` |
| `list.Should().BeEquivalentTo(other)` | `list.ShouldBe(other, ignoreOrder: true)` |
| `list.Should().BeInAscendingOrder()` | `list.ShouldBeInOrder(SortDirection.Ascending)` |
| `x.Should().BeGreaterThan(0)` | `x.ShouldBeGreaterThan(0)` |
| `act.Should().Throw<T>()` | `Should.Throw<T>(() => act())` |
| `await act.Should().ThrowAsync<T>()` | `await Should.ThrowAsync<T>(() => act())` |
| `.Because("raison")` | `customMessage: "raison"` |

## Conséquences

### Positives

- Conformité de licence restaurée (Apache-2.0)
- Élimination d'un risque d'audit ISO 27001 / HDS
- Bibliothèque activement maintenue

### Négatives

- Effort de migration ponctuel sur tous les projets `*.Tests`
- Formation de l'équipe à la syntaxe Shouldly (courbe d'apprentissage faible)
- Mise à jour de la documentation (`docs/testing/assertions.md`, `CLAUDE.md`)

## Plan d'exécution

1. Ajouter `Shouldly` dans `Directory.Packages.props` (granit-dotnet + guava-backend)
2. Remplacer les assertions dans chaque projet `*.Tests`
3. Supprimer `FluentAssertions` de `Directory.Packages.props`
4. Mettre à jour `THIRD-PARTY-NOTICES.md`
5. Mettre à jour `docs/testing/assertions.md`
6. Mettre à jour `CLAUDE.md` (section Tests)
7. Valider : `dotnet test` passe sans échec
