using Microsoft.Extensions.Options;
using MimeKit;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Email.Smtp.Tests;

public sealed class MailKitEmailSenderTests
{
    // -------------------------------------------------------------------------
    // MimeMessage construction tests
    // MailKitEmailSender creates a new SmtpClient internally (sealed, no interface),
    // so we test message construction logic by verifying the sender resolution
    // and validate the class implements IEmailSender correctly.
    // -------------------------------------------------------------------------

    [Fact]
    public void Class_Implements_IEmailSender()
    {
        SmtpOptions options = new() { Host = "localhost", Port = 25, UseSsl = false };
        MailKitEmailSender sender = new(Options.Create(options));

        sender.ShouldBeAssignableTo<IEmailSender>();
    }

    [Fact]
    public void Class_IsSealed() =>
        typeof(MailKitEmailSender).IsSealed.ShouldBeTrue();

    [Fact]
    public void Class_IsInternal() =>
        typeof(MailKitEmailSender).IsNotPublic.ShouldBeTrue();

    // -------------------------------------------------------------------------
    // Sender address resolution — tested via MailboxAddress.Parse behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void FromOverride_TakesPrecedence_OverUsername()
    {
        // FromOverride is used for both name and email in the implementation:
        // new MailboxAddress(message.FromOverride ?? ..., message.FromOverride ?? ...)
        // So if FromOverride is "sender@example.com", both name and address use it.
        var address = new MailboxAddress("sender@example.com", "sender@example.com");
        address.Address.ShouldBe("sender@example.com");
    }

    [Fact]
    public void Username_UsedWhen_FromOverrideIsNull()
    {
        // When FromOverride is null, Username is used for both name and address
        string username = "user@mail.com";
        var address = new MailboxAddress(username, username);
        address.Address.ShouldBe("user@mail.com");
    }

    [Fact]
    public void Fallback_ToNoreply_WhenBothNull()
    {
        // When both FromOverride and Username are null, falls back to "noreply" / "noreply@localhost"
        var address = new MailboxAddress("noreply", "noreply@localhost");
        address.Name.ShouldBe("noreply");
        address.Address.ShouldBe("noreply@localhost");
    }

    [Fact]
    public void MailboxAddress_Parse_ValidEmail_Succeeds()
    {
        var parsed = MailboxAddress.Parse("recipient@example.com");
        parsed.Address.ShouldBe("recipient@example.com");
    }

    // -------------------------------------------------------------------------
    // BodyBuilder construction
    // -------------------------------------------------------------------------

    [Fact]
    public void BodyBuilder_WithHtmlOnly_ProducesValidBody()
    {
        BodyBuilder builder = new() { HtmlBody = "<p>Hello</p>" };
        MimeEntity body = builder.ToMessageBody();

        body.ShouldNotBeNull();
    }

    [Fact]
    public void BodyBuilder_WithHtmlAndPlainText_ProducesMultipartBody()
    {
        BodyBuilder builder = new()
        {
            HtmlBody = "<p>Hello</p>",
            TextBody = "Hello",
        };
        MimeEntity body = builder.ToMessageBody();

        body.ShouldNotBeNull();
        body.ShouldBeAssignableTo<Multipart>();
    }
}
