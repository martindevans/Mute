using System;
using System.Threading.Tasks;
using Discord;

using Mute.Moe.Discord.Context.Preprocessing;

namespace Mute.Moe.Services.Imitation
{
    public interface IImitationModelProvider
        : IMessagePreprocessor
    {
        Task<IImitationModel?> GetModel( IUser user);

        Task<IImitationModel> BeginTraining( IUser user,  IMessageChannel channel, Func<string, Task>? statusCallback = null);
    }

    public interface IImitationModel
    {
        Task<string> Predict(string? prompt);

         Task Train( string message);
    }
}