using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Words;
using TensorFlow;

namespace Mute.Moe.Services.Sentiment
{
    public class TensorflowSentiment
        : ISentimentEvaluator
    {
        private readonly IWords _wordVectors;

        private readonly string _sentimentModelPath;
        private readonly string _sentimentModelInput;
        private readonly string _sentimentModelOutput;

        private readonly Task<TFGraph> _graph;

        public TensorflowSentiment(Configuration config, IWords wordVectors)
        {
            _wordVectors = wordVectors ?? throw new ArgumentNullException(nameof(wordVectors));

            var cfg = config.Sentiment ?? throw new ArgumentNullException(nameof(config.Sentiment));
            _sentimentModelPath = cfg.SentimentModelPath ?? throw new ArgumentNullException(nameof(config.Sentiment.SentimentModelPath));
            _sentimentModelInput = cfg.SentimentModelInputLayer ?? throw new ArgumentNullException(nameof(config.Sentiment.SentimentModelInputLayer));
            _sentimentModelOutput = cfg.SentimentModelOutputLayer ?? throw new ArgumentNullException(nameof(config.Sentiment.SentimentModelOutputLayer));

            _graph = Task.Run(async () => await LoadGraph());
        }

        public async Task<SentimentResult> Predict(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return new SentimentResult(message, 0, 0, 1, TimeSpan.Zero);

            try
            {
                var w = new Stopwatch();
                w.Start();

                //todo: clean message more!
                var words = message.SplitSpan(' ').Select(a => a.ToString()).ToArray();

                var graph = await _graph;

                using var session = new TFSession(graph);
                var runner = session.GetRunner();

                //Create input tensor (1 sentence, N words, 300 word vector dimensions)
                var input = new float[1, words.Length, 300];

                var tasks = words.Select(_wordVectors.Vector).ToList();

                //Copy in word vectors element by element
                var wordIndex = 0;
                foreach (var wordVector in tasks)
                {
                    var wv = await wordVector;
                    if (wv != null)
                        for (var i = 0; i < 300; i++)
                            input[0, wordIndex, i] = wv[i];

                    wordIndex++;
                }

                //Set tensor as input to the graph
                runner.AddInput(graph[_sentimentModelInput][0], input);

                //Tell the runner what result we want
                runner.Fetch(graph[_sentimentModelOutput][0]);

                //Execute the graph
                var results = runner.Run();

                //Fetch the result (we only asked for one)
                var result = (float[,])results.Single().GetValue();

                return new SentimentResult(message, result[0, 0], result[0, 1], result[0, 2], w.Elapsed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<TFGraph> LoadGraph()
        {
            var graph = new TFGraph();
            graph.Import(await File.ReadAllBytesAsync(_sentimentModelPath));
            return graph;
        }
    }
}
