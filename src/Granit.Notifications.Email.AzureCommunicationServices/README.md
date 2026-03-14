# Granit.Notifications.Email.AzureCommunicationServices

Azure Communication Services email provider for `Granit.Notifications.Email`. Registered as Keyed Service with key `"AzureCommunicationServices"` for multi-provider resolution.

Part of the [Granit](https://github.com/granit-fx/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Notifications.Email.AzureCommunicationServices
```

## Configuration

```json
{
  "AzureCommunicationServices": {
    "Email": {
      "ConnectionString": "endpoint=https://my-acs.communication.azure.com/;accesskey=...",
      "SenderAddress": "noreply@example.com",
      "TimeoutSeconds": 30
    }
  }
}
```

When deployed on Azure (AKS, App Service, Container Apps), use `Endpoint` instead of `ConnectionString` to leverage Managed Identity via `DefaultAzureCredential`:

```json
{
  "AzureCommunicationServices": {
    "Email": {
      "Endpoint": "https://my-acs.communication.azure.com",
      "SenderAddress": "noreply@example.com"
    }
  }
}
```

## Documentation

See the [full documentation](https://github.com/granit-fx/granit-dotnet).
