using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Imitation
{
    public class MarkovModelImitation
        : IImitationModelProvider
    {
        private const string HasModelSql = "Select Count(*) FROM MarkovModels WHERE UserId = @UserId LIMIT 1";

        private readonly IDatabaseService _db;

        public MarkovModelImitation(IDatabaseService db)
        {
            _db = db;

            try
            {
                _db.Exec("CREATE TABLE IF NOT EXISTS `MarkovModels` (`UserId` TEXT NOT NULL, `PreviousWord` TEXT NOT NULL, `NextWord` TEXT NOT NULL, `Count` INTEGER NOT NULL, PRIMARY KEY(`PreviousWord`,`NextWord`,`UserId`));");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<bool> HasModel(IUser user)
        {
            try
            {
                using var cmd = _db.CreateCommand();
                cmd.CommandText = HasModelSql;
                cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = user.Id.ToString()});

                var result = (uint)(long)await cmd.ExecuteScalarAsync();
                return (int)result != 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<IImitationModel?> GetModel(IUser user)
        {
            // Early out if no model exists for this user
            if (!await HasModel(user))
                return null;

            // Return model for this user (since there's some data in the DB)
            return new DatabaseMarkovModel(_db, user, new Random());
        }

        public async Task<IImitationModel> BeginTraining(IUser user, IMessageChannel channel, Func<string, Task>? statusCallback = null)
        {
            // Return existing model if one is already trained
            var m = await GetModel(user);
            if (m != null)
                return m;

            m = new DatabaseMarkovModel(_db, user, new Random());

            // Train on the empty string to initialise database with a single value for this user
            if (statusCallback != null)
                await statusCallback("Initialising DB");
            await m.Train("");

            // Train on all messages
            var count = 0;
            await Scrape(channel).Where(msg => msg.Author.Id == user.Id).ForEachAsync(async msg => {
                count++;
                if (count % 250 == 0 && statusCallback != null)
                    await statusCallback($"Processed {count} messages");
                await m.Train(msg.Content);
            });

            if (statusCallback != null)
                await statusCallback($"Completed training with {count} messages");

            return m;
        }

        private async IAsyncEnumerable<IMessage> Scrape(IMessageChannel channel)
        {
            //If start message is not set then get the latest message in the channel now
            var start = (await channel.GetMessagesAsync(1).FlattenAsync()).SingleOrDefault();

            // Keep loading pages until the start message is null
            while (start != null)
            {
                // Add a slight delay between fetching pages so we don't hammer discord too hard
                await Task.Delay(150);

                // Get the next page of messages
                var page = (await channel.GetMessagesAsync(start, Direction.Before, 99).FlattenAsync()).OrderByDescending(a => a.CreatedAt).ToArray();

                // Set the start of the next page to the end of this page
                start = page.LastOrDefault();

                // yield every message in page
                foreach (var message in page)
                    yield return message;
            }
        }

        public async Task Process(MuteCommandContext context)
        {
            var model = await GetModel(context.User);
            if (model == null)
                return;

            await model.Train(context.Message.Resolve(TagHandling.Name, TagHandling.Name, TagHandling.NameNoPrefix, TagHandling.NameNoPrefix, TagHandling.Ignore));
        }
    }

    public class DatabaseMarkovModel
        : IImitationModel
    {
        private const string UpdateModel = "INSERT INTO MarkovModels(UserId, PreviousWord, NextWord, Count) values(@UserId, @PreviousWord, @NextWord, 1) " +
                                           "ON CONFLICT(UserId, PreviousWord, NextWord) DO UPDATE SET Count = Count + 1";

        private const string SelectWords = "SELECT NextWord, Count FROM MarkovModels WHERE UserId = @UserId AND PreviousWord = @PreviousWord";

        private readonly IDatabaseService _db;
        private readonly IUser _user;
        private readonly Random _random;

        public DatabaseMarkovModel(IDatabaseService db, IUser user, Random random)
        {
            _db = db;
            _user = user;
            _random = random;
        }

        private async Task<string?> NextWord(string input, float exhaustion)
        {
            static (string?, int) ParseWord(DbDataReader reader)
            {
                return (
                    reader["NextWord"]?.ToString(),
                    int.Parse(reader["Count"].ToString()!)
                );
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = SelectWords;
                cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = _user.Id.ToString()});
                cmd.Parameters.Add(new SQLiteParameter("@PreviousWord", System.Data.DbType.String) {Value = input});
                return cmd;
            }

            // Select all next possible words
            var words = await new SqlAsyncResult<(string?, int)>(_db, PrepareQuery, ParseWord).Select(a => (a.Item1, (float)a.Item2)).ToArrayAsync();

            // If model doesn't know of any next words end here
            if (words.Length == 0)
                return null;

            // Limit exhaustion to sensible range
            if (exhaustion > 0.9)
                exhaustion = 0.9f;

            // Calculate weights
            var total = 0f;
            for (var i = 0; i < words.Length; i++)
            {
                if (words[i].Item1 != "")
                    words[i].Item2 *= (1 - exhaustion);
                total += words[i].Item2;
            }

            // Select a word
            var index = _random.NextDouble() * total;
            for (var i = 0; i < words.Length; i++)
            {
                index -= words[i].Item2;
                if (index <= 0)
                    return words[i].Item1;
            }

            // We ran off the end, select the last item
            return words.Last().Item1;
        }

        public async Task<string> Predict(string? prompt)
        {
            const int targetLength = 10;
            const int maxLength = 30;
            var sentence = new List<string>();

            while (true)
            {
                // Predict next word, if it's null that's the end of the sentence
                var word = await NextWord(sentence.LastOrDefault() ?? "", sentence.Count / (float)targetLength);
                if (word == null || string.IsNullOrEmpty(word))
                    break;

                // It wasn't null, extend sentence and increase exhaustion
                sentence.Add(word);

                if (sentence.Count >= maxLength)
                {
                    sentence.Add("...");
                    break;
                }
            }

            return string.Join(" ", sentence);
        }

        public async Task Train(string message)
        {
            var words = new string(message.Where(a => !char.IsPunctuation(a)).ToArray()).Split(" ").Where(a => !string.IsNullOrWhiteSpace(a)).Append("").Prepend("").ToArray();

            for (var i = 0; i < words.Length - 1; i++)
            {
                var p = words[i];
                var n = words[i + 1];

                try
                {
                    using var cmd = _db.CreateCommand();
                    cmd.CommandText = UpdateModel;
                    cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = _user.Id.ToString()});
                    cmd.Parameters.Add(new SQLiteParameter("@PreviousWord", System.Data.DbType.String) {Value = p});
                    cmd.Parameters.Add(new SQLiteParameter("@NextWord", System.Data.DbType.String) {Value = n});
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
    }
}
