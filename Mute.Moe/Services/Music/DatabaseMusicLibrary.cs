using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Music
{
    public class DatabaseMusicLibrary
        : IMusicLibrary
    {
        private const string InsertTrackSql = "INSERT INTO `MusicLibrary2` (`GuildId`, `OwnerId`, `Title`, `FileName`, `Url`, `ThumbnailUrl`, `Duration`) VALUES (@GuildId, @Owner, @Title, @FileName, @Url, @ThumbnailUrl, @Duration);  SELECT last_insert_rowid();";
        private const string FindTrackSql = "SELECT *, rowid as Id FROM MusicLibrary2 WHERE GuildId = @GuildId AND (rowid = @Id or @Id IS null) AND (Url = @Url or @Url IS null) AND (Title like '%' || @Title || '%' or @Title IS null) ORDER BY {{order}} LIMIT ifnull(@Limit, -1)";

         private readonly MusicLibraryConfig _config;
         private readonly IDatabaseService _database;
         private readonly IFileSystem _fs;

        public DatabaseMusicLibrary(Configuration config, IDatabaseService database, IFileSystem fs)
        {
            _config = config.MusicLibrary ?? throw new ArgumentNullException(nameof(config.MusicLibrary));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));

            _database.Exec("CREATE TABLE IF NOT EXISTS `MusicLibrary2` (`GuildId` TEXT NOT NULL, `OwnerId` TEXT NOT NULL, `Title` TEXT NOT NULL, `FileName` TEXT NOT NULL, `Url` TEXT NOT NULL, `ThumbnailUrl` TEXT NOT NULL, `Duration` TEXT NOT NULL)");
        }

        /// <summary>
        /// Add a new track to the music library
        /// </summary>
        /// <param name="guild">Guild which owns this track</param>
        /// <param name="owner"></param>
        /// <param name="audio">Open stream to read audio data from (wav format)</param>
        /// <param name="title">Human readable title of this track</param>
        /// <param name="duration"></param>
        /// <param name="url">URL related to this track</param>
        /// <param name="thumbnailUrl"></param>
        /// <returns></returns>
        public async Task<ITrack> Add(ulong guild, ulong owner, Stream audio, string title, TimeSpan duration, string? url = null, string? thumbnailUrl = null)
        {
            // Make sure the guild music folder exists
            var guildDir = _fs.Path.Combine(_config.MusicFolder, guild.ToString());
            _fs.Directory.CreateDirectory(guildDir);

            // Choose a unique name for this file based on the parameters
            var nonce = 0ul;
            string fileName;
            do
            {
                var hashName = $"{guild}{title}{duration}{url ?? "none"}{thumbnailUrl ?? "none"}{nonce++}".SHA256();
                fileName = Path.Combine(guildDir, hashName + ".wav");
            } while (_fs.File.Exists(_fs.Path.Combine(guildDir, fileName)));

            // Write to disk
            await using (var fs = _fs.File.Create(_fs.Path.Combine(guildDir, fileName)))
                await audio.CopyToAsync(fs);

            // Insert into the database
            await using var cmd = _database.CreateCommand();
            cmd.CommandText = InsertTrackSql;
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@Title", System.Data.DbType.String) { Value = title.ToLowerInvariant() });
            cmd.Parameters.Add(new SQLiteParameter("@FileName", System.Data.DbType.String) { Value = fileName });
            cmd.Parameters.Add(new SQLiteParameter("@Url", System.Data.DbType.String) { Value = url ?? "" });
            cmd.Parameters.Add(new SQLiteParameter("@ThumbnailUrl", System.Data.DbType.String) { Value = thumbnailUrl ?? "" });
            cmd.Parameters.Add(new SQLiteParameter("@Duration", System.Data.DbType.String) { Value = ((uint)duration.TotalSeconds).ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@Owner", System.Data.DbType.String) { Value = owner.ToString() });

            var id = (ulong)(long)(await cmd.ExecuteScalarAsync() ?? 0);

            // Return track object
            return new DatabaseMusicTrack(
                guild,
                id,
                _fs.Path.Combine(guildDir, fileName),
                title,
                url,
                thumbnailUrl,
                duration
            );
        }

        public async Task<IAsyncEnumerable<ITrack>> Get(ulong guild, ulong? id = null, string? titleSearch = null, string? url = null, int? limit = null, TrackOrder? order = null)
        {
            static ITrack ParseTransaction(DbDataReader reader)
            {
                return new DatabaseMusicTrack(
                    ulong.Parse((string)reader["GuildId"]),
                    unchecked((ulong)(long)reader["Id"]),
                    (string)reader["FileName"],
                    (string)reader["Title"],
                    (string)reader["Url"],
                    (string)reader["ThumbnailUrl"],
                    TimeSpan.FromSeconds(uint.Parse((string)reader["Duration"]))
                );
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var sql = order switch {
                    TrackOrder.Random => FindTrackSql.Replace("{{order}}", "random()"),
                    null => FindTrackSql.Replace("{{order}}", "rowid"),
                    TrackOrder.Id => FindTrackSql.Replace("{{order}}", "rowid"),
                    _ => FindTrackSql.Replace("{{order}}", "rowid")
                };

                var cmd = db.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@Id", System.Data.DbType.String) { Value = id?.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@Url", System.Data.DbType.String) { Value = url });
                cmd.Parameters.Add(new SQLiteParameter("@Title", System.Data.DbType.String) { Value = titleSearch });
                cmd.Parameters.Add(new SQLiteParameter("@Limit", System.Data.DbType.Int64) { Value = limit ?? int.MaxValue });
                return cmd;
            }

            return new SqlAsyncResult<ITrack>(_database, PrepareQuery, ParseTransaction);
        }

        private class DatabaseMusicTrack
            : ITrack
        {
            public DatabaseMusicTrack(ulong guild, ulong id, string path, string title, string? url, string? thumbnailUrl, TimeSpan duration)
            {
                Guild = guild;
                ID = id;
                Path = path;
                Title = title;
                Url = url;
                ThumbnailUrl = thumbnailUrl;
                Duration = duration;
            }

            public ulong Guild { get; }
            public ulong ID { get; }
            public string Path { get; }
            public string Title { get; }
            public string? Url { get; }
            public string? ThumbnailUrl { get; }
            public TimeSpan Duration { get; }
        }
    }
}
