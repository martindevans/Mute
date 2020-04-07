using System;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Discord.Context.Preprocessing;

namespace Mute.Moe.Services.Imitation
{
    public interface IImitationModelProvider
        : IMessagePreprocessor
    {
        [ItemCanBeNull, NotNull] Task<IImitationModel> GetModel([NotNull] IUser user);

        [ItemNotNull, NotNull] Task<IImitationModel> BeginTraining([NotNull] IUser user, [NotNull] IMessageChannel channel, [CanBeNull] Func<string, Task> statusCallback = null);
    }

    public interface IImitationModel
    {
        [ItemNotNull, NotNull] Task<string> Predict([CanBeNull] string prompt);

        [NotNull] Task Train([NotNull] string message);
    }
}