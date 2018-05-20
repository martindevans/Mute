using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    [Group("acl")]
    public class AccessControl
        : InteractiveBase
    {
        private readonly FileSystemService _fs;

        public AccessControl(FileSystemService fs)
        {
            _fs = fs;
        }

        [Command("add"), Summary("I will be able to read files in the given directory")]
        [RequireOwner]
        public async Task AddDirectory(string path)
        {
            _fs.AllowDirectoryAccess(path);

            await this.TypingReplyAsync($"Added directory `{path}` to access whitelist list");
        }
    }
}
