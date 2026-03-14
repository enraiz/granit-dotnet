using Amazon.SimpleEmailV2.Model;
using Granit.Notifications.Email.Ses.Internal;
using Granit.Notifications.Email.Ses.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Ses.Tests;

public sealed class SesEmailSenderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static (SesEmailSender Sender, ISesTransport Transport) CreateSender(SesOptions? options = null)
    {
        SesOptions opts = options ?? new SesOptions
        {
            Region = "eu-west-1",
            FromAddress = "noreply@example.com",
            TimeoutSeconds = 10,
        };
        ISesTransport transport = Substitute.For<ISesTransport>();
        transport.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResponse { MessageId = "test-message-id" });

        SesEmailSender sender = new(
            Microsoft.Extensions.Options.Options.Create(opts),
            NullLogger<SesEmailSender>.Instance,
            () => transport);

        return (sender, transport);
    }

    private static EmailMessage SimpleMessage(string? fromOverride = null, string? plainText = null) =>
        new()
        {
            To = "recipient@example.com",
            Subject = "Test subject",
            HtmlBody = "<p>Hello</p>",
            FromOverride = fromOverride,
            PlainTextBody = plainText,
        };

    // -------------------------------------------------------------------------
    // Class structure
    // -------------------------------------------------------------------------

    [Fact]
    public void Class_Implements_IEmailSender()
    {
        (SesEmailSender sender, ISesTransport _) = CreateSender();
        sender.ShouldBeAssignableTo<IEmailSender>();
    }

    [Fact]
    public void Class_IsSealed() =>
        typeof(SesEmailSender).IsSealed.ShouldBeTrue();

    [Fact]
    public void Class_IsInternal() =>
        typeof(SesEmailSender).IsNotPublic.ShouldBeTrue();

    // -------------------------------------------------------------------------
    // SendAsync — successful send
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_CallsSesTransportWithCorrectRequest()
    {
        (SesEmailSender sender, ISesTransport transport) = CreateSender();
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.FromEmailAddress.ShouldBe("noreply@example.com");
        captured.Destination.ToAddresses.ShouldContain("recipient@example.com");
        captured.Content.Simple.Subject.Data.ShouldBe("Test subject");
        captured.Content.Simple.Body.Html.Data.ShouldBe("<p>Hello</p>");
    }

    [Fact]
    public async Task SendAsync_DisposesTransportAfterSend()
    {
        (SesEmailSender sender, ISesTransport transport) = CreateSender();

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        transport.Received(1).Dispose();
    }

    // -------------------------------------------------------------------------
    // Sender address resolution
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_WithFromOverride_UsesSenderAddressFromOverride()
    {
        (SesEmailSender sender, ISesTransport transport) = CreateSender();
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(
            SimpleMessage(fromOverride: "custom@example.com"),
            TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.FromEmailAddress.ShouldBe("custom@example.com");
    }

    [Fact]
    public async Task SendAsync_WithoutFromOverride_UsesFromAddress()
    {
        SesOptions opts = new()
        {
            Region = "eu-west-1",
            FromAddress = "sender@example.com",
            TimeoutSeconds = 5,
        };
        (SesEmailSender sender, ISesTransport transport) = CreateSender(opts);
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.FromEmailAddress.ShouldBe("sender@example.com");
    }

    [Fact]
    public async Task SendAsync_WithoutFromOverrideOrFromAddress_FallsBackToNoreply()
    {
        SesOptions opts = new()
        {
            Region = "eu-west-1",
            FromAddress = null,
            TimeoutSeconds = 5,
        };
        (SesEmailSender sender, ISesTransport transport) = CreateSender(opts);
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.FromEmailAddress.ShouldBe("noreply@localhost");
    }

    // -------------------------------------------------------------------------
    // Body construction
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_WithPlainTextBody_SetsTextContent()
    {
        (SesEmailSender sender, ISesTransport transport) = CreateSender();
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(
            SimpleMessage(plainText: "Hello plain"),
            TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.Content.Simple.Body.Text.Data.ShouldBe("Hello plain");
    }

    [Fact]
    public async Task SendAsync_WithNullPlainTextBody_DoesNotSetTextContent()
    {
        (SesEmailSender sender, ISesTransport transport) = CreateSender();
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(
            SimpleMessage(plainText: null),
            TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.Content.Simple.Body.Text.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Configuration set
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_WithConfigurationSetName_IncludesItInRequest()
    {
        SesOptions opts = new()
        {
            Region = "eu-west-1",
            FromAddress = "sender@example.com",
            ConfigurationSetName = "my-tracking-set",
            TimeoutSeconds = 5,
        };
        (SesEmailSender sender, ISesTransport transport) = CreateSender(opts);
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.ConfigurationSetName.ShouldBe("my-tracking-set");
    }

    [Fact]
    public async Task SendAsync_WithoutConfigurationSetName_DoesNotSetIt()
    {
        SesOptions opts = new()
        {
            Region = "eu-west-1",
            FromAddress = "sender@example.com",
            ConfigurationSetName = null,
            TimeoutSeconds = 5,
        };
        (SesEmailSender sender, ISesTransport transport) = CreateSender(opts);
        SendEmailRequest? captured = null;
        await transport.SendEmailAsync(
            Arg.Do<SendEmailRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.ConfigurationSetName.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Logger
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_LogsEmailSent()
    {
        SesOptions opts = new()
        {
            Region = "eu-central-1",
            FromAddress = "sender@example.com",
            TimeoutSeconds = 5,
        };
        ISesTransport transport = Substitute.For<ISesTransport>();
        transport.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SendEmailResponse { MessageId = "test-id" });

        ILogger<SesEmailSender> loggerSub = Substitute.For<ILogger<SesEmailSender>>();
        loggerSub.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        SesEmailSender sender = new(
            Microsoft.Extensions.Options.Options.Create(opts),
            loggerSub,
            () => transport);

        await sender.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        loggerSub.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("recipient@example.com")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithNullFactory_Throws()
    {
        SesOptions opts = new() { Region = "eu-west-1" };

        Should.Throw<ArgumentNullException>(() =>
            new SesEmailSender(
                Microsoft.Extensions.Options.Options.Create(opts),
                NullLogger<SesEmailSender>.Instance,
                transportFactory: null));
    }
}
