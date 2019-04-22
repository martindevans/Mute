using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.AsyncEnumerable;

namespace Mute.Tests.AsyncEnumerable
{
    [TestClass]
    public class PagedAsyncEnumerableTests
    {
        [TestMethod]
        public async Task ReturnsBatchedResults()
        {
            async Task<Page> GetPage(Page previous)
            {
                await Task.CompletedTask;

                if (previous == null)
                    return new Page { Index = 0 };
                else if (previous.Index == 0)
                    return new Page { Index = 1 };
                else
                    return null;
            }

            IEnumerator<string> GetItems(Page current)
            {
                if (current.Index == 0)
                    return new List<string> { "a", "b" }.GetEnumerator();
                else if (current.Index == 1)
                    return new List<string> { "c", "d" }.GetEnumerator();
                else
                    return new List<string>().GetEnumerator();
            }

            var results = await new PagedAsyncEnumerable<Page, string>(GetPage, GetItems).ToArray();

            Assert.AreEqual(4, results.Length);
            Assert.AreEqual("a", results[0]);
            Assert.AreEqual("b", results[1]);
            Assert.AreEqual("c", results[2]);
            Assert.AreEqual("d", results[3]);
        }

        class Page
        {
            public int Index;
        }
    }
}
