# ADR-012 : PuppeteerSharp — Rendu HTML vers PDF

- **Statut** : Accepté
- **Date** : 2026-02-28
- **Issue** : [#324](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/issues/324)
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.DocumentGeneration.Pdf)

## Contexte

Le module `Granit.DocumentGeneration.Pdf` convertit du HTML (généré par Scriban)
en documents PDF. Les cas d'usage incluent : factures, rapports médicaux,
attestations, et documents réglementaires.

Les exigences sont :

- **Fidélité CSS** : rendu pixel-perfect du HTML/CSS (flexbox, grid, @media print)
- **PDF/A-3b** : conformité pour l'archivage long terme (ISO 27001) et Factur-X
- **Headers/footers** : en-têtes et pieds de page dynamiques (pagination, date)
- **Performance** : génération en < 2 secondes pour un document standard

## Décision

**PuppeteerSharp** (Chromium headless) pour la conversion HTML → PDF.

## Alternatives évaluées

### Option 1 : PuppeteerSharp (retenue)

- **Licence** : MIT
- **Avantage** : fidélité CSS parfaite (moteur Chromium Blink), support PDF/A-3b
  (via post-processing), headers/footers dynamiques, landscape/portrait,
  marges configurables, API async .NET native
- **Pipeline** : Scriban → HTML → PuppeteerSharp → PDF

### Option 2 : QuestPDF

- **Licence** : **Community License** (gratuit < $1M revenue, sinon commercial)
- **Avantage** : API C# fluent (code-first), pas de Chromium, léger
- **Inconvénient** : pas de rendu HTML (API C# uniquement — incompatible
  avec le pipeline Scriban → HTML), licence restrictive pour les entreprises,
  pas de PDF/A natif, courbe d'apprentissage pour les non-développeurs

### Option 3 : iText7

- **Licence** : **AGPL-3.0** (usage commercial nécessite une licence payante)
- **Avantage** : bibliothèque de référence pour la manipulation PDF, PDF/A natif,
  très mature
- **Inconvénient** : licence AGPL incompatible avec un framework distribué
  (obligation de publier le code source), licence commerciale coûteuse,
  rendu HTML limité (pdfHTML add-on payant)

### Option 4 : wkhtmltopdf

- **Avantage** : léger, rendu WebKit
- **Inconvénient** : **projet abandonné** (dernière release 2020), moteur WebKit
  obsolète (pas de flexbox, grid), problèmes de sécurité non corrigés,
  pas de support .NET natif (wrapper CLI)

### Option 5 : Playwright (via Microsoft.Playwright)

- **Licence** : Apache-2.0
- **Avantage** : moteur Chromium comme PuppeteerSharp, API moderne, maintenu
  par Microsoft
- **Inconvénient** : package beaucoup plus lourd (~200 Mo vs ~50 Mo — inclut
  Firefox et WebKit), orienté testing (pas de PDF generation API native,
  nécessite workarounds), overhead d'initialisation plus important

## Justification

| Critère | PuppeteerSharp | QuestPDF | iText7 | wkhtmltopdf | Playwright |
| ------- | -------------- | -------- | ------ | ----------- | ---------- |
| Licence | MIT | Freemium | AGPL/Commercial | MIT | Apache-2.0 |
| Rendu HTML | Chromium (parfait) | Non (C# only) | Limité (payant) | WebKit (obsolète) | Chromium |
| Pipeline Scriban→HTML | Oui | Non | Partiel | Oui | Oui |
| PDF/A-3b | Post-processing | Non | Natif | Non | Post-processing |
| Taille package | ~50 Mo | ~5 Mo | ~10 Mo | ~40 Mo | ~200 Mo |
| Maintenance | Active | Active | Active | Abandonné | Active (MS) |
| Performance | Bonne | Excellente | Bonne | Moyenne | Bonne |

## Conséquences

### Positives

- Fidélité CSS parfaite : le PDF est identique au rendu navigateur
- Pipeline unifié : Scriban (template) → HTML → PuppeteerSharp (PDF)
- Support PDF/A-3b via post-processing pour l'archivage ISO 27001 et Factur-X
- MIT : pas de contrainte de licence
- API async .NET native avec gestion du lifecycle Chromium

### Négatives

- Dépendance Chromium : ~50 Mo de binaire à télécharger au premier lancement
- Consommation mémoire : un processus Chromium par instance de rendu
  (pool de navigateurs recommandé)
- Temps de démarrage Chromium : ~1-2 secondes au premier appel (mitigé par
  le pool de navigateurs)
- Chromium nécessite des dépendances système en CI/production
  (libgbm, libatk, etc. — géré via l'image Docker)
