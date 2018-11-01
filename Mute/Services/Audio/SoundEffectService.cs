using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services.Audio.Playback;
using NAudio.Wave;

namespace Mute.Services.Audio
{
    public class SoundEffectService
    {
        private const string InsertSfxSql = "INSERT INTO `Sfx` (`Name`, `FileId`) VALUES (@Name, @FileId)";
        private const string FindSfxSql = "Select * from Sfx where Name like '%' || @Search || '%'";

        [NotNull] private readonly MultichannelAudioService _audio;
        [NotNull] private readonly Random _random;
        [NotNull] private readonly DatabaseService _database;
        [NotNull] private readonly SimpleQueueChannel<SoundEffect> _queue;
        [NotNull] private readonly SoundEffectConfig _config;

        public SoundEffectService([NotNull] Configuration config, [NotNull] MultichannelAudioService audio, [NotNull] Random random, [NotNull] DatabaseService database)
        {
            _config = config.SoundEffects;
            _audio = audio;
            _random = random;
            _database = database;
            _queue = new SimpleQueueChannel<SoundEffect>();

            audio.Open(_queue);

            _database.Exec("CREATE TABLE IF NOT EXISTS `Sfx` (`Name` TEXT NOT NULL, `FileId` TEXT NOT NULL)");
        }

        public async Task<(bool, string)> Play(IUser user, string searchstring)
        {
            var items = await Find(searchstring);
            if (items.Count == 0)
                return (false, "Cannot find any items by search string");

            var item = items.Random(_random);
            if (!File.Exists(item.Path))
                return (false, "The file for this sound effect is missing!");

            if (!await _audio.MoveChannel(user))
                return (false, "You are not in a voice channel!");

#pragma warning disable 4014 //don't want to await this task - it completes _when the sfx finishes_
            _queue.Enqueue(item, new AudioFileReader(item.Path));
#pragma warning restore 4014
            return (true, "ok");
        }

        [ItemNotNull]
        public async Task<IReadOnlyList<SoundEffect>> Find(string search)
        {
            //Insert into the database
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = FindSfxSql;
                cmd.Parameters.Add(new SQLiteParameter("@Search", System.Data.DbType.String) { Value = search.ToLowerInvariant() });

                var results = new List<SoundEffect>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new SoundEffect(
                            (string)reader["Name"],
                            Path.Combine(_config.SfxFolder, (string)reader["FileId"])
                        ));
                    }
                }

                return results;
            }
        }

        public async Task<(bool, string)> Create([NotNull] string name, [NotNull] byte[] data)
        {
            //Generate a unique name for this name+data combo
            var hashName = name.SHA256();
            var hashData = data.SHA256();
            var hash = $"{hashName}{hashData}";
            var path = Path.Combine(_config.SfxFolder, hash);

            //Check that there isn't a file collision
            if (File.Exists(path))
                return (false, "file already exists, use a different name");

            //Write data to file
            using (var writer = File.OpenWrite(path))
                writer.Write(data, 0, data.Length);

            //Insert into the database
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertSfxSql;
                cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = name.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@FileId", System.Data.DbType.String) { Value = hash });

                await cmd.ExecuteNonQueryAsync();
            }

            return (true, "ok");
        }

        public struct SoundEffect
        {
            public readonly string Name;
            public readonly string Path;

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
