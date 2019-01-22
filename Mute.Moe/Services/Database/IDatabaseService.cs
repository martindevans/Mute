using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Database
{
    public interface IDatabaseService
    {
        DbCommand CreateCommand();
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

        [NotNull]
        IAsyncEnumerator<TItem> IAsyncEnumerable<TItem>.GetEnumerator()
        {
            return new AsyncEnumerator(_prepare(_database), _read);
        }

        private class AsyncEnumerator
            : IAsyncEnumerator<TItem>, IDisposable
        {
            private readonly DbCommand _query;
            private readonly Func<DbDataReader, TItem> _read;

            private DbDataReader _reader;

            public AsyncEnumerator(DbCommand query, Func<DbDataReader, TItem> read)
            {
                _query = query;
                _read = read;
            }

            public void Dispose()
            {
                _query.Dispose();

                _reader?.Dispose();
                _reader = null;
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (_reader == null)
                    _reader = await _query.ExecuteReaderAsync(cancellationToken);

                return await _reader.ReadAsync(cancellationToken);
            }

            public TItem Current => _read(_reader);
        }
    }
}
