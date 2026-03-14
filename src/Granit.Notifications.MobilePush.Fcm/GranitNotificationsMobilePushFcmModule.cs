using Granit.Core.Modularity;
using Granit.Notifications.MobilePush;

namespace Granit.Notifications.MobilePush.Fcm;

/// <summary>
/// Granit module for the Firebase Cloud Messaging (FCM) mobile push provider.
/// </summary>
/// <remarks>
/// Registration is done via <c>AddGranitNotificationsMobilePushFcm()</c>.
/// Registers <c>FcmMobilePushSender</c> as a keyed <c>IMobilePushSender</c> implementation.
/// </remarks>
[DependsOn(typeof(GranitNotificationsMobilePushModule))]
public sealed class GranitNotificationsMobilePushFcmModule : GranitModule;
