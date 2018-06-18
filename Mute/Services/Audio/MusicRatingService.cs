using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services.Audio
{
    public class MusicRatingService
    {
        #region sql
        private const string InsertRating = "REPLACE into Music_Ratings (TrackId, UserId, Rating) values(@TrackId, @UserId, @Rating)";

        private const string GetRatingsByUserId = "SELECT * FROM Music_Ratings WHERE UserId == @UserId";
        private const string GetRatingsByTrackId = "SELECT * FROM Music_Ratings WHERE TrackId == @TrackId";
        private const string GetTopRatedTracks = "SELECT TrackId, Sum(Rating) as Rating FROM Music_Ratings GROUP BY TrackId";
        #endregion

        private readonly DatabaseService _database;

        public MusicRatingService(DatabaseService database)
        {
            _database = database;

            _database.Exec("CREATE TABLE IF NOT EXISTS `Music_Ratings` (`TrackId` TEXT NOT NULL, `UserId` TEXT NOT NULL, `Rating` INTEGER NOT NULL, PRIMARY KEY(`TrackId`, `UserId`))");
            _database.Exec("CREATE INDEX IF NOT EXISTS `MusicRatingsByTrackId` ON `Music_Ratings` (`TrackId` ASC)");
            _database.Exec("CREATE INDEX IF NOT EXISTS `MusicRatingsByUserId` ON `Music_Ratings` (`UserId` ASC)");
        }

        [NotNull] public async Task Record([NotNull] string trackId, ulong userId, int score)
        {
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertRating;
                    cmd.Parameters.Add(new SQLiteParameter("@TrackId", System.Data.DbType.String) {Value = trackId});
                    cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = userId.ToString()});
                    cmd.Parameters.Add(new SQLiteParameter("@Rating", System.Data.DbType.Int32) {Value = score});

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [NotNull, ItemNotNull] public async Task<IReadOnlyList<(string, int)>> GetAggregateTrackRatings()
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = GetTopRatedTracks;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var results = new List<(string, int)>();

                    while (await reader.ReadAsync())
                    {
                        results.Add((
                            reader["TrackId"].ToString(),
                            int.Parse(reader["Rating"].ToString())
                        ));
                    }

                    return results;
                }
            }
        }

        [NotNull, ItemNotNull] public async Task<IReadOnlyList<(ulong, byte)>> GetTrackRatings(string trackId)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = GetRatingsByTrackId;
                cmd.Parameters.Add(new SQLiteParameter("@TrackId", System.Data.DbType.String) {Value = trackId});

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var results = new List<(ulong, byte)>();

                    while (await reader.ReadAsync())
                    {
                        results.Add((
                            ulong.Parse(reader["UserId"].ToString()),
                            byte.Parse(reader["Rating"].ToString())
                        ));
                    }

                    return results;
                }
            }
        }

        [NotNull, ItemNotNull] public async Task<IReadOnlyList<(string, byte)>> GetUserRatings(ulong userId)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = GetRatingsByUserId;
                cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = userId.ToString()});

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var results = new List<(string, byte)>();

                    while (await reader.ReadAsync())
                    {
                        results.Add((
                            reader["TrackId"].ToString(),
                            byte.Parse(reader["Rating"].ToString())
                        ));
                    }

                    return results;
                }
            }
        }
    }
}
