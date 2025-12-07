using Mute.Moe.Services.Host;

namespace Mute.Moe.Services.Reminders;

/// <summary>
/// service to send reminders at their appointed time
/// </summary>
public interface IReminderSender
    : IHostedService;