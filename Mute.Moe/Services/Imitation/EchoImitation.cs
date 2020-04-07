using System;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Services.Imitation
{
    public class EchoImitationProvider
        : IImitationModelProvider
    {

        [ItemNotNull]
        public async Task<IImitationModel> GetModel(IUser user)
        {
            return new EchoImitation(user);
        }

        public async Task<IImitationModel> BeginTraining(IUser user, [NotNull] IMessageChannel channel, Func<string, Task> statusCallback = null)
        {
            if (statusCallback != null)
            {
                await statusCallback("Update 1");
                await Task.Delay(100);
                await statusCallback("Update 2");
                await Task.Delay(100);
                await statusCallback("Update 3");
                await Task.Delay(100);
            }

            return new EchoImitation(user);
        }

        private class EchoImitation
            : IImitationModel
        {
            private readonly IUser _user;

            public EchoImitation(IUser user)
            {
                _user = user;
            }

            public async Task<string> Predict(string prompt)
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return $"Hi, I'm {_user.Username}";
                else
                    return prompt;
            }

            public Task Train(string message)
            {
                return Task.CompletedTask;
            }
        }

        public Task Process(MuteCommandContext context)
        {
            Console.WriteLine(context.Message);
            return Task.CompletedTask;
        }
    }
}
