using Granit.Vault.GoogleCloud.Diagnostics;
using Shouldly;
using Xunit;

namespace Granit.Vault.GoogleCloud.Tests;

public sealed class VaultGoogleCloudActivitySourceTests
{
    [Fact]
    public void Name_IsGranitVaultGoogleCloud() =>
        VaultGoogleCloudActivitySource.Name.ShouldBe("Granit.Vault.GoogleCloud");

    [Fact]
    public void Source_HasCorrectName() =>
        VaultGoogleCloudActivitySource.Source.Name.ShouldBe("Granit.Vault.GoogleCloud");

    [Fact]
    public void Operations_KmsEncrypt_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Operations.KmsEncrypt.ShouldBe("cloudkms.encrypt");

    [Fact]
    public void Operations_KmsDecrypt_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Operations.KmsDecrypt.ShouldBe("cloudkms.decrypt");

    [Fact]
    public void Operations_KmsGetKey_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Operations.KmsGetKey.ShouldBe("cloudkms.get-key");

    [Fact]
    public void Operations_SecretsObtain_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Operations.SecretsObtain.ShouldBe("secretmanager.obtain");

    [Fact]
    public void Operations_SecretsCheck_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Operations.SecretsCheck.ShouldBe("secretmanager.check-rotation");

    [Fact]
    public void Tags_KeyName_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Tags.KeyName.ShouldBe("cloudkms.key_name");

    [Fact]
    public void Tags_ProjectId_HasExpectedValue() =>
        VaultGoogleCloudActivitySource.Tags.ProjectId.ShouldBe("gcp.project_id");
}
