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

/// <summary>
/// Extensions for <see cref="IDatabaseService"/>
/// </summary>
public static class IDatabaseServiceExtensions
{
    /// <summary>
    /// Immediately execute some non-query SQL and return the count
    /// </summary>
    /// <param name="db"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static int Exec(this IDatabaseService db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }
}

/// <summary>
/// Provides results from an SQL query as an <see cref="IAsyncEnumerable{TItem}"/>
/// </summary>
/// <typeparam name="TItem"></typeparam>
public class SqlAsyncResult<TItem>
    : IAsyncEnumerable<TItem>
{
    private readonly IDatabaseService _database;
    private readonly Func<IDatabaseService, DbCommand> _prepare;
    private readonly Func<DbDataReader, TItem> _read;

    /// <summary>
    /// Create a new <see cref="SqlAsyncResult{TItem}"/>
    /// </summary>
    /// <param name="database">Database to query from</param>
    /// <param name="prepare">Prepare a <see cref="DbCommand"/> which will provide results</param>
    /// <param name="read">Read a single item from a <see cref="DbDataReader"/></param>
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

    private class AsyncEnumerator(DbCommand _query, Func<DbDataReader, TItem> _read, CancellationToken _cancellation)
        : IAsyncEnumerator<TItem>
    {
        private DbDataReader? _reader;

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