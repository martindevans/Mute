using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Services.Images.Cats;
using Mute.Moe.Services.Images.Dogs;
using Mute.Moe.Services.Randomness;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    public class Pictures
        : BaseModule
    {
        private readonly ICatPictureProvider _cats;
        private readonly IArtificialCatPictureProvider _mlcats;
        private readonly IDogPictureService _dogs;
        private readonly IDiceRoller _roll;

        public Pictures(ICatPictureProvider cats, IArtificialCatPictureProvider mlcats, IDogPictureService dogs, IDiceRoller roll)
        {
            _cats = cats;
            _mlcats = mlcats;
            _dogs = dogs;
            _roll = roll;
        }

        [Command("cat"), Summary("I will find a cute cat picture")]
        public async Task CatAsync()
        {
            var stream = await _cats.GetCatPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("mlcat"), Summary("I will find an ML generated cat picture")]
        public async Task MlCatAsync()
        {
            var stream = await _mlcats.GetCatPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("rollcat"), Summary("I will post a picture which may or may not be a real cat")]
        public async Task MaybeCat()
        {
            var real = _roll.Flip();
            var stream = await (real ? _cats.GetCatPictureAsync() : _mlcats.GetCatPictureAsync());
            stream.Seek(0, SeekOrigin.Begin);

            //Upload image
            await Context.Channel.SendFileAsync(stream, "this might be a cat.png");

            await ReplyAsync("Is this a real cat or not?");

            var next = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));

            if (next == null)
            {
                await ReplyAsync("Too slow!");
            }
            else
            {
                var choice = FuzzyParsing.BooleanChoice(next.Content);
                if (choice.Value == real)
                    await ReplyAsync("Correct!");
                else if (real)
                    await TypingReplyAsync("Wrong! This is a real kitty");
                else
                    await TypingReplyAsync("Wrong! This is a machine kitty");
            }

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
