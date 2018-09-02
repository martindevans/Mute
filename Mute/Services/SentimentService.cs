using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.ML;
using Microsoft.ML.Models;
using Microsoft.ML.Runtime;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Runtime.Data;
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
                    Console.WriteLine("Trained sentiment model. Accuracy:" + EvaluateModel(model).Accuracy);
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
            try
            {
                //Get all the training files and concat into one big file of training data
                var trainingDataTempFileName = Path.Combine(_config.TempTrainingCache, Guid.NewGuid().ToString());
                using (var trainingData = new StreamWriter(File.OpenWrite(trainingDataTempFileName)))
                {
                    foreach (var file in Directory.EnumerateFiles(_trainingDataDirectory))
                    {
                        foreach (var line in File.ReadAllLines(file))
                            trainingData.WriteLine(line);
                    }

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
                                var score = bool.Parse((string)reader["Score"]) ? 1 : 0;

                                trainingData.WriteLine($"{content}\t{score}");
                            }

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (SQLiteException e)
                    {
                        Console.WriteLine(e);
                    }
                }

                //Create a training pipeline
                var pipeline = new LearningPipeline();
                pipeline.Add(new TextLoader(trainingDataTempFileName).CreateFrom<SentimentData>());
                pipeline.Add(new TextFeaturizer("Features", "SentimentText") {
                    KeepDiacritics = false,
                    KeepPunctuations = false,
                    TextCase = TextNormalizerTransformCaseNormalizationMode.Lower,
                    OutputTokens = true,
                    StopWordsRemover = new PredefinedStopWordsRemover(),
                    VectorNormalizer = TextTransformTextNormKind.L2,
                    CharFeatureExtractor = new NGramNgramExtractor {NgramLength = 3, AllLengths = false},
                    WordFeatureExtractor = new NGramNgramExtractor {NgramLength = 3, AllLengths = true}
                });
                pipeline.Add(new AveragedPerceptronBinaryClassifier());
                pipeline.Add(new PredictedLabelColumnOriginalValueConverter {PredictedLabelColumn = "PredictedLabel"});

                //Train the model
                var model = pipeline.Train<SentimentData, SentimentPrediction>();

                //Delete the concatenated training data file
                File.Delete(trainingDataTempFileName);

                return model;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
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

        private BinaryClassificationMetrics EvaluateModel(PredictionModel model)
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
                var evaluator = new BinaryClassificationEvaluator();
                var metrics = evaluator.Evaluate(model, testData);

                return metrics;
            }
            finally
            {
                //Delete temp file
                File.Delete(evalDataTemp);
            }
        }

        public async Task<BinaryClassificationMetrics> EvaluateModelMetrics()
        {
            return EvaluateModel(await _model);
        }

        public async Task<float> Sentiment([NotNull] string message)
        {
            var model = await _model;
            var result = model.Predict(new SentimentData { SentimentText = message });
            return result.Probability;
        }

        public async Task Teach([NotNull] string text, bool sentiment)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertTaggedSentimentData;
                cmd.Parameters.Add(new SQLiteParameter("@Content", System.Data.DbType.String) { Value = text.ToLowerInvariant() });
                cmd.Parameters.Add(new SQLiteParameter("@Score", System.Data.DbType.String) { Value = sentiment.ToString() });
                await cmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"Sentiment learned: `{text}` == {sentiment}");
        }
        
        #region helper classes
        private class SentimentData
        {
            [UsedImplicitly, Column(ordinal: "1", name: "Label")]
            public float Sentiment;
            [UsedImplicitly, Column(ordinal: "0")]
            public string SentimentText;
        }

        private class SentimentPrediction
        {
            [UsedImplicitly, ColumnName("PredictedLabel")]
            public DvBool Sentiment;

            [UsedImplicitly, ColumnName("Probability")]
            public float Probability;
        }
        #endregion
    }
}
