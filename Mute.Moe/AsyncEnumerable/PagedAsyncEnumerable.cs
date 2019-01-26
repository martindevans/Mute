using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.AsyncEnumerable
{
    public class PagedAsyncEnumerable<TPage, TItem>
        : IAsyncEnumerable<TItem>
        where TPage : class
    {
        private readonly Func<TPage, Task<TPage>> _nextPage;
        private readonly Func<TPage, IEnumerator<TItem>> _items;
        private readonly uint _pageLimit;

        public PagedAsyncEnumerable(Func<TPage, Task<TPage>> nextPage, Func<TPage, IEnumerator<TItem>> items, uint pageLimit = uint.MaxValue)
        {
            _nextPage = nextPage;
            _items = items;
            _pageLimit = pageLimit;
        }

        [NotNull] public IAsyncEnumerator<TItem> GetEnumerator()
        {
            return new Enumerator(_nextPage, _items, _pageLimit);
        }

        private class Enumerator
            : IAsyncEnumerator<TItem>
        {
            private readonly Func<TPage, Task<TPage>> _nextPage;
            private readonly Func<TPage, IEnumerator<TItem>> _items;
            private readonly uint _pageLimit;

            private uint _pagesFetched;
            private TPage _currentPage;
            private IEnumerator<TItem> _currentPageItems;

            public Enumerator(Func<TPage, Task<TPage>> nextPage, Func<TPage, IEnumerator<TItem>> items, uint pageLimit)
            {
                _nextPage = nextPage;
                _items = items;
                _pageLimit = pageLimit;
            }

            public void Dispose()
            {
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                //Iterate through current page
                if (_currentPageItems != null && _currentPageItems.MoveNext())
                {
                    return true;
                }
                else
                {
                    //Make sure we don't fetch more pages than the limit
                    if (_pagesFetched == _pageLimit)
                        return false;

                    //Reached the end of previous page, get the next page
                    _currentPage = await _nextPage(_currentPage);
                    _pagesFetched++;
                    if (_currentPage == null)
                        return false;

                    //Begin enumerating items in this page
                    _currentPageItems = _items(_currentPage);
                    return _currentPageItems.MoveNext();
                }
            }

            public TItem Current => _currentPageItems.Current;
        }
    }
}
