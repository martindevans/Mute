using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.AsyncEnumerable
{
    public class EmptyAsyncEnumerable<T>
        : IAsyncEnumerable<T>
    {
        [NotNull]
        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new Enumerator();
        }

        private class Enumerator
            : IAsyncEnumerator<T>
        {
            public void Dispose()
            {
            }

            [NotNull]
            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                return Task.FromResult(false);
            }

            [CanBeNull]
            public T Current => default(T);
        }
    }
}
