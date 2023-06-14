using System.Threading.Tasks;
using Discord.WebSocket;

namespace Mute.Moe.Discord.Services.ComponentActions.Responses
{
    public class TimeResponder
    {
        public const string ComponentId = "5845A5AF-F988-4036-9261-4F05004F3B72";

        public TimeResponder(DiscordSocketClient client)
        {
            client.SelectMenuExecuted += SelectMenuExecuted;
        }

        private async Task SelectMenuExecuted(SocketMessageComponent arg)
        {
            if (arg.Data.CustomId != ComponentId)
                return;

            var message = Response(arg.Data.Values.FirstOrDefault() ?? "UTC");
            await arg.RespondAsync(message);
        }

        private string Response(string tz)
        {
            return tz;
        }
    }
}
