using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Utilities;

namespace Mute.Tests.Utilities
{
    [TestClass]
    public class StringEditTests
    {
        private const ulong AnyEditor = 42UL;

        #region Apply - Insert

        [TestMethod]
        public void Apply_Insert_InsertsTextAtPosition()
        {
            var edit = new StringEdit(StringEditType.Insert, 5, "world", AnyEditor);
            Assert.AreEqual("helloworld there", edit.Apply("hello there"));
        }

        [TestMethod]
        public void Apply_Insert_AtBeginning()
        {
            var edit = new StringEdit(StringEditType.Insert, 0, "Hello, ", AnyEditor);
            Assert.AreEqual("Hello, world", edit.Apply("world"));
        }

        [TestMethod]
        public void Apply_Insert_AtEnd()
        {
            var edit = new StringEdit(StringEditType.Insert, 5, "!", AnyEditor);
            Assert.AreEqual("hello!", edit.Apply("hello"));
        }

        [TestMethod]
        public void Apply_Insert_IntoEmptyString()
        {
            var edit = new StringEdit(StringEditType.Insert, 0, "abc", AnyEditor);
            Assert.AreEqual("abc", edit.Apply(string.Empty));
        }

        #endregion

        #region Apply - Delete

        [TestMethod]
        public void Apply_Delete_RemovesTextAtPosition()
        {
            var edit = new StringEdit(StringEditType.Delete, 5, " there", AnyEditor);
            Assert.AreEqual("hello", edit.Apply("hello there"));
        }

        [TestMethod]
        public void Apply_Delete_AtBeginning()
        {
            var edit = new StringEdit(StringEditType.Delete, 0, "Hello, ", AnyEditor);
            Assert.AreEqual("world", edit.Apply("Hello, world"));
        }

        [TestMethod]
        public void Apply_Delete_AtEnd()
        {
            var edit = new StringEdit(StringEditType.Delete, 5, "!", AnyEditor);
            Assert.AreEqual("hello", edit.Apply("hello!"));
        }

        [TestMethod]
        public void Apply_Delete_EntireString()
        {
            var edit = new StringEdit(StringEditType.Delete, 0, "hello", AnyEditor);
            Assert.AreEqual(string.Empty, edit.Apply("hello"));
        }

        #endregion

        #region Unapply - Insert

        [TestMethod]
        public void Unapply_Insert_RemovesInsertedText()
        {
            var edit = new StringEdit(StringEditType.Insert, 5, "world", AnyEditor);
            Assert.AreEqual("hello there", edit.Unapply("helloworld there"));
        }

        [TestMethod]
        public void Unapply_Insert_AtBeginning()
        {
            var edit = new StringEdit(StringEditType.Insert, 0, "Hello, ", AnyEditor);
            Assert.AreEqual("world", edit.Unapply("Hello, world"));
        }

        [TestMethod]
        public void Unapply_Insert_AtEnd()
        {
            var edit = new StringEdit(StringEditType.Insert, 5, "!", AnyEditor);
            Assert.AreEqual("hello", edit.Unapply("hello!"));
        }

        #endregion

        #region Unapply - Delete

        [TestMethod]
        public void Unapply_Delete_ReInsertsDeletedText()
        {
            var edit = new StringEdit(StringEditType.Delete, 5, " there", AnyEditor);
            Assert.AreEqual("hello there", edit.Unapply("hello"));
        }

        [TestMethod]
        public void Unapply_Delete_AtBeginning()
        {
            var edit = new StringEdit(StringEditType.Delete, 0, "Hello, ", AnyEditor);
            Assert.AreEqual("Hello, world", edit.Unapply("world"));
        }

        [TestMethod]
        public void Unapply_Delete_AtEnd()
        {
            var edit = new StringEdit(StringEditType.Delete, 5, "!", AnyEditor);
            Assert.AreEqual("hello!", edit.Unapply("hello"));
        }

        #endregion

        #region Round-trip

        [TestMethod]
        public void Apply_ThenUnapply_Insert_ReturnsOriginal()
        {
            const string original = "hello world";
            var edit = new StringEdit(StringEditType.Insert, 5, " beautiful", AnyEditor);
            Assert.AreEqual(original, edit.Unapply(edit.Apply(original)));
        }

        [TestMethod]
        public void Apply_ThenUnapply_Delete_ReturnsOriginal()
        {
            const string original = "hello beautiful world";
            var edit = new StringEdit(StringEditType.Delete, 5, " beautiful", AnyEditor);
            Assert.AreEqual(original, edit.Unapply(edit.Apply(original)));
        }

        #endregion

        #region Invalid type

        [TestMethod]
        public void Apply_UnknownType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var edit = new StringEdit((StringEditType)999, 0, "x", AnyEditor);
                edit.Apply("hello");
            });
        }

        [TestMethod]
        public void Unapply_UnknownType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var edit = new StringEdit((StringEditType)999, 0, "x", AnyEditor);
                edit.Unapply("hello");
            });
        }

        #endregion
    }
}
