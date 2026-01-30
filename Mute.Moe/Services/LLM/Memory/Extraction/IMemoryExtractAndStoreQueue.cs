using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Host;
using Serilog;

namespace Mute.Moe.Services.LLM.Memory.Extraction;

/// <summary>
/// Store a queue of transcripts waiting to be processed for memory extraction
/// </summary>
public interface IMemoryExtractAndStoreQueue
{
    /// <summary>
    /// Enqueue a transcript for later processing
    /// </summary>
    /// <param name="context"></param>
    /// <param name="transcript"></param>
    public Task Enqueue(ulong context, string transcript);
}

/// <summary>
/// Stores the extraction queue in the database
/// </summary>
public class DatabaseMemoryExtractAndStoreQueue
    : IMemoryExtractAndStoreQueue, IHostedService
{
    private static readonly ILogger Logger = Log.Logger.ForContext<DatabaseMemoryExtractAndStoreQueue>();

    private readonly IDatabaseService _database;
    private readonly FactExtractionService _extraction;
    private readonly IAgentMemoryStorage _storage;

    private readonly AutoResetEvent _event = new(true);
    private CancellationTokenSource _cancellation = new();
    private Task? _worker;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="database"></param>
    /// <param name="extraction"></param>
    /// <param name="storage"></param>
    public DatabaseMemoryExtractAndStoreQueue(IDatabaseService database, FactExtractionService extraction, IAgentMemoryStorage storage)
    {
        _database = database;
        _extraction = extraction;
        _storage = storage;

        _database.Exec("""
                       CREATE TABLE IF NOT EXISTS `MemoryExtractionJobs`
                       (
                           `ID` INTEGER PRIMARY KEY ASC,
                           `Transcript` TEXT NOT NULL,
                           `Context` INTEGER NOT NULL
                       );
                       """);
    }

    /// <inheritdoc />
    public async Task Enqueue(ulong context, string transcript)
    {
        // Store the work in the database
        await _database.Connection.InsertAsync(new MemoryExtractionJob()
        {
            Context = context,
            Transcript = transcript
        });

        // Signal the worker that there's something to do
        _event.Set();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Link the source, so any of them being cancelled will cancel this one
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token);

        // Start the endless worker loop
        _worker = Task.Run(Worker, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _event.Set();
        await _cancellation.CancelAsync();
    }

    private async Task Worker()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            while (!_cancellation.IsCancellationRequested)
            {
                // Keep taking work from the database and processing it
                var item = await _database.Connection.QuerySingleOrDefaultAsync<MemoryExtractionJob>("SELECT * FROM MemoryExtractionJobs LIMIT 1;");

                // Return to waiting as soon as the DB queue is empty
                if (item == null)
                    break;

                // Do the actual extraction and processing, storing extracted data to long term agent memory
                await ProcessJobItem(item);

                // Cooldown between extractions
                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            // Wait until an event happens
            await _event.WaitOneAsync(TimeSpan.FromHours(1));
        }
    }

    private async Task ProcessJobItem(MemoryExtractionJob job)
    {
        // We want to introduce a little bit of noise into the memory initialisation, otherwise we get clusters of memories
        var rng = new Random(HashCode.Combine(job.Context, job.Transcript, job.ID));

        // Call out to LLM to extract from the transcript
        var facts = await _extraction.Extract(job.Transcript, _cancellation.Token);

        // Add facts and delete work item in one transaction
        using (var tsx = _database.Connection.BeginTransaction())
        {
            // Store the transcript as evidence
            var evidence = await _storage.CreateEvidence(job.Context, job.Transcript, tsx);

            // Store facts, each linked to the transcript
            foreach (var fact in facts)
            {
                // Create memory
                var memId = await _storage.CreateMemory(job.Context, fact.Text, confidenceLogit: rng.NextSingle(0.99f, 1.01f), tsx);

                // Create evidence link
                if (memId.HasValue)
                    await _storage.CreateEvidenceLink(evidence, memId.Value);
            }

            // Delete job
            await _database.Connection.DeleteAsync(job, tsx);

            tsx.Commit();
        }

        Logger.Information("Extracted and stored {0} new memory items in context {1}", facts.Count, job.Context);
    }

    private class MemoryExtractionJob
    {
        /// <summary>
        /// The unique ID of this memory
        /// </summary>
        public int ID { get; init; }

        /// <summary>
        /// The raw transcript
        /// </summary>
        public required string Transcript { get; init; }

        /// <summary>
        /// The context to store all extracted facts under
        /// </summary>
        public required ulong Context { get; init; }
    }
}