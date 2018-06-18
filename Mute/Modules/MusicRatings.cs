using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Services.Audio;

namespace Mute.Modules
{
    [Group("ratings")]
    public class MusicRatings
        : InteractiveBase
    {
        private readonly MusicRatingService _ratings;

        public MusicRatings(MusicRatingService ratings)
        {
            _ratings = ratings;
        }

        private async Task PaginatedTracks([NotNull] IEnumerable<string> youtubeIds)
        {
            await PagedReplyAsync(new PaginatedMessage {
                Pages = youtubeIds.Select(a => $"https://www.youtube.com/watch?v={a}"),
            });
        }

        [Command, Summary("I will show the ratings for a user")]
        public async Task GetTrackRatings(IUser user = null)
        {
            user = user ?? Context.User;

            var ratings = (await _ratings.GetUserRatings(user.Id))
                          .OrderBy(a => a.Item2)
                          .ThenBy(a => a.Item1)
                          .Select(a => a.Item1);

            await PaginatedTracks(ratings);
        }

        [Command("best"), Summary("I will show the highest rated tracks")]
        public async Task GetTopTracks()
        {
            var ratings = (await _ratings.GetAggregateTrackRatings())
                          .OrderByDescending(a => a.Item2)
                          .ThenBy(a => a.Item1)
                          .Select(a => a.Item1);

            await PaginatedTracks(ratings);
        }

        [Command("worst"), Summary("I will show the lowest rated tracks")]
        public async Task GetBottomTracks()
        {
            var ratings = (await _ratings.GetAggregateTrackRatings())
                          .OrderBy(a => a.Item2)
                          .ThenBy(a => a.Item1)
                          .Select(a => a.Item1);

            await PaginatedTracks(ratings);
        }
    }
}
