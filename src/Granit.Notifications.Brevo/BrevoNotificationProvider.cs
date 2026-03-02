using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Granit.Notifications.Email;
using Granit.Notifications.Sms;
using Granit.Notifications.WhatsApp;
using Microsoft.Extensions.Options;

namespace Granit.Notifications.Brevo;

/// <summary>
/// Unified Brevo provider implementing <see cref="IEmailSender"/>, <see cref="ISmsSender"/>,
/// and <see cref="IWhatsAppSender"/>. Uses Brevo Transactional API via <see cref="HttpClient"/>.
/// Registered as three Keyed Services with key "Brevo".
/// </summary>
internal sealed class BrevoNotificationProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<BrevoOptions> options) : IEmailSender, ISmsSender, IWhatsAppSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    async Task IEmailSender.SendAsync(EmailMessage message, CancellationToken ct)
    {
        BrevoOptions opts = options.Value;
        HttpClient client = httpClientFactory.CreateClient("Brevo");

        object payload = new
        {
            sender = new
            {
                email = message.FromOverride ?? opts.DefaultSenderEmail,
                name = opts.DefaultSenderName,
            },
            to = new[] { new { email = message.To } },
            subject = message.Subject,
            htmlContent = message.HtmlBody,
            textContent = message.PlainTextBody,
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "smtp/email", payload, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    async Task ISmsSender.SendAsync(SmsMessage message, CancellationToken ct)
    {
        BrevoOptions opts = options.Value;
        HttpClient client = httpClientFactory.CreateClient("Brevo");

        object payload = new
        {
            sender = message.SenderId ?? opts.DefaultSmsSenderId,
            recipient = message.To,
            content = message.Body,
            type = "transactional",
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "transactionalSMS/sms", payload, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    async Task IWhatsAppSender.SendAsync(WhatsAppMessage message, CancellationToken ct)
    {
        HttpClient client = httpClientFactory.CreateClient("Brevo");

        object payload = new
        {
            senderNumber = (string?)null, // Brevo provides the sender number
            contactNumbers = new[] { message.To },
            templateId = message.TemplateName,
            @params = message.TemplateParameters,
            language = message.Language ?? "fr",
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "whatsapp/sendTemplate", payload, JsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
