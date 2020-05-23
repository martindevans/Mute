using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Mute.Moe.Services.Words;
using TensorFlow;

namespace Mute.Moe.Services.Sentiment
{
    public class TensorflowSentiment
        : ISentimentEvaluator
    {
        private readonly IWords _wordVectors;
        private readonly SentimentConfig _config;

        private readonly Task<TFGraph> _graph;

        public TensorflowSentiment( Configuration config, IWords wordVectors)
        {
            _wordVectors = wordVectors;
            _config = config.Sentiment;
            _graph = Task.Run(async () => await LoadGraph());
        }

        public async Task<SentimentResult> Predict( string message)
        {
            try
            {
                var w = new Stopwatch();
                w.Start();

                //todo: clean message more!
                var words = message.Split(' ');

                var graph = await _graph;

                using var session = new TFSession(graph);
                var runner = session.GetRunner();

                //Create input tensor (1 sentence, N words, 300 word vector dimensions)
                var input = new float[1, words.Length, 300];

                var tasks = words.Select(_wordVectors.Vector).ToArray();
                await Task.WhenAll(tasks);

                //Copy in word vectors element by element
                var wordIndex = 0;
                foreach (var wordVector in tasks)
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<TFGraph> LoadGraph()
        {
            var graph = new TFGraph();
            graph.Import(await File.ReadAllBytesAsync(_config.SentimentModelPath));
            return graph;
        }
    }
}
