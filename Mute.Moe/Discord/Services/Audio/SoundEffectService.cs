using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Audio.Mixing;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Extensions;
using Mute.Moe.Services;
using Mute.Moe.Services.Database;
using NAudio.Wave;

namespace Mute.Moe.Discord.Services.Audio
{
    public class SoundEffectService
    {
        private const string InsertSfxSql = "INSERT INTO `Sfx` (`Name`, `FileId`) VALUES (@Name, @FileId)";
        private const string GetSfxByNameSql = "Select * from Sfx where Name = @Name";
        private const string FindSfxSql = "Select * from Sfx where Name like '%' || @Search || '%'";
        private const string FindAllSfxSql = "Select * from Sfx";

        [NotNull] private readonly MultichannelAudioService _audio;
        [NotNull] private readonly Random _random;
        [NotNull] private readonly IDatabaseService _database;
        [NotNull] private readonly SimpleQueueChannel<SoundEffect> _queue;
        [NotNull] private readonly SoundEffectConfig _config;

        public SoundEffectService([NotNull] Configuration config, [NotNull] MultichannelAudioService audio, [NotNull] Random random, [NotNull] IDatabaseService database)
        {
            _config = config.SoundEffects;
            _audio = audio;
            _random = random;
            _database = database;
            _queue = new SimpleQueueChannel<SoundEffect>();

            audio.Open(_queue);

            _database.Exec("CREATE TABLE IF NOT EXISTS `Sfx` (`Name` TEXT NOT NULL, `FileId` TEXT NOT NULL)");
        }

        public async Task<(bool, string)> Play(IUser user, [NotNull] string searchstring)
        {
            var items = await Find(searchstring);
            if (items.Count == 0)
                return (false, "Cannot find any items by search string");

            var item = items.Random(_random);
            var path = Path.Combine(_config.SfxFolder, item.FileName);
            if (!File.Exists(path))
                return (false, "The file for this sound effect is missing!");

            if (!await _audio.MoveChannel(user))
                return (false, "You are not in a voice channel!");

#pragma warning disable 4014 //don't want to await this task - it completes _when the sfx finishes_
            _queue.Enqueue(item, new AudioFileReader(path));
#pragma warning restore 4014

            return (true, "ok");
        }

        [ItemNotNull]
        public async Task<IReadOnlyList<SoundEffect>> Find([NotNull] string search)
        {
            //Insert into the database
            using (var cmd = _database.CreateCommand())
            {
                if (search == "*")
                {
                    cmd.CommandText = FindAllSfxSql;
                }
                else
                {
                    cmd.CommandText = FindSfxSql;
                    cmd.Parameters.Add(new SQLiteParameter("@Search", System.Data.DbType.String) { Value = search.ToLowerInvariant() });
                }

                return await ParseSoundEffects(await cmd.ExecuteReaderAsync());
            }
        }

        public async Task<(bool, string)> Create([NotNull] string name, [NotNull] byte[] data)
        {
            try
            {
                //Generate a unique name for this name+data combo
                var hashName = name.SHA256();
                var hashData = data.SHA256();
                var hash = $"{hashName}{hashData}".SHA256();
                var path = Path.Combine(_config.SfxFolder, hash);

                //Check that there isn't a file collision
                if (File.Exists(path))
                    return (false, "file already exists, use a different name");

                //Normalize audio file so peak colume is 1.0 and write out to disk
                WriteNormalizedAudio(path, data);

                //Insert into the database
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertSfxSql;
                    cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) {Value = name.ToLowerInvariant()});
                    cmd.Parameters.Add(new SQLiteParameter("@FileId", System.Data.DbType.String) {Value = hash});

                    await cmd.ExecuteNonQueryAsync();
                }

                return (true, "ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return (false, "Exception!");
            }
        }

        [NotNull]
        public Task NormalizeAllSfx()
        {
            return Task.Run(() => {

                var files = Directory.EnumerateFiles(_config.SfxFolder);
                foreach (var file in files)
                {
                    //Read data and delete source file
                    var data = File.ReadAllBytes(file);
                    File.Delete(file);
                    try
                    {
                        WriteNormalizedAudio(file, data);
                    }
                    catch (Exception e)
                    {
                        //If something went wrong write back the original data
                        File.WriteAllBytes(file, data);
                        Console.WriteLine($"Exception processing file {file}:");
                        Console.WriteLine(e);
                    }
                }

            });
        }

        private static void WriteNormalizedAudio(string path, [NotNull] WaveStream reader)
        {
            var sampleReader = reader.ToSampleProvider();

            // find the max peak
            float max = 0;
            var buffer = new float[reader.WaveFormat.SampleRate];
            int read;
            do
            {
                read = sampleReader.Read(buffer, 0, buffer.Length);
                if (read > 0)
                    max = Math.Max(max, Enumerable.Range(0, read).Select(i => Math.Abs(buffer[i])).Max());
            } while (read > 0);

            if (Math.Abs(max) < float.Epsilon || max > 1.0f)
                throw new InvalidOperationException("Audio normalization failed to find a reasonable peak volume");

            // rewind and amplify
            reader.Position = 0;

            // write out to a new WAV file
            WaveFileWriter.CreateWaveFile16(path, new GainSampleProvider(sampleReader, 1 / max - 0.05f));
        }

        private static void WriteNormalizedAudio(string path, byte[] data)
        {
            using (var reader = new StreamMediaFoundationReader(new MemoryStream(data)))
                WriteNormalizedAudio(path, reader);
        }

        public async Task<(bool, string)> Alias(SoundEffect sfx, [NotNull] string alias)
        {
            var aliased = await Get(alias);
            if (aliased.HasValue)
                return (false, "Sound effect `{alias}` already exists");

            using (var cmdIns = _database.CreateCommand())
            {
                cmdIns.CommandText = InsertSfxSql;
                cmdIns.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = alias.ToLowerInvariant() });
                cmdIns.Parameters.Add(new SQLiteParameter("@FileId", System.Data.DbType.String) { Value = sfx.FileName });

                await cmdIns.ExecuteNonQueryAsync();
            }

            return (true, "ok");
        }

        public async Task<SoundEffect?> Get([NotNull] string name)
        {
            using (var cmdGet = _database.CreateCommand())
            {
                cmdGet.CommandText = GetSfxByNameSql;
                cmdGet.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) {Value = name.ToLowerInvariant()});

                var results = await ParseSoundEffects(await cmdGet.ExecuteReaderAsync());

                if (results.Count == 0)
                    return null;
                else
                    return results[0];
            }
        }

        [ItemNotNull] private static async Task<List<SoundEffect>> ParseSoundEffects([NotNull] DbDataReader reader)
        {
            using (reader)
            {
                var results = new List<SoundEffect>();
                while (await reader.ReadAsync())
                {
                    results.Add(new SoundEffect(
                        (string)reader["Name"],
                        (string)reader["FileId"]
                    ));
                }
                return results;
            }
        }

        public struct SoundEffect
        {
            public readonly string Name;
            public readonly string FileName;

            public SoundEffect(string name, string fileName)
            {
                Name = name;
                FileName = fileName;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
