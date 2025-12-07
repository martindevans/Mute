using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Database;

/// <summary>
/// Main SQL database services
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// The current open DB connection
    /// </summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Create a database command
    /// </summary>
    /// <returns></returns>
    DbCommand CreateCommand();
}

public static class IDatabaseServiceExtensions
{
    public static int Exec(this IDatabaseService db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }
}

public class SqlAsyncResult<TItem>
    : IAsyncEnumerable<TItem>
{
    private readonly IDatabaseService _database;
    private readonly Func<IDatabaseService, DbCommand> _prepare;
    private readonly Func<DbDataReader, TItem> _read;

    protected internal SqlAsyncResult(IDatabaseService database, Func<IDatabaseService, DbCommand> prepare, Func<DbDataReader, TItem> read)
    {
        _database = database;
        _prepare = prepare;
        _read = read;
    }

        
    IAsyncEnumerator<TItem> IAsyncEnumerable<TItem>.GetAsyncEnumerator(CancellationToken ct)
    {
        return new AsyncEnumerator(_prepare(_database), _read, ct);
    }

    private class AsyncEnumerator
        : IAsyncEnumerator<TItem>
    {
        private readonly DbCommand _query;
        private readonly Func<DbDataReader, TItem> _read;
        private readonly CancellationToken _cancellation;

        private DbDataReader? _reader;

        public AsyncEnumerator(DbCommand query, Func<DbDataReader, TItem> read, CancellationToken cancellation)
        {
            _query = query;
            _read = read;
            _cancellation = cancellation;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            _reader ??= await _query.ExecuteReaderAsync(_cancellation);

            return await _reader.ReadAsync(_cancellation);
        }

        public TItem Current => _reader == null ? throw new InvalidOperationException("Called `Current` before `MoveNextAsync` or after `Dispose`") : _read(_reader);

        public ValueTask DisposeAsync()
        {
            _query.Dispose();

            _reader?.Dispose();
            _reader = null;

            return new ValueTask(Task.CompletedTask);
        }
    }
}