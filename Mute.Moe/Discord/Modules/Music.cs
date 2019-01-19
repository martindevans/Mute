using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Audio;
using Mute.Moe.Discord.Services.Audio.Clips;
using Mute.Moe.Discord.Services.Audio.Playback;

namespace Mute.Moe.Discord.Modules
{
    [Group]
    public class Music
        : BaseModule
    {
        private static readonly TimeSpan ReactionTimeout = TimeSpan.FromSeconds(15);

        private static readonly IReadOnlyDictionary<IEmote, int> ReactionScores = new Dictionary<IEmote, int> {
            { EmojiLookup.Heart, 2 },
            { EmojiLookup.ThumbsUp, 1 },
            { EmojiLookup.Expressionless, 0 },
            { EmojiLookup.ThumbsDown, -1 },
            { EmojiLookup.BrokenHeart, -2 }
        };

        private readonly MusicPlayerService _music;
        private readonly MultichannelAudioService _audio;
        private readonly YoutubeService _youtube;
        private readonly MusicRatingService _ratings;
        private readonly Random _random;

        public Music(MusicPlayerService music, MultichannelAudioService audio, YoutubeService youtube, MusicRatingService ratings, Random random)
        {
            _music = music;
            _audio = audio;
            _youtube = youtube;
            _ratings = ratings;
            _random = random;
        }

        [NotNull, Command("skip"), Summary("Skip the currently playing track")]
        public Task Skip()
        {
            _music.Skip();

            return Task.CompletedTask;
        }

        [NotNull, Command("stop"), Summary("Clear the music queue")]
        public Task StopPlayback()
        {
            _music.Stop();
            return Task.CompletedTask;
        }

        [NotNull, Command("playing"), Summary("Get information about the currently playing track")]
        public async Task NowPlaying()
        {
            var playing = _music.Playing;
            if (playing.Item1 == null)
                await TypingReplyAsync("Nothing is currently playing");
            else
            {
                //Show a message about what's currently playing
                IUserMessage msg;
                if (playing.Item1 is YoutubeAsyncFileAudio youtube)
                {
                    msg = await ReplyAsync(new EmbedBuilder().WithUrl($"https://www.youtube.com/watch?v={youtube.ID}"));

                    await AddYoutubeReactions(msg, youtube.ID);
                }
                else
                {
                    msg = await TypingReplyAsync("Now playing: " + playing.Item1.Name);
                }

                //Wait for song to finish
                await playing.Item2;
                await Task.Delay(ReactionTimeout);

                //Delete playing message
                await msg.DeleteAsync();
            }
        }

        [NotNull, ItemCanBeNull] private async Task<Task> EnqueueMusicClip(Func<IAudioClip> clip)
        {
            if (!await _audio.MoveChannel(Context.User))
            {
                await ReplyAsync("You are not in a voice channel");
                return null;
            }
            else
            {
                return _music.Enqueue(clip());
            }
        }

        [NotNull, ItemCanBeNull] private async Task<Task> EnqueueYoutubeClip(string vid, IUserMessage message)
        {
            var url = $"https://www.youtube.com/watch?v={vid}";

            //Try to get the audio from the cache
            var cached = _youtube.TryGetCachedYoutubeAudio(url);

            if (cached != null)
            {
                Console.WriteLine($"Retrieved {vid} from cache");
                return await EnqueueMusicClip(() => new FileAudio(cached));
            }
            else
            {
                //Add reaction indicating download
                var addEmoji = message.AddReactionAsync(EmojiLookup.Loading);

                //Start downloading the video
                var download = Task.Factory.StartNew(async () => {
                    var yt = await _youtube.GetYoutubeAudio(url);
                    Console.WriteLine("Download complete");
                    return yt;
                }).Unwrap();

                //Wait for download to complete
                await addEmoji;
                await download;

                //Remove emoji indicating download
                await message.RemoveReactionAsync(EmojiLookup.Loading, Context.Client.CurrentUser);

                //Enqueue the track
                return await EnqueueMusicClip(() => new YoutubeAsyncFileAudio(vid, download));
            }
        }

        private async Task RunClipReactions(IUserMessage message, string vid, [NotNull] Task playingClip)
        {
            //Setup reaction buttons
            await Task.Run(async () => await AddYoutubeReactions(message, vid));

            //Wait for the clip to finish playing
            await playingClip;

            //After a short time remove the reactions
            Console.WriteLine("Track complete, starting timeout...");
            await Task.Delay(ReactionTimeout).ContinueWith(async _ => {
                Console.WriteLine("Timeout complete!");
                Interactive.RemoveReactionCallback(message);
                await message.RemoveAllReactionsAsync();
            });
        }

        [Command("play"), Summary("I will download and play audio from a youtube video into whichever voice channel you are in")]
        public async Task EnqueueYoutube(string url)
        {
            // Tolerate people misuing the play command
            if (url == "random")
            {
                await PlayRandom();
                return;
            }

            //Check that the user is in a channel
            if (Context.User is IVoiceState vs && vs.VoiceChannel == null)
            {
                await TypingReplyAsync("You're not in a voice channel!");
                return;
            }

            //Check that the URL is valid
            try
            {
                _youtube.CheckUrl(url);
            }
            catch (Exception e)
            {
                await TypingReplyAsync(e.Message);
                return;
            }

            //Get video ID
            var queryDictionary = HttpUtility.ParseQueryString(new Uri(url).Query);
            var vid = queryDictionary["v"];
            if (vid == null)
            {
                await TypingReplyAsync("I'm sorry, I don't recognise this video URL");
                return;
            }

            //Enqueue the track, if that returns a null task that means something went wrong (early exit)
            var clip = await EnqueueYoutubeClip(vid, Context.Message);
            if (clip == null)
                return;

            //Show reaction buttons and remove them once track is complete
            await RunClipReactions(Context.Message, vid, clip);
        }

        [Command("shuffle"), Summary("I will randomise the order of the media player queue")]
        public async Task ShuffleQueue()
        {
            _music.Shuffle();

            await TypingReplyAsync($"Shuffled {_music.Queue.Count()} items");
        }

        [Command("play-random"), Summary("I will play a random track which has previously been played and rated")]
        public async Task PlayRandom()
        {
            //Get a set of youtube items currently in the play queue
            var queue = new HashSet<string>(_music.Queue.OfType<YoutubeAsyncFileAudio>().Select(a => a.ID));

            //Get a list of top rated tracks (best first) which are not already in the queue
            var rated = (await _ratings.GetAggregateTrackRatings())
                        .Where(a => !queue.Contains(a.Item1))
                        .OrderByDescending(a => a.Item2)
                        .Select(NormalizeScore)
                        .ToArray();

            Console.WriteLine($"{rated.Length} tracks can be selected to play randomly");

            //Select one (weighted by rating)
            var vid = SelectWeightedItem(rated, _random);

            if (vid != null)
            {
                var clip = await EnqueueYoutubeClip(vid, Context.Message);
                if (clip == null)
                    return;

                var url = $"https://www.youtube.com/watch?v={vid}";
                var msg = await ReplyAsync(url);

                await RunClipReactions(msg, vid, clip);
            }
            else
                Console.WriteLine("Didn't select a track to play");
        }

        [CanBeNull] private static T SelectWeightedItem<T>([NotNull] IEnumerable<(T, float)> items, [NotNull] Random random) where T : class 
        {
            //Get total ratings
            var ratingSum = items.Select(a => a.Item2).Sum();

            //Choose the "index" of the item we're going to use
            var index = random.NextDouble() * ratingSum;

            //Find that item
            var accumulator = 0f;
            foreach (var item in items)
            {
                if (accumulator + item.Item2 >= index)
                    return item.Item1;
                else
                    accumulator += item.Item2;
            }

            return null;
        }

        /// <summary>
        /// Scores on tracks can be negative, transform into positive rating (lower scores still come lower in a total ordering)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static (string, float) NormalizeScore((string, int) item)
        {
            if (item.Item2 >= 0)
                return (item.Item1, (float)item.Item2 + 1);
            else
                return (item.Item1, 1f / -item.Item2);
        }

        private async Task AddYoutubeReactions(IUserMessage message, string id)
        {
            Interactive.AddReactionCallback(message, new ReactionCallbackHandler(_ratings, Context, id));

            //Add the reaction options (from love to hate)
            foreach (var reactionScore in ReactionScores)
                await message.AddReactionAsync(reactionScore.Key);
        }

        private class ReactionCallbackHandler
            : IReactionCallback
        {
            private readonly MusicRatingService _rating;
            private readonly string _id;

            public RunMode RunMode => RunMode.Async;

            [NotNull] public ICriterion<SocketReaction> Criterion => new EmptyCriterion<SocketReaction>();

            public TimeSpan? Timeout => TimeSpan.FromMinutes(60);

            public SocketCommandContext Context { get; }

            public ReactionCallbackHandler(MusicRatingService rating, SocketCommandContext context, string id)
            {
                _rating = rating;
                _id = id;
                Context = context;
            }

            public async Task<bool> HandleCallbackAsync([NotNull] SocketReaction reaction)
            {
                if (ReactionScores.TryGetValue(reaction.Emote, out var score))
                    await _rating.Record(_id, reaction.User.Value.Id, score);

                return false;
            }
        }
    }
}
