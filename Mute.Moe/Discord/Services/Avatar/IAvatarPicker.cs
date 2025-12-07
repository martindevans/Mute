using System.Threading.Tasks;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Discord.Services.Avatar;

/// <summary>
/// Automatically pick new bot avatars
/// </summary>
public interface IAvatarPicker
    : IHostedService
{
    /// <summary>
    /// Force pick a new avatar now
    /// </summary>
    /// <returns></returns>
    Task<AvatarPickResult> PickAvatarNow();

    /// <summary>
    /// Override the current avatar and set it now
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    Task<AvatarPickResult> SetAvatarNow(string path);

    /// <summary>
    /// Get all of the options for avatars (file paths) which <see cref="PickAvatarNow"/> will pick from
    /// </summary>
    /// <returns></returns>
    Task<string[]> GetOptions();
}

/// <summary>
/// Result of picking an avatar
/// </summary>
public record AvatarPickResult(IReadOnlyList<string> Options, string? Choice);