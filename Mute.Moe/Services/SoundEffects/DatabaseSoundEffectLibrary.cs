using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Audio.Mixing;
using Mute.Moe.Services.Database;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.SoundEffects
{
    public class DatabaseSoundEffectLibrary
        : ISoundEffectLibrary
    {
        private const string InsertSfxSql = "INSERT INTO `Sfx2` (`GuildId`, `Name`, `FileName`) VALUES (@GuildId, @Name, @FileName);";
        private const string GetSfxByNameSql = "Select * from Sfx2 where Name = @Name AND GuildId = @GuildId";
        private const string FindSfxSql = "Select * from Sfx2 where GuildId = @GuildId AND Name like '%' || @Search || '%'";
        private const string FindAllSfxSql = "Select * from Sfx2";

        [NotNull] private readonly SoundEffectConfig _config;
        [NotNull] private readonly IDatabaseService _database;
        [NotNull] private readonly IFileSystem _fs;

        public DatabaseSoundEffectLibrary([NotNull] Configuration config, [NotNull] IDatabaseService database, [NotNull] IFileSystem fs)
        {
            _config = config.SoundEffects;
            _database = database;
            _fs = fs;

            _database.Exec("CREATE TABLE IF NOT EXISTS `Sfx2` (`GuildId` TEXT NOT NULL, `Name` TEXT NOT NULL, `FileName` TEXT NOT NULL)");
        }

        public async Task<ISoundEffect> Create(ulong guild, string name, byte[] data)
        {
            var normalized = NormalizeAudioData(data);

            //Choose a unique name for this file based on the hash(name, data) and guild
            var hashName = name.SHA256();
            var hashData = normalized.SHA256();
            var hash = $"{hashName}{hashData}{guild}".SHA256();
            var fileName = hash + ".wav";
            var pathDir = Path.Combine(_config.SfxFolder, guild.ToString());
            var path = Path.Combine(pathDir, fileName);

            //Check that there isn't a file collision
            if (_fs.File.Exists(path))
                throw new InvalidOperationException("File already exists, use a different name");

            //Ensure the path exists to put the file where it needs to go
            _fs.Directory.CreateDirectory(pathDir);

            //Write out to disk
            normalized.Position = 0;
            using (var fs = _fs.File.Create(path))
                await normalized.CopyToAsync(fs);

            //Insert into the database
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertSfxSql;
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = name.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@FileName", System.Data.DbType.String) { Value = fileName });
                await cmd.ExecuteNonQueryAsync();
            }

            return new DatabaseSoundEffect(_config.SfxFolder, guild, fileName, name);
        }

        public async Task<ISoundEffect> Alias([NotNull] string alias, [NotNull] ISoundEffect sfx)
        {
            alias = alias.ToLowerInvariant();

            //Check if this sfx alias already exists
            if (await Get(sfx.Guild, alias) != null)
                throw new InvalidOperationException("Sound effect `{alias}` already exists in this guild");

            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertSfxSql;
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = sfx.Guild });
                cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = alias });
                cmd.Parameters.Add(new SQLiteParameter("@FileName", System.Data.DbType.String) { Value = sfx.Path });

                await cmd.ExecuteNonQueryAsync();
            }

            return new DatabaseSoundEffect(_config.SfxFolder, sfx.Guild, sfx.Path, alias);
        }

        public async Task<ISoundEffect> Get(ulong guild, string name)
        {
            ISoundEffect Parse(DbDataReader reader)
            {
                return new DatabaseSoundEffect(
                    _config.SfxFolder,
                    ulong.Parse((string)reader["GuildId"]),
                    (string)reader["FileName"],
                    (string)reader["Name"]
                );
            }

            DbCommand Prepare(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetSfxByNameSql;
                cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = name.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
                return cmd;
            }

            return await new SqlAsyncResult<ISoundEffect>(_database, Prepare, Parse).FirstOrDefault();
        }

        public async Task<IAsyncEnumerable<ISoundEffect>> Find(ulong guild, string search)
        {
            ISoundEffect Parse(DbDataReader reader)
            {
                return new DatabaseSoundEffect(
                    _config.SfxFolder, 
                    ulong.Parse((string)reader["GuildId"]),
                    (string)reader["FileName"],
                    (string)reader["Name"]
                );
            }

            DbCommand Prepare(IDatabaseService db)
            {
                var cmd = db.CreateCommand();

                cmd.CommandText = search == "*"
                                ? FindAllSfxSql
                                : FindSfxSql;

                cmd.Parameters.Add(new SQLiteParameter("@Search", System.Data.DbType.String) { Value = search.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
                return cmd;
            }

            return new SqlAsyncResult<ISoundEffect>(_database, Prepare, Parse);
        }

        [NotNull] private static MemoryStream NormalizeAudioData(byte[] data)
        {
            #if NCRUNCH
                return new MemoryStream(data);
            #endif

            //Construct reader which can read the audio (whatever format it is in)
            var reader = new StreamMediaFoundationReader(new MemoryStream(data));
            var sampleProvider = reader.ToSampleProvider();

            // find the max peak
            float max = 0;
            var buffer = new float[reader.WaveFormat.SampleRate];
            int read;
            do
            {
                read = sampleProvider.Read(buffer, 0, buffer.Length);
                if (read > 0)
                    max = Math.Max(max, Enumerable.Range(0, read).Select(i => Math.Abs(buffer[i])).Max());
            } while (read > 0);

            if (Math.Abs(max) < float.Epsilon || max > 1.0f)
                throw new InvalidOperationException("Audio normalization failed to find a reasonable peak volume");
            
            //Write (as wav) with soft clipping and peak volume normalization
            var output = new MemoryStream((int)(reader.Length * 4));
            var input = new SoftClipSampleProvider(new VolumeSampleProvider(sampleProvider) { Volume = 1 / max - 0.05f });
            reader.Position = 0;
            WaveFileWriter.WriteWavFileToStream(output, input.ToWaveProvider16());

            return output;
        }

        private class DatabaseSoundEffect
            : ISoundEffect
        {
            public ulong Guild { get; }
            public string Path { get; }
            public string Name { get; }

            public DatabaseSoundEffect(string rootPath, ulong guild, string fileName, string name)
            {
                Guild = guild;
                Path = System.IO.Path.Combine(rootPath, guild.ToString(), fileName);
                Name = name;
            }
        }
    }
}
