# Validation — Granit.Validation

`Granit.Validation` fournit un système de validation d'entrées structuré, intégré à FluentValidation.
Il expose deux couches complémentaires :

- **`GranitValidator<T>`** — classe de base pour les validateurs applicatifs
- **`FluentValidationEndpointFilter<T>`** — filtre Minimal API qui valide le body avant le handler
- **Extensions FluentValidation** — règles métier prêtes à l'emploi pour les identifiants légaux
  belges et français, les numéros fiscaux européens, les instruments de paiement et les contacts

## Pourquoi une validation structurée ?

FluentValidation émet par défaut des messages en clair, résolus côté serveur dans la culture du
thread. Cette approche pose deux problèmes dans un contexte SPA multilingue :

1. Les messages sont figés à la compilation et ne tiennent pas compte de la langue du client
2. Le code de retour (`ErrorCode`) et le message divergent souvent en pratique

Granit adopte une convention « code = message » :

```json
ValidationProblemDetails.errors["Value"] = ["Granit:Validation:InvalidBelgianNiss"]
```

Le SPA reçoit le code, l'envoie à `GET /api/granit/localization` et affiche le libellé dans
la langue de l'utilisateur. Le serveur ne traduit jamais.

## Architecture

```text
Granit.Validation
├── GranitValidator.cs                            (classe de base AbstractValidator<T>)
├── RuleBuilderExtensions.cs                      (WithErrorCodeAndMessage)
├── PersonalIdentifierValidatorExtensions.cs      (BelgianNiss, FrenchNir, BelgianEid)
├── ProfessionalRegistryValidatorExtensions.cs    (FrenchRpps, FrenchAdeli, FrenchFiness, BelgianInami)
├── CompanyIdentifierValidatorExtensions.cs       (FrenchSiren, FrenchSiret, BelgianBce, FrenchNafCode)
├── TaxIdentifierValidatorExtensions.cs           (BelgianVat, FrenchVat, EuropeanVat)
├── PaymentValidatorExtensions.cs                 (Iban, BicSwift, SepaCreditorIdentifier, FrenchRib, BelgianAccountNumber)
├── ContactValidatorExtensions.cs                 (Email, E164Phone)
├── AddressValidatorExtensions.cs                 (FrenchPostalCode, BelgianPostalCode, FrenchInseeCode)
├── LocaleValidatorExtensions.cs                  (Iso3166Alpha2CountryCode, Bcp47LanguageTag)
├── AspNetCore/
│   ├── FluentValidationEndpointFilter.cs        (IEndpointFilter → 422)
│   └── ValidationEndpointConventionBuilderExtensions.cs (.ValidateBody<T>())
├── Extensions/
│   └── ValidationServiceCollectionExtensions.cs (AddGranitValidation)
├── Internal/                                     (algorithmes — non exposés publiquement)
│   ├── Fondamentaux
│   │   ├── LuhnAlgorithm.cs                      (ISO/IEC 7812-1)
│   │   └── Mod97Algorithm.cs                     (ISO 7064 MOD 97-10)
│   ├── Identifiants personnels
│   │   ├── NissAlgorithm.cs                      (NISS belge)
│   │   ├── NirAlgorithm.cs                       (NIR français)
│   │   └── EidAlgorithm.cs                       (eID belge)
│   ├── Registres professionnels
│   │   ├── RppsAlgorithm.cs                      (RPPS)
│   │   ├── AdeliAlgorithm.cs                     (ADELI)
│   │   ├── FinesAlgorithm.cs                     (FINESS)
│   │   └── InamiAlgorithm.cs                     (INAMI/RIZIV)
│   ├── Entreprises
│   │   ├── SirenAlgorithm.cs
│   │   ├── SiretAlgorithm.cs
│   │   └── BceAlgorithm.cs
│   ├── Fiscalité
│   │   ├── FrenchVatAlgorithm.cs
│   │   └── EuropeanVatAlgorithm.cs               (27 pays UE)
│   ├── Paiement
│   │   ├── IbanAlgorithm.cs
│   │   ├── BicSwiftAlgorithm.cs
│   │   ├── SepaCreditorIdentifierAlgorithm.cs
│   │   ├── FrenchRibAlgorithm.cs               (clé RIB — pondération 89/15/3)
│   │   └── BelgianAccountNumberAlgorithm.cs    (mod 97 sur 10 chiffres de base)
│   └── Entreprises
│       └── NafAlgorithm.cs                     (regex format NNNNL)
└── Localization/Validation/
    ├── en.json
    └── fr.json
```

## Installation

### Module

```csharp
[DependsOn(typeof(GranitValidationModule))]
public sealed class MyAppModule : GranitModule { }
```

`GranitValidationModule` dépend de `GranitExceptionHandlingModule` et `GranitLocalizationModule`.
Il enregistre automatiquement :

| Service | Rôle |
| --- | --- |
| `GranitErrorCodeLanguageManager` | Remplace le `LanguageManager` FluentValidation — tous les codes intégrés suivent la convention `Granit:Validation:*` |
| `FluentValidationExceptionStatusCodeMapper` | Mappe `ValidationException` → HTTP 422 Unprocessable Entity dans `GranitExceptionHandler` |
| Ressources de localisation | Clés `Granit:Validation:*` en français et anglais |

> Le `FluentValidationEndpointFilter<T>` n'est pas un service DI — il est ajouté via
> `.ValidateBody<T>()` directement sur le `RouteHandlerBuilder` de chaque endpoint.

### Enregistrement manuel (sans module)

```csharp
builder.Services.AddGranitValidation();
```

## Créer un validateur

Héritez de `GranitValidator<T>` plutôt que d'`AbstractValidator<T>` :

```csharp
public sealed class CreatePatientCommandValidator : GranitValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.Niss).BelgianNiss();
        RuleFor(x => x.Nir).FrenchNir();
        RuleFor(x => x.Phone).E164Phone();
    }
}
```

`GranitValidator<T>` force `CascadeMode.Continue` au niveau classe : **toutes** les règles sont
évaluées et toutes les erreurs sont renvoyées dans la même réponse — pas d'effet « une erreur
à la fois » dans les formulaires SPA.

### Règles personnalisées

Utilisez `WithErrorCodeAndMessage` pour que le code et le message restent synchronisés :

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .WithErrorCodeAndMessage("MyApp:Validation:InvalidEmail");
```

> **Convention** : ne jamais appeler `ValidateAndThrow()` dans un handler Wolverine.
> Le middleware FluentValidation est le seul point d'entrée de validation du bus.

## Validation des endpoints Minimal API

`FluentValidationEndpointFilter<T>` valide le body de la requête avant l'exécution du handler.
Il se branche via l'extension `.ValidateBody<T>()` sur `RouteHandlerBuilder` :

```csharp
group.MapPost("/", HandleCreate)
    .WithName("CreateResource")
    .WithSummary("Creates a resource.")
    .ValidateBody<CreateResourceRequest>();
```

### Comportement

| `IValidator<T>` en DI ? | Requête valide | Requête invalide |
| --- | --- | --- |
| Oui (Wolverine auto-discovery ou enregistrement manuel) | Passe au handler | **422** `HttpValidationProblemDetails` avec codes `Granit:Validation:*` |
| Non | Passe au handler | Passe au handler (filet de sécurité : la validation manuelle dans le handler s'applique) |

Le filtre résout `IValidator<T>` depuis le DI. Si aucun validateur n'est enregistré, il passe
silencieusement au handler suivant — cela permet une adoption progressive.

### Enregistrement des validateurs en DI

Les validateurs sont auto-découverts par Wolverine via
`UseFluentValidation(RegistrationBehavior.DiscoverAndRegisterValidators)`. Si Wolverine n'est pas
utilisé, enregistrez-les manuellement :

```csharp
builder.Services.AddScoped<IValidator<CreateResourceRequest>, CreateResourceRequestValidator>();
```

### Deux couches de validation

La validation du body et la validation manuelle dans le handler coexistent :

1. **Filtre `ValidateBody<T>`** — validation structurelle (NotEmpty, MaxLength, format).
   Renvoie **422** avec codes structurés si `IValidator<T>` est en DI.
2. **Validation manuelle dans le handler** — validation métier (existence d'une entité référencée,
   droits, état). Renvoie **400** avec `ProblemDetails` textuel.

Le filtre intercepte avant le handler. Les deux couches sont complémentaires : la première
fournit une réponse structurée pour les formulaires SPA, la seconde couvre les règles métier
qui dépendent du contexte d'exécution (base de données, services externes).

### Validateurs intégrés dans les packages Granit

| Package | Validateur | Règles |
| --- | --- | --- |
| `Granit.Localization.Endpoints` | `SetLocalizationOverrideRequestValidator` | `Value` NotEmpty, MaxLength(4000) |
| `Granit.Querying.Endpoints` | `CreateSavedViewRequestValidator` | `Name` NotEmpty, MaxLength(200) |
| `Granit.Querying.Endpoints` | `UpdateSavedViewRequestValidator` | `Name` NotEmpty, MaxLength(200) |
| `Granit.DataExchange.Endpoints` | `CreateExportJobRequestValidator` | `DefinitionName`, `Format` NotEmpty |
| `Granit.DataExchange.Endpoints` | `SaveExportPresetRequestValidator` | `DefinitionName`, `PresetName`, `SelectedFields`, `Format` NotEmpty |

## Référence des validateurs

### Identifiants personnels

| Méthode | Identifiant | Algorithme | Normalisation |
| --- | --- | --- | --- |
| `.BelgianNiss()` | NISS / INSZ belge | 97-mod (97 − base mod 97) | Points, tirets |
| `.FrenchNir()` | NIR français (Sécu) | 97-mod, depts 2A/2B Corse | Espaces, points, tirets |
| `.BelgianEid()` | Carte d'identité eID belge | 97-mod (12 chiffres) | Tirets (`NNN-NNNNNNN-NN`) |

**NIR** — Format : `SAAMMDDCCCOOOCKK` (sexe + année + mois + code commune INSEE + ordre + clé).
Les départements 2A et 2B (Corse) sont remplacés par 19 et 18 avant le calcul.

### Registres professionnels de santé

| Méthode | Identifiant | Algorithme | Normalisation |
| --- | --- | --- | --- |
| `.FrenchRpps()` | RPPS (Répertoire Partagé PS) | Luhn, 11 chiffres | — |
| `.FrenchAdeli()` | ADELI (auxiliaires médicaux) | Luhn, 9 chiffres | — |
| `.FrenchFiness()` | FINESS (établissements) | Luhn, 9 chiffres | — |
| `.BelgianInami()` | INAMI / RIZIV | 97-mod, 11 chiffres | Séparateurs (`XXXXXX/XXX-XX`) |

**INAMI** — Format : `XXXXXX/XXX-CC` où `CC = 97 − (9 premiers chiffres mod 97)`.

### Entreprises

| Méthode | Identifiant | Algorithme | Normalisation |
| --- | --- | --- | --- |
| `.FrenchSiren()` | SIREN | Luhn, 9 chiffres | Espaces internes |
| `.FrenchSiret()` | SIRET | Luhn, 14 chiffres | Espaces internes |
| `.BelgianBce()` | BCE / KBO | 97-mod, 10 chiffres | Points, espaces (`XXXX.XXX.XXX`) |
| `.FrenchNafCode()` | Code NAF / APE | Regex format `NNNNL` | Majusculation automatique |

**BCE** — Format : `DDDDDDDDCC` où `CC = 97 − (8 premiers chiffres mod 97)`.
Le numéro peut être saisi avec ou sans points : `0100.000.070` ≡ `0100000070`.

**NAF/APE** — Format : 4 chiffres + 1 lettre majuscule (ex. `6201Z`). Seul le format est vérifié,
pas l'appartenance à la nomenclature officielle. La lettre est normalisée en majuscule.

### Fiscalité

| Méthode | Identifiant | Algorithme | Couverture |
| --- | --- | --- | --- |
| `.BelgianVat()` | TVA belge | BCE algorithm | Préfixe `BE` + BCE 9 ou 10 chiffres |
| `.FrenchVat()` | TVA française | Clé SIREN | Préfixe `FR` + clé 2 chiffres + SIREN 9 chiffres |
| `.EuropeanVat()` | TVA UE intracommunautaire | FR/BE algorithmique + regex format | 27 pays membres |

**TVA française** — La clé est calculée : `clé = (12 + 3 × (SIREN mod 97)) mod 97`.
Les clés alphabétiques (DOM-TOM) ne sont pas supportées par ce validateur.

**TVA européenne** — Les validations par pays :

| Pays supportés (format) | AT, BG, CY, CZ, DE, DK, EE, EL, ES, FI, HR, HU, IE, IT, LT, LU, LV, MT, NL, PL, PT, RO, SE, SI, SK |
| --- | --- |
| Pays avec vérification algorithmique | FR (clé SIREN), BE (algorithme BCE) |

Un code pays non reconnu retourne `false`. Insensible à la casse et aux espaces.

### Paiements

| Méthode | Identifiant | Algorithme | Normalisation |
| --- | --- | --- | --- |
| `.Iban()` | IBAN | ISO 7064 MOD 97-10 | Espaces (`FR76 3000...` accepté) |
| `.BicSwift()` | BIC/SWIFT | Regex ISO 9362 | Majusculation automatique |
| `.SepaCreditorIdentifier()` | ICS / Identifiant Créancier SEPA | ISO 7064 MOD 97-10 | Espaces |
| `.FrenchRib()` | RIB français | Clé pondérée 89/15/3, mod 97 | Espaces, tirets |
| `.BelgianAccountNumber()` | Numéro de compte belge | mod 97 sur 10 chiffres de base | Tirets (`NNN-NNNNNNN-NN`) |

**BIC/SWIFT** — 8 caractères (`GEBABEBB`) ou 11 avec code de branche (`GEBABEBB36A`).
Format : 4 lettres (banque) + 2 lettres (pays) + 2 alphanums (lieu) + 3 alphanums optionnels (branche).

**ICS** — Format : `CC + 2 chiffres de contrôle + 3 CBA + identifiant national`.
La vérification est identique à l'IBAN : les 4 premiers caractères sont déplacés en fin de chaîne,
les lettres sont converties en chiffres (A=10…Z=35), et le résultat modulo 97 doit être égal à 1.

**RIB français** — Format : `BBBBBGGGGGGCCCCCCCCCCCCKK` (5 banque + 5 guichet + 11 compte + 2 clé).
Clé = `97 − (89 × banque + 15 × guichet + 3 × compte) mod 97` (97 si reste = 0).
Les lettres dans le compte sont substituées (A,J→1 ; B,K,S→2 ; … ; I,R,Z→9) avant calcul.

**Compte belge** — Format : `NNN-NNNNNNN-NN` (12 chiffres, les tirets sont optionnels).
Clé = `(10 premiers chiffres) mod 97`, ou 97 si le reste est 0.

### Contact

| Méthode | Format | Algorithme |
| --- | --- | --- |
| `.Email()` | Adresse e-mail | Regex RFC 5321 pratique — null et espaces rejetés |
| `.E164Phone()` | E.164 (`+32...`, `+33...`) | Regex `^\+[1-9]\d{7,14}$` |

### Adresse

| Méthode | Identifiant | Format | Normalisation |
| --- | --- | --- | --- |
| `.FrenchPostalCode()` | Code postal français | 5 chiffres, range 01000–99999 | — |
| `.BelgianPostalCode()` | Code postal belge | 4 chiffres, range 1000–9999 | — |
| `.FrenchInseeCode()` | Code INSEE commune | 5 chars : depts 01–95, 2A/2B, DOM 971–976 | Insensible à la casse |

**Code INSEE** — Métropole : `(01–95 ou 2A/2B) + 3 chiffres commune`. DOM : `97[1-6] + 2 chiffres commune`.
Les départements 96–99 n'existent pas et sont rejetés.

### Locale

| Méthode | Identifiant | Format | Normalisation |
| --- | --- | --- | --- |
| `.Iso3166Alpha2CountryCode()` | Code pays ISO 3166-1 alpha-2 | 2 lettres majuscules (ex. `BE`, `FR`) | Majusculation + trim |
| `.Bcp47LanguageTag()` | Balise de langue BCP 47 | `ll[-Script][-rr]` (ex. `fr`, `fr-BE`, `zh-Hans-CN`) | Insensible à la casse |

**BCP 47 (sous-ensemble pratique)** : langue 2–3 chars + script optionnel 4 chars + région optionnelle 2 chars.
Les sous-balises numériques de région (ex. `419`) et les extensions ne sont pas supportées.

## Codes d'erreur

Tous les codes suivent la convention `Granit:Validation:{ValidatorName}` :

| Code | Description |
| --- | --- |
| `Granit:Validation:InvalidBelgianNiss` | NISS / INSZ invalide |
| `Granit:Validation:InvalidFrenchNir` | NIR / Numéro de Sécurité Sociale invalide |
| `Granit:Validation:InvalidBelgianEid` | Numéro de carte d'identité eID invalide |
| `Granit:Validation:InvalidFrenchRpps` | Numéro RPPS invalide |
| `Granit:Validation:InvalidFrenchAdeli` | Numéro ADELI invalide |
| `Granit:Validation:InvalidFrenchFiness` | Numéro FINESS invalide |
| `Granit:Validation:InvalidBelgianInami` | Numéro INAMI / RIZIV invalide |
| `Granit:Validation:InvalidFrenchSiren` | SIREN invalide |
| `Granit:Validation:InvalidFrenchSiret` | SIRET invalide |
| `Granit:Validation:InvalidBelgianBce` | Numéro BCE / KBO invalide |
| `Granit:Validation:InvalidBelgianVat` | Numéro TVA belge invalide |
| `Granit:Validation:InvalidFrenchVat` | Numéro TVA française invalide |
| `Granit:Validation:InvalidEuropeanVat` | Numéro TVA intracommunautaire invalide |
| `Granit:Validation:InvalidIban` | IBAN invalide |
| `Granit:Validation:InvalidBicSwift` | Code BIC/SWIFT invalide |
| `Granit:Validation:InvalidSepaCreditorIdentifier` | Identifiant Créancier SEPA invalide |
| `Granit:Validation:InvalidFrenchRib` | RIB français invalide |
| `Granit:Validation:InvalidBelgianAccountNumber` | Numéro de compte belge invalide |
| `Granit:Validation:InvalidEmail` | Adresse e-mail invalide |
| `Granit:Validation:InvalidE164Phone` | Numéro de téléphone E.164 invalide |
| `Granit:Validation:InvalidFrenchPostalCode` | Code postal français invalide |
| `Granit:Validation:InvalidBelgianPostalCode` | Code postal belge invalide |
| `Granit:Validation:InvalidFrenchInseeCode` | Code INSEE commune invalide |
| `Granit:Validation:InvalidFrenchNafCode` | Code NAF / APE invalide |
| `Granit:Validation:InvalidIso3166Alpha2` | Code pays ISO 3166-1 alpha-2 invalide |
| `Granit:Validation:InvalidBcp47LanguageTag` | Balise de langue BCP 47 invalide |

Les codes intégrés de FluentValidation (`NotEmpty`, `EmailAddress`, etc.) sont également
réécrits sous la forme `Granit:Validation:{NomDuValidateur}` par `GranitErrorCodeLanguageManager`.

## Algorithmes fondamentaux

Les algorithmes partagés sont dans `Internal/` et ne font pas partie de l'API publique du package.

### Luhn (ISO/IEC 7812-1)

Utilisé par : SIREN, SIRET, RPPS, ADELI, FINESS.

```text
Somme alternée de gauche à droite (en partant de l'avant-dernier chiffre) :
  - positions paires  : valeur × 2 ; si ≥ 10, sommer les chiffres du produit
  - positions impaires: valeur telle quelle
Somme totale mod 10 = 0 → valide
```

### Modulo 97 (ISO 7064 MOD 97-10)

Utilisé par : IBAN, ICS (SEPA CI), BCE, NISS, eID, INAMI, NIR, TVA française (indirectement).

```text
Résidu = valeur numérique mod 97
  → pour IBAN/ICS : déplacer les 4 premiers chars en fin, convertir lettres → chiffres, mod 97 = 1
  → pour BCE/NISS/eID/INAMI : clé = 97 − (base mod 97)
```

Le calcul est effectué chiffre par chiffre (`remainder = (remainder * 10 + digit) % 97`) pour
éviter tout débordement de type entier avec de longs identifiants numériques.

## Dépendances Granit

| Direction       | Modules                                                                                                           |
|-----------------|-------------------------------------------------------------------------------------------------------------------|
| **Dépend de**   | `Granit.Core`, `Granit.ExceptionHandling`, `Granit.Localization`, `Microsoft.AspNetCore.App` (FrameworkReference) |
| **Utilisé par** | Packages `.Endpoints` (filtre validation), applications Wolverine (auto-discovery)                                |

> Voir le [graphe de dépendances complet](../../dependencies.md).
