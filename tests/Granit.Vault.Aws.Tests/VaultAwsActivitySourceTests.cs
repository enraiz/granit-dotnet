using Granit.Vault.Aws.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Vault.Aws.Tests;

public sealed class VaultAwsActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        VaultAwsActivitySource.Source.Name.ShouldBe("Granit.Vault.Aws");

    [Fact]
    public void Operations_KmsEncrypt_HasCorrectValue() =>
        VaultAwsActivitySource.Operations.KmsEncrypt.ShouldBe("kms.encrypt");

    [Fact]
    public void Operations_KmsDecrypt_HasCorrectValue() =>
        VaultAwsActivitySource.Operations.KmsDecrypt.ShouldBe("kms.decrypt");

    [Fact]
    public void Operations_SecretsObtain_HasCorrectValue() =>
        VaultAwsActivitySource.Operations.SecretsObtain.ShouldBe("secrets.obtain");
}
