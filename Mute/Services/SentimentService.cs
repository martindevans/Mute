using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using TensorFlow;

namespace Mute.Services
{
    public class SentimentService
    {
        private readonly WordVectorsService _wordVectors;
        private readonly SentimentConfig _config;

        private readonly Task<TFGraph> _graph;

        public SentimentService([NotNull] Configuration config, WordVectorsService wordVectors)
        {
            _wordVectors = wordVectors;
            _config = config.Sentiment;
            _graph = Task.Run(async () => await LoadGraph());
        }

        public async Task<SentimentResult> Predict([NotNull] string message)
        {
            try
            {
                var w = new Stopwatch();
                w.Start();

                //todo: clean message more!
                var words = message.Split(' ');

                var graph = await _graph;

                using (var session = new TFSession(graph))
                {
                    var runner = session.GetRunner();

                    //Create input tensor (1 sentence, N words, 300 word vector dimensions)
                    var input = new float[1, words.Length, 300];

                    //Copy in word vectors element by element
                    var wordIndex = 0;
                    foreach (var wordVector in words.AsParallel().AsOrdered().Select(_wordVectors.GetVector))
                    {
                        for (var i = 0; i < 300; i++)
                        {
                            var wv = await wordVector;
                            if (wv != null)
                                input[0, wordIndex, i] = wv[i];
                        }

                        wordIndex++;
                    }

                    //Set tensor as input to the graph
                    runner.AddInput(graph[_config.SentimentModelInputLayer][0], input);

                    //Tell the runner what result we want
                    runner.Fetch(graph[_config.SentimentModelOutputLayer][0]);

                    //Execute the graph
                    var results = runner.Run();

                    //Fetch the result (we only asked for one)
                    var result = (float[,])results.Single().GetValue();

                    return new SentimentResult(message, result[0, 0], result[0, 1], result[0, 2], w.Elapsed);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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
            public readonly string Text;

            public readonly float ClassificationScore;
            public readonly Sentiment Classification;

            public readonly float NegativeScore;
            public readonly float PositiveScore;
            public readonly float NeutralScore;

            public readonly TimeSpan ClassificationTime;

            public SentimentResult(string message, float negative, float positive, float neutral, TimeSpan time)
            {
                Text = message;
                NegativeScore = negative;
                PositiveScore = positive;
                NeutralScore = neutral;
                ClassificationTime = time;

                ClassificationScore = Math.Max(negative, Math.Max(positive, neutral));

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (ClassificationScore == negative)
                    Classification = Sentiment.Negative;
                else if (ClassificationScore == positive)
                    Classification = Sentiment.Positive;
                else
                    Classification = Sentiment.Neutral;
                // ReSharper restore CompareOfFloatsByEqualityOperator
            }
        }
        #endregion

        [ItemNotNull]
        private async Task<TFGraph> LoadGraph()
        {
            var graph = new TFGraph();
            graph.Import(await File.ReadAllBytesAsync(_config.SentimentModelPath));
            return graph;
        }
    }
}
