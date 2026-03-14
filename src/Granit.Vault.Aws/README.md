# Granit.Vault.Aws

AWS KMS encryption and Secrets Manager credential provider for Granit applications. Drop-in replacement for `Granit.Vault` (HashiCorp Vault).

Part of the [Granit](https://github.com/granit-fx/granit-dotnet) framework.

## Installation

```bash
dotnet add package Granit.Vault.Aws
```

## Features

- **KMS transit encryption**: `IStringEncryptionProvider` backed by AWS KMS symmetric encryption
- **Secrets Manager**: `IAwsDatabaseCredentialProvider` with automatic rotation detection
- **Health check**: KMS key reachability probe
- **IAM roles**: Default credential chain (ECS/EKS), access keys for local dev

## Configuration

```json
{
  "Vault": {
    "Aws": {
      "Region": "eu-west-1",
      "KmsKeyId": "alias/granit-encryption",
      "DatabaseSecretArn": "arn:aws:secretsmanager:eu-west-1:123456789:secret:db-creds"
    }
  },
  "Encryption": {
    "ProviderName": "AwsKms"
  }
}
```

## Documentation

See the [full documentation](https://github.com/granit-fx/granit-dotnet).
