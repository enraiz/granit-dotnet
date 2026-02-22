// =============================================================================
// FoundationSecurityModule - Module d'abstractions de sécurité
// =============================================================================
// Module de base fournissant ICurrentUserService (interface uniquement).
// L'implémentation et la configuration JWT Bearer sont dans :
//   Foundation.Authentication.JwtBearer  (FoundationJwtBearerModule)
//   Foundation.Authentication.Keycloak   (FoundationAuthenticationKeycloakModule)
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;

namespace DigitalDynamics.Foundation.Security;

/// <summary>
/// Module Foundation fournissant les abstractions de sécurité (<see cref="ICurrentUserService"/>).
/// Aucun service enregistré ici — utiliser <c>FoundationJwtBearerModule</c> ou
/// <c>FoundationAuthenticationKeycloakModule</c> pour l'implémentation complète.
/// </summary>
public sealed class FoundationSecurityModule : FoundationModule;
