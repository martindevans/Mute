using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.AsyncEnumerable
{
    public class AsyncSelectEnumerable<TIn, TOut>
        : IAsyncEnumerable<TOut>
    {
        private readonly IAsyncEnumerable<TIn> _input;
        private readonly Func<TIn, Task<TOut>> _transform;

        public AsyncSelectEnumerable(IAsyncEnumerable<TIn> input, Func<TIn, Task<TOut>> transform)
        {
            _input = input;
            _transform = transform;
        }

        public IAsyncEnumerator<TOut> GetEnumerator()
        {
            return new Enumerator(_input.GetEnumerator(), _transform);
        }

        private class Enumerator
            : IAsyncEnumerator<TOut>
        {
            private readonly IAsyncEnumerator<TIn> _input;
            private readonly Func<TIn, Task<TOut>> _transform;

            public Enumerator(IAsyncEnumerator<TIn> input, Func<TIn, Task<TOut>> transform)
            {
                _input = input;
                _transform = transform;
            }

            public TOut Current { get; private set; }

            public void Dispose()
            {
                Current = default;
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                Current = default;

                if (!await _input.MoveNext())
                    return false;

                Current = await _transform(_input.Current);
                return true;
            }
        }
    }
}
