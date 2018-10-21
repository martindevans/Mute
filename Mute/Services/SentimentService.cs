using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ML;
using Microsoft.ML.Models;
using Microsoft.ML.Runtime;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using TextLoader = Microsoft.ML.Data.TextLoader;

namespace Mute.Services
{
    public class SentimentService
    {
        private readonly DatabaseService _database;
        private readonly MlConfig _config;

        private readonly string _modelPath;
        private readonly string _trainingDataDirectory;
        private readonly string _evalDataDirectory;

        private Task<PredictionModel<SentimentData, SentimentPrediction>> _model;

        private const string InsertTaggedSentimentData = "INSERT INTO TaggedSentimentData (Content, Score) values(@Content, @Score)";
        private const string SelectTaggedSentimentData = "SELECT * FROM TaggedSentimentData";

        public SentimentService([NotNull] Configuration config, DatabaseService database)
        {
            _database = database;
            _config = config.MlConfig;

            _modelPath = Path.Combine(_config.BaseModelPath, _config.Sentiment.ModelDirectory, "model.m");
            _trainingDataDirectory = Path.Combine(_config.BaseDatasetsPath, _config.Sentiment.TrainingDatasetDirectory);
            _evalDataDirectory = Path.Combine(_config.BaseDatasetsPath, _config.Sentiment.EvalDatasetDirectory);

            // Create database structure
            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `TaggedSentimentData` (`Content` TEXT NOT NULL UNIQUE, `Score` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _model = Task.Run(GetOrCreateModel);
        }

        private async Task<PredictionModel<SentimentData, SentimentPrediction>> GetOrCreateModel()
        {
            try
            {
                //Check if the model already exists on disk. If not then train it
                if (!File.Exists(_modelPath))
                {
                    var model = await Train();
                    await model.WriteAsync(_modelPath);
                    Console.WriteLine("Trained sentiment model. Accuracy:" + EvaluateModel(model).AccuracyMicro);
                }
                else
                {
                    Console.WriteLine("Loaded sentiment model");
                }
                
                return await PredictionModel.ReadAsync<SentimentData, SentimentPrediction>(_modelPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Sentiment training/loading failed: " + e.Message);
                if (e.InnerException != null)
                    Console.WriteLine(e.InnerException.Message);

                throw;
            }
        }

        private async Task<PredictionModel<SentimentData, SentimentPrediction>> Train()
        {
            //A temp file to put all training data into
            var trainingDataTempFileName = Path.Combine(_config.TempTrainingCache, Guid.NewGuid().ToString());

            try
            {
                //Create a single file with all training data
                using (var trainingData = new StreamWriter(File.OpenWrite(trainingDataTempFileName)))
                {
                    //Get training data from files
                    foreach (var file in Directory.EnumerateFiles(_trainingDataDirectory))
                    foreach (var line in File.ReadAllLines(file))
                        trainingData.WriteLine(line);

                    //Get training data from database
                    try
                    {
                        using (var cmd = _database.CreateCommand())
                        {
                            cmd.CommandText = SelectTaggedSentimentData;
                            var reader = await cmd.ExecuteReaderAsync();
                            while (reader.Read())
                            {
                                var content = (string)reader["Content"];
                                var score = int.Parse((string)reader["Score"]);

                                trainingData.WriteLine($"{content}\t{score}");
                            }
                        }
                    }
                    catch (SQLiteException e)
                    {
                        Console.WriteLine(e);
                    }
                }

                //Create a training pipeline
                var pipeline = new LearningPipeline {
                    new TextLoader(trainingDataTempFileName).CreateFrom<SentimentData>(),
                    new TextFeaturizer("Features", "SentimentText") {
                        KeepDiacritics = false,
                        KeepPunctuations = false,
                        TextCase = TextNormalizerTransformCaseNormalizationMode.Lower,
                        OutputTokens = true,
                        StopWordsRemover = new PredefinedStopWordsRemover(),
                        VectorNormalizer = TextTransformTextNormKind.L2,
                        CharFeatureExtractor = new NGramNgramExtractor {NgramLength = 3, AllLengths = false},
                        WordFeatureExtractor = new NGramNgramExtractor {NgramLength = 3, AllLengths = true}
                    },
                    //new AveragedPerceptronBinaryClassifier(),
                    new StochasticDualCoordinateAscentClassifier(),
                    //new PredictedLabelColumnOriginalValueConverter { PredictedLabelColumn = "PredictedLabel" }
                };


                //Train the model
                var model = pipeline.Train<SentimentData, SentimentPrediction>();

                Console.WriteLine("Training complete");
                return model;
            }
            catch (Exception e)
            {
                Console.WriteLine("Training failed: " + e);
                throw;
            }
            finally
            {
                //Delete the temp file
                if (File.Exists(trainingDataTempFileName))
                    File.Delete(trainingDataTempFileName);
            }
        }

        public async Task ForceRetrain()
        {
            _model = Task.Run(async () => {
                var model = await Train();
                await model.WriteAsync(_modelPath);
                return model;
            });

            await _model;
        }

        private ClassificationMetrics EvaluateModel(PredictionModel model)
        {
            var evalDataTemp = Path.Combine(_config.TempTrainingCache, Guid.NewGuid().ToString());

            try
            {
                //Get all the evaluation files and concat into one big file of training data
                using (var evalData = new StreamWriter(File.OpenWrite(evalDataTemp)))
                    foreach (var file in Directory.EnumerateFiles(_evalDataDirectory))
                    foreach (var line in File.ReadAllLines(file))
                        evalData.WriteLine(line);

                //Evaluate the model
                var testData = new TextLoader(evalDataTemp).CreateFrom<SentimentData>();
                var evaluator = new ClassificationEvaluator();
                Console.WriteLine("Beginning sentiment model evaluation");
                var metrics = evaluator.Evaluate(model, testData);

                return metrics;
            }
            finally
            {
                //Delete temp file
                File.Delete(evalDataTemp);
            }
        }

        public async Task<ClassificationMetrics> EvaluateModelMetrics()
        {
            return EvaluateModel(await _model);
        }

        public async Task<SentimentResult> Predict([NotNull] string message)
        {
            var model = await _model;
            var result = model.Predict(new SentimentData { SentimentText = message });

            var pos = result.Score[(int)Sentiment.Positive];
            var neg = result.Score[(int)Sentiment.Negative];
            var neut = result.Score[(int)Sentiment.Neutral];

            var max = Math.Max(pos, Math.Max(neut, neg));

            var largestScore = float.MinValue;
            var largestIndex = -1;
            for (var i = 0; i < 3; i++)
            {
                var score = result.Score[i];
                if (score > largestScore)
                {
                    largestScore = score;
                    largestIndex = i;
                }
            }

            return new SentimentResult {
                Classification = (Sentiment)largestIndex,
                Score = max,
                Text = message,
                PositiveScore = pos,
                NegativeScore = neg,
                NeutralScore = neut
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

        private class SentimentData
        {
            [UsedImplicitly, Column(ordinal: "1")]
            public float Label;

            [UsedImplicitly, Column(ordinal: "0")]
            public string SentimentText;
        }

        private class SentimentPrediction
        {
            [UsedImplicitly, ColumnName("Score")]
            public float[] Score;
        }
        #endregion
    }
}
