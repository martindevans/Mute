using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Services.Images;

namespace Mute.Moe.Discord.Modules
{
    public class Pictures
        : ModuleBase
    {
        private readonly ICatPictureService _cats;
        private readonly IDogPictureService _dogs;

        public Pictures(ICatPictureService cats, IDogPictureService dogs)
        {
            _cats = cats;
            _dogs = dogs;
        }

        [Command("cat"), Summary("I will find a cute cat picture")]
        public async Task CatAsync()
        {
            var stream = await _cats.GetCatPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("dog"), Summary("I will find a cute dog picture")]
        public async Task DogAsync()
        {
            var stream = await _dogs.GetDogPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(stream, "dog.png");
        }
    }
}
