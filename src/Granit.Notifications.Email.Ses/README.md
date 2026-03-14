# Granit.Notifications.Email.Ses

Amazon SES v2 provider for `Granit.Notifications.Email`. Registered as Keyed Service with key `"Ses"` for multi-provider resolution.

Part of the [Granit](https://github.com/granit-fx/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Notifications.Email.Ses
```

## Configuration

```json
{
  "Notifications": {
    "Email": {
      "Provider": "Ses"
    },
    "Ses": {
      "Region": "eu-west-1",
      "FromAddress": "noreply@example.com",
      "ConfigurationSetName": "my-tracking-set"
    }
  }
}
```

When deployed on AWS (ECS, EKS, Lambda), the SDK uses IAM roles automatically.
For local development, set `AccessKeyId` and `SecretAccessKey` or use `aws configure`.

## Documentation

See the [full documentation](https://github.com/granit-fx/granit-dotnet).
