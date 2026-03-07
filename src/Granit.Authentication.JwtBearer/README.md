# Granit.Authentication.JwtBearer

Generic JWT Bearer authentication (OIDC) for Granit applications. Provides `ICurrentUserService`,
`JwtBearerAuthOptions`, the `Authenticated` policy, and provider-agnostic OIDC Back-Channel Logout
support (`IRevokedSessionStore`, `BackChannelLogoutTokenValidator`, `MapBackChannelLogout`).

Part of the [granit](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Authentication.JwtBearer
```

## Documentation

See the [full documentation](https://gitlab.digitaldynamics.be/digital-dynamics/granit-dotnet/-/blob/develop/docs/framework/security/authentication.md).
