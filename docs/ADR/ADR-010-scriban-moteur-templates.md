# ADR-010 : Scriban — Moteur de templates texte

- **Statut** : Accepté
- **Date** : 2026-02-27
- **Auteurs** : Jean-François Meyers
- **Portée** : granit-dotnet (Granit.Templating.Scriban)

## Contexte

Le module `Granit.Templating` fournit un pipeline de génération documentaire :
template texte → rendu HTML → conversion vers le format final (PDF, Excel, etc.).

Le moteur de templates texte doit :

- **Sécurité** : exécuter les templates en sandbox (pas d'accès filesystem ou réseau)
- **Extensibilité** : fonctions custom, variables globales (date, utilisateur, tenant)
- **Performance** : compilation des templates, cache
- **Syntaxe** : intuitive pour les non-développeurs (opérations métier)

## Décision

**Scriban** comme moteur de templates texte pour la génération documentaire.

## Alternatives évaluées

### Option 1 : Scriban (retenue)

- **Licence** : BSD-2-Clause
- **Avantage** : sandboxed par défaut (pas d'accès système), syntaxe Liquid-like
  intuitive, extensible (fonctions custom, `GlobalContext`), compilation et cache
  des templates, performances excellentes (~10x plus rapide que Razor)
- **Taille** : léger (~200 Ko)

### Option 2 : Razor (RazorLight)

- **Avantage** : syntaxe C# familière pour les développeurs .NET, puissant
- **Inconvénient** : **pas sandboxed** (accès complet au runtime .NET —
  risque de sécurité si les templates sont éditables par les utilisateurs),
  dépendance au compilateur Roslyn (lourd, ~20 Mo), temps de compilation
  élevé, RazorLight est un wrapper tiers moins maintenu

### Option 3 : Fluid (Liquid .NET)

- **Licence** : MIT
- **Avantage** : implémentation .NET de Liquid (standard Shopify), sandboxed,
  syntaxe similaire à Scriban
- **Inconvénient** : moins performant que Scriban sur les benchmarks,
  extensibilité plus limitée (pas de `GlobalContext` natif), communauté
  .NET plus restreinte

### Option 4 : Handlebars.NET

- **Licence** : MIT
- **Avantage** : port .NET de Handlebars.js, logicless templates
- **Inconvénient** : « logicless » trop restrictif (pas de conditions complexes,
  pas de boucles avancées), extensibilité via helpers uniquement,
  performances inférieures à Scriban

### Option 5 : Mustache (Stubble)

- **Licence** : MIT
- **Avantage** : standard multi-langage, très simple
- **Inconvénient** : trop minimaliste (pas de filtres, pas de fonctions,
  pas d'expressions), inadapté pour la génération de documents complexes

## Justification

| Critère | Scriban | Razor | Fluid | Handlebars | Mustache |
| ------- | ------- | ----- | ----- | ---------- | -------- |
| Licence | BSD-2-Clause | MIT | MIT | MIT | MIT |
| Sandbox | Oui (natif) | Non | Oui | Partiel | Oui |
| Extensibilité | Excellente | Totale (C#) | Bonne | Limitée | Très limitée |
| Performance | Très rapide | Lente (compil) | Rapide | Moyenne | Rapide |
| Syntaxe intuitive | Oui (Liquid-like) | C# (dev only) | Oui | Oui | Oui |
| Taille | ~200 Ko | ~20 Mo (Roslyn) | ~150 Ko | ~100 Ko | ~50 Ko |
| GlobalContext | Oui | Non | Non | Non | Non |

## Conséquences

### Positives

- Templates exécutés en sandbox : pas de risque d'exécution de code arbitraire
- Syntaxe Liquid-like accessible aux utilisateurs métier
- GlobalContext pour les variables d'enrichissement (date, tenant, user)
- Cache et compilation des templates pour les performances
- Pipeline complet : Scriban → HTML → PDF (via PuppeteerSharp)

### Négatives

- Syntaxe Scriban spécifique (pas un standard comme Liquid ou Mustache)
- BSD-2-Clause (très permissive, mais moins courante que MIT/Apache-2.0)
- Projet maintenu par un développeur individuel (Alexandre Mutel — aussi
  auteur de markdig, SharpDX, etc.)
