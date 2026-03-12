# ADR-011 : ClosedXML — Génération de tableurs Excel

- **Statut** : Accepté
- **Date** : 2026-02-27
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.DocumentGeneration.Excel)

## Contexte

Le module `Granit.DocumentGeneration.Excel` permet de générer des tableurs `.xlsx`
à partir de templates Excel. Les cas d'usage incluent : exports de données,
rapports financiers, tableaux de bord, et documents réglementaires.

La bibliothèque doit supporter :

- **Templates** : remplissage de cellules nommées dans un fichier `.xlsx` existant
- **Styling** : préservation des styles, formules et mise en page du template
- **Performance** : génération de fichiers volumineux (10 000+ lignes) en streaming
- **Licence** : compatible usage commercial sans coût récurrent

## Décision

**ClosedXML** pour la génération de fichiers Excel (.xlsx).

## Alternatives évaluées

### Option 1 : ClosedXML (retenue)

- **Licence** : MIT
- **Avantage** : API haut niveau intuitive, support des templates .xlsx,
  styles/formules/mise en page préservés, active maintenance, MIT
- **Maturité** : 10+ ans, large communauté

### Option 2 : EPPlus

- **Licence** : **Polyform Noncommercial License** (v5+) — usage commercial
  nécessite une **licence payante** (~$300/dev/an)
- **Avantage** : API très complète, excellentes performances, documentation
  exhaustive
- **Inconvénient** : changement de licence en 2020 (v5), coût récurrent
  incompatible avec la stratégie OSS du projet

### Option 3 : NPOI

- **Licence** : Apache-2.0
- **Avantage** : port .NET de Apache POI, support .xls et .xlsx
- **Inconvénient** : API bas niveau et verbeuse (proche de la POI Java),
  documentation .NET insuffisante, performances inférieures à ClosedXML
  et EPPlus, maintenance irrégulière

### Option 4 : Open XML SDK (Microsoft)

- **Licence** : MIT
- **Avantage** : SDK officiel Microsoft, accès complet au format OOXML
- **Inconvénient** : API **très bas niveau** (manipulation directe du XML OOXML),
  verbosité extrême pour des opérations simples (remplir une cellule = ~20 lignes),
  pas de support templates natif, pas de gestion de styles haut niveau

## Justification

| Critère | ClosedXML | EPPlus | NPOI | Open XML SDK |
| ------- | --------- | ------ | ---- | ------------ |
| Licence | MIT | Commercial (v5+) | Apache-2.0 | MIT |
| Coût | Gratuit | ~$300/dev/an | Gratuit | Gratuit |
| API haut niveau | Oui | Oui | Non | Non |
| Support templates | Oui | Oui | Partiel | Non |
| Documentation | Bonne | Excellente | Faible | Bonne (bas niveau) |
| Performance | Bonne | Excellente | Moyenne | Bonne |
| Maintenance | Active | Active | Irrégulière | Active (MS) |

## Conséquences

### Positives

- Licence MIT : pas de coût récurrent, compatible usage commercial
- API intuitive : `worksheet.Cell("A1").Value = "Hello"` vs ~20 lignes Open XML SDK
- Support templates : remplissage de cellules nommées dans un .xlsx existant
- Préservation des styles, formules et mise en page
- Large communauté et documentation

### Négatives

- Performances légèrement inférieures à EPPlus sur les fichiers très volumineux
- Certaines fonctionnalités avancées (charts, pivot tables) sont moins complètes
  que dans EPPlus
- Pas de support natif du streaming pour les très grands fichiers (charge en
  mémoire — workaround possible avec pagination)
