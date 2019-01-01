using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TensorFlow;

namespace Mute.Services
{
    public class SentimentService
    {
        private readonly IDatabaseService _database;
        private readonly SentimentConfig _config;

        private const string InsertTaggedSentimentData = "INSERT INTO TaggedSentimentData (Content, Score) values(@Content, @Score)";
        private const string SelectTaggedSentimentData = "SELECT * FROM TaggedSentimentData";

        public SentimentService([NotNull] Configuration config, IDatabaseService database)
        {
            _database = database;
            _config = config.Sentiment;

            // Create database structure
            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `TaggedSentimentData` (`Content` TEXT NOT NULL UNIQUE, `Score` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<SentimentResult> Predict([NotNull] string message)
        {
            //await LoadModel();

            return new SentimentResult {
                Classification = Sentiment.Neutral,
                Score = 0,
                Text = message,
                PositiveScore = 0,
                NegativeScore = 0,
                NeutralScore = 0
            };
        }

        public async Task Teach([NotNull] string text, Sentiment sentiment)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertTaggedSentimentData;
                cmd.Parameters.Add(new SQLiteParameter("@Content", System.Data.DbType.String) { Value = text.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@Score", System.Data.DbType.String) { Value = ((int)sentiment).ToString() });
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Sentiment learned: `{text}` == {sentiment}");
        }

        #region helper classes
        public enum Sentiment
        {
            Negative = 0,
            Positive = 1,
            Neutral = 2
        }

        public struct SentimentResult
        {
            public string Text;

            public float Score;
            public Sentiment Classification;

            public float PositiveScore;
            public float NeutralScore;
            public float NegativeScore;
        }
        #endregion

        private async Task<object> LoadModel()
        {
            using (var graph = new TFGraph())
            {
                graph.Import(await File.ReadAllBytesAsync(@"C:\Users\Martin\Documents\tensorflow\keras_to_tensorflow\converted.pb"));
                var session = new TFSession(graph);

                //graph.

                var runner = session.GetRunner();
                runner.AddInput(graph["gaussian_noise_1_input"][0], new TFTensor(TFDataType.Float, new long[] { 1, 1, 300 }, 300 * sizeof(float)));
                runner.Fetch(graph["dense_3/Softmax"][0]);

                try
                {
                    var result = runner.Run();

                    foreach (var item in result)
                    {
                    }
                }
                catch (Exception e)
                {

                }

                return null;
            }
        }
    }
}
