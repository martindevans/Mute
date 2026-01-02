using Mute.Moe.Services.Host;

namespace Mute.Moe.Services.Notifications.RSS;

/// <summary>
/// Automatically send RSS notification to channels. See <see cref="IRssSubscription"/>
/// </summary>
public interface IRssNotificationsSender
    : IHostedService;