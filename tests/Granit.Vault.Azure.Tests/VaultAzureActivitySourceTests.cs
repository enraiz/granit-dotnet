using Granit.Vault.Azure.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Vault.Azure.Tests;

public sealed class VaultAzureActivitySourceTests
{
    [Fact]
    public void Source_HasCorrectName() =>
        VaultAzureActivitySource.Source.Name.ShouldBe("Granit.Vault.Azure");

    [Fact]
    public void Operations_AkvEncrypt_HasCorrectValue() =>
        VaultAzureActivitySource.Operations.AkvEncrypt.ShouldBe("akv.encrypt");

    [Fact]
    public void Operations_AkvDecrypt_HasCorrectValue() =>
        VaultAzureActivitySource.Operations.AkvDecrypt.ShouldBe("akv.decrypt");

    [Fact]
    public void Operations_AkvGetSecret_HasCorrectValue() =>
        VaultAzureActivitySource.Operations.AkvGetSecret.ShouldBe("akv.get-secret");

    [Fact]
    public void Operations_AkvCheckRotation_HasCorrectValue() =>
        VaultAzureActivitySource.Operations.AkvCheckRotation.ShouldBe("akv.check-rotation");
}
