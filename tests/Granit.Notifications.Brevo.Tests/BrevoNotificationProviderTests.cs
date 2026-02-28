// =============================================================================
// Tests - BrevoNotificationProvider
// =============================================================================
// Verifies the unified Brevo provider: email, SMS, and WhatsApp sending via
// Brevo Transactional API, correct endpoint routing, payload mapping, and
// error handling on non-2xx responses.
// =============================================================================

using System.Net;
using System.Text.Json;
using Granit.Notifications.Email;
using Granit.Notifications.Sms;
using Granit.Notifications.WhatsApp;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Brevo.Tests;

public sealed class BrevoNotificationProviderTests : IDisposable
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly BrevoNotificationProvider _provider;

    public BrevoNotificationProviderTests()
    {
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/"),
        };

        _httpClientFactory.CreateClient("Brevo").Returns(_httpClient);

        IOptions<BrevoOptions> options = Options.Create(new BrevoOptions
        {
            ApiKey = "test-key",
            DefaultSenderEmail = "default@test.com",
            DefaultSenderName = "Test App",
            DefaultSmsSenderId = "TestApp",
        });

        _provider = new BrevoNotificationProvider(_httpClientFactory, options);
    }

    // -------------------------------------------------------------------------
    // Email
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendEmailAsync_PostsToCorrectEndpoint()
    {
        IEmailSender emailSender = _provider;

        await emailSender.SendAsync(
            new EmailMessage
            {
                To = "user@test.com",
                Subject = "Test Subject",
                HtmlBody = "<p>Hello</p>",
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("smtp/email");
    }

    [Fact]
    public async Task SendEmailAsync_IncludesCorrectPayload()
    {
        IEmailSender emailSender = _provider;

        await emailSender.SendAsync(
            new EmailMessage
            {
                To = "user@test.com",
                Subject = "Test Subject",
                HtmlBody = "<p>Hello</p>",
                PlainTextBody = "Hello",
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        string body = _handler.Requests[0].Body;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        root.GetProperty("to")[0].GetProperty("email").GetString().ShouldBe("user@test.com");
        root.GetProperty("subject").GetString().ShouldBe("Test Subject");
        root.GetProperty("htmlContent").GetString().ShouldBe("<p>Hello</p>");
        root.GetProperty("textContent").GetString().ShouldBe("Hello");
    }

    [Fact]
    public async Task SendEmailAsync_UsesFromOverride_WhenProvided()
    {
        IEmailSender emailSender = _provider;

        await emailSender.SendAsync(
            new EmailMessage
            {
                To = "user@test.com",
                Subject = "Test",
                HtmlBody = "<p>Hi</p>",
                FromOverride = "custom@test.com",
            },
            TestContext.Current.CancellationToken);

        string body = _handler.Requests[0].Body;
        body.ShouldContain("custom@test.com");
    }

    [Fact]
    public async Task SendEmailAsync_UsesDefaultSender_WhenNoOverride()
    {
        IEmailSender emailSender = _provider;

        await emailSender.SendAsync(
            new EmailMessage
            {
                To = "user@test.com",
                Subject = "Test",
                HtmlBody = "<p>Hi</p>",
            },
            TestContext.Current.CancellationToken);

        string body = _handler.Requests[0].Body;
        body.ShouldContain("default@test.com");
        body.ShouldContain("Test App");
    }

    [Fact]
    public async Task SendEmailAsync_ThrowsOnNon2xx()
    {
        _handler.ResponseStatusCode = HttpStatusCode.InternalServerError;
        IEmailSender emailSender = _provider;

        Func<Task> act = () => emailSender.SendAsync(
            new EmailMessage
            {
                To = "user@test.com",
                Subject = "Test",
                HtmlBody = "<p>Hi</p>",
            },
            TestContext.Current.CancellationToken);

        await Should.ThrowAsync<HttpRequestException>(act);
    }

    // -------------------------------------------------------------------------
    // SMS
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendSmsAsync_PostsToCorrectEndpoint()
    {
        ISmsSender smsSender = _provider;

        await smsSender.SendAsync(
            new SmsMessage
            {
                To = "+32470000000",
                Body = "Hello SMS",
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("transactionalSMS/sms");
    }

    [Fact]
    public async Task SendSmsAsync_IncludesCorrectPayload()
    {
        ISmsSender smsSender = _provider;

        await smsSender.SendAsync(
            new SmsMessage
            {
                To = "+32470000000",
                Body = "Hello SMS",
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        string body = _handler.Requests[0].Body;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        root.GetProperty("recipient").GetString().ShouldBe("+32470000000");
        root.GetProperty("content").GetString().ShouldBe("Hello SMS");
        root.GetProperty("type").GetString().ShouldBe("transactional");
    }

    [Fact]
    public async Task SendSmsAsync_UsesSenderIdFromMessage_WhenProvided()
    {
        ISmsSender smsSender = _provider;

        await smsSender.SendAsync(
            new SmsMessage
            {
                To = "+32470000000",
                Body = "Hello SMS",
                SenderId = "CustomId",
            },
            TestContext.Current.CancellationToken);

        string body = _handler.Requests[0].Body;
        body.ShouldContain("CustomId");
    }

    [Fact]
    public async Task SendSmsAsync_UsesDefaultSenderId_WhenNullInMessage()
    {
        ISmsSender smsSender = _provider;

        await smsSender.SendAsync(
            new SmsMessage
            {
                To = "+32470000000",
                Body = "Hello SMS",
            },
            TestContext.Current.CancellationToken);

        string body = _handler.Requests[0].Body;
        body.ShouldContain("TestApp");
    }

    // -------------------------------------------------------------------------
    // WhatsApp
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendWhatsAppAsync_PostsToCorrectEndpoint()
    {
        IWhatsAppSender whatsAppSender = _provider;

        await whatsAppSender.SendAsync(
            new WhatsAppMessage
            {
                To = "+32470000000",
                TemplateName = "welcome_template",
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        _handler.Requests[0].Url.ShouldContain("whatsapp/sendTemplate");
    }

    [Fact]
    public async Task SendWhatsAppAsync_IncludesCorrectPayload()
    {
        IWhatsAppSender whatsAppSender = _provider;

        await whatsAppSender.SendAsync(
            new WhatsAppMessage
            {
                To = "+32470000000",
                TemplateName = "welcome_template",
                TemplateParameters = ["Jean", "2026"],
            },
            TestContext.Current.CancellationToken);

        _handler.Requests.Count.ShouldBe(1);
        string body = _handler.Requests[0].Body;
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;
        root.GetProperty("contactNumbers")[0].GetString().ShouldBe("+32470000000");
        root.GetProperty("templateId").GetString().ShouldBe("welcome_template");
        root.GetProperty("params")[0].GetString().ShouldBe("Jean");
        root.GetProperty("params")[1].GetString().ShouldBe("2026");
    }

    [Fact]
    public async Task SendWhatsAppAsync_DefaultsLanguageToFrench_WhenNull()
    {
        IWhatsAppSender whatsAppSender = _provider;

        await whatsAppSender.SendAsync(
            new WhatsAppMessage
            {
                To = "+32470000000",
                TemplateName = "welcome_template",
            },
            TestContext.Current.CancellationToken);

        string body = _handler.Requests[0].Body;
        body.ShouldContain("\"language\":\"fr\"");
    }

    // -------------------------------------------------------------------------
    // IDisposable
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
