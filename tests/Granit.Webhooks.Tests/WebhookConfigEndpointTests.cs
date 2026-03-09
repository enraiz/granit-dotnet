// =============================================================================
// Tests - WebhookModuleConfigProvider
// =============================================================================
// Verifies the module config provider returns the correct configuration.
// =============================================================================

using Granit.Webhooks.Dtos;
using Granit.Webhooks.Endpoints;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Granit.Webhooks.Tests;

public sealed class WebhookModuleConfigProviderTests
{
    [Fact]
    public void GetConfig_StorePayloadFalse_ReturnsFalse()
    {
        IOptions<WebhooksOptions> options = Options.Create(new WebhooksOptions { StorePayload = false });
        WebhookModuleConfigProvider provider = new(options);

        WebhookModuleConfigResponse result = provider.GetConfig();

        result.StorePayload.ShouldBeFalse();
    }

    [Fact]
    public void GetConfig_StorePayloadTrue_ReturnsTrue()
    {
        IOptions<WebhooksOptions> options = Options.Create(new WebhooksOptions { StorePayload = true });
        WebhookModuleConfigProvider provider = new(options);

        WebhookModuleConfigResponse result = provider.GetConfig();

        result.StorePayload.ShouldBeTrue();
    }
}
