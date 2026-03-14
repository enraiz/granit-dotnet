using Amazon.SimpleNotificationService.Model;
using Granit.Notifications.Sms.Sns.Internal;
using Granit.Notifications.Sms.Sns.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.Notifications.Sms.Sns.Tests;

public sealed class SnsSmsSenderTests
{
    [Fact]
    public void Class_Implements_ISmsSender()
    {
        (SnsSmsSender sut, _) = CreateSender();
        sut.ShouldBeAssignableTo<ISmsSender>();
    }

    [Fact]
    public void Class_IsInternal() =>
        typeof(SnsSmsSender).IsNotPublic.ShouldBeTrue();

    [Fact]
    public void Class_IsSealed() =>
        typeof(SnsSmsSender).IsSealed.ShouldBeTrue();

    [Fact]
    public async Task SendAsync_NullMessage_ThrowsArgumentNull()
    {
        (SnsSmsSender sut, _) = CreateSender();

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.SendAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendAsync_CallsTransportWithCorrectPhoneNumber()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender();

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        await sut.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.PhoneNumber.ShouldBe("+15559876543");
        captured.Message.ShouldBe("Test SMS body");
    }

    [Fact]
    public async Task SendAsync_SetsSmsTypeAttribute()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender();

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        await sut.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.MessageAttributes.ShouldContainKey("AWS.SNS.SMS.SMSType");
        captured.MessageAttributes["AWS.SNS.SMS.SMSType"].StringValue.ShouldBe("Transactional");
    }

    [Fact]
    public async Task SendAsync_WithSenderId_SetsSenderIdAttribute()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender(senderId: "MySender");

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        await sut.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.MessageAttributes.ShouldContainKey("AWS.SNS.SMS.SenderID");
        captured.MessageAttributes["AWS.SNS.SMS.SenderID"].StringValue.ShouldBe("MySender");
    }

    [Fact]
    public async Task SendAsync_MessageSenderIdOverridesOptions()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender(senderId: "OptSender");

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        SmsMessage message = new()
        {
            To = "+15559876543",
            Body = "Test",
            SenderId = "MsgSender",
        };

        await sut.SendAsync(message, TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.MessageAttributes["AWS.SNS.SMS.SenderID"].StringValue.ShouldBe("MsgSender");
    }

    [Fact]
    public async Task SendAsync_WithOriginationNumber_SetsAttribute()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender(originationNumber: "+15551234567");

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        await sut.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.MessageAttributes.ShouldContainKey("AWS.MM.SMS.OriginationNumber");
        captured.MessageAttributes["AWS.MM.SMS.OriginationNumber"].StringValue.ShouldBe("+15551234567");
    }

    [Fact]
    public async Task SendAsync_WithoutSenderId_DoesNotSetSenderIdAttribute()
    {
        (SnsSmsSender sut, ISnsSmsTransport transport) = CreateSender();

        PublishRequest? captured = null;
        transport.PublishAsync(Arg.Do<PublishRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new PublishResponse { MessageId = "msg-123" });

        await sut.SendAsync(SimpleMessage(), TestContext.Current.CancellationToken);

        captured.ShouldNotBeNull();
        captured.MessageAttributes.ShouldNotContainKey("AWS.SNS.SMS.SenderID");
    }

    private static SmsMessage SimpleMessage() => new()
    {
        To = "+15559876543",
        Body = "Test SMS body",
    };

    private static (SnsSmsSender Sut, ISnsSmsTransport Transport) CreateSender(
        string? senderId = null,
        string? originationNumber = null)
    {
        ISnsSmsTransport transport = Substitute.For<ISnsSmsTransport>();

        SnsSmsOptions options = new()
        {
            Region = "eu-west-1",
            SenderId = senderId,
            OriginationNumber = originationNumber,
        };

        SnsSmsSender sut = new(
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<SnsSmsSender>.Instance,
            transport);

        return (sut, transport);
    }
}
