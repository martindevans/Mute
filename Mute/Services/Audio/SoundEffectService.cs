using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services.Audio
{
    public class SoundEffectService
    {
        public async Task<(bool, string)> Play(string searchstring)
        {
            // Interrupting or mixing with currently playing clips?
            // Do we maintain a separate queue of sfx, so e.g. sfx interrupt/mix with music but wait for other sfx?

            return (false, "not implemented");
        }

        [ItemNotNull]
        public async Task<IReadOnlyList<SoundEffect>> Find(string search)
        {
            return new[] {
                "sfx 1",
                "sfx 2",
                "sfx 3",
                "sfx 4",
                "sfx 5",
                "sfx 6",
                "sfx 7",
                "sfx 8",
                "sfx 9",
            }.Select(a => new SoundEffect(a, a)).ToArray();
        }

        public async Task<(bool, string)> Create(string name, byte[] data)
        {
            return (false, "not implemented");
        }

        public struct SoundEffect
        {
            public string Name;
            public string Path;

            public SoundEffect(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        
    }
}
