using System;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.SoundEffects;

namespace Mute.Tests.Services.SoundEffects
{
    [TestClass]
    public class DatabaseSoundEffectsLibraryTests
    {
        private (IDatabaseService, ISoundEffectLibrary) Create()
        {
            var db = new SqliteInMemoryDatabase();
            var sx = new DatabaseSoundEffectLibrary(new Moe.Configuration {
                SoundEffects = new Moe.SoundEffectConfig { 
                    SfxFolder = "/",
                },
            }, db, new MockFileSystem());

            return (db, sx);
        }

        [TestMethod]
        public async Task CreateSoundEffectDoesNotThrow()
        {
            var (db, sx) = Create();

            var effect = await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });

            Assert.AreEqual((ulong)123, effect.Guild);
            Assert.AreEqual("name", effect.Name);
            Assert.IsNotNull(effect.Path);
        }

        [TestMethod]
        public async Task CreateSoundEffectThrowsWithDuplicate()
        {
            var (db, sx) = Create();

            await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });

            var thrown = false;
            try
            {
                await sx.Create(123, "name", new byte[] {0, 0, 0, 0});
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }

            if (!thrown)
                Assert.Fail();
        }

        [TestMethod]
        public async Task GetGetsExistingSoundEffect()
        {
            var (db, sx) = Create();

            var created = await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });
            var got = await sx.Get(123, "name");

            Assert.IsNotNull(got);
            Assert.AreEqual(created.Guild, got.Guild);
            Assert.AreEqual(created.Name, got.Name);
            Assert.AreEqual(created.Path, got.Path);
        }

        [TestMethod]
        public async Task GetGetsNothingForSoundEffectInOtherGuild()
        {
            var (db, sx) = Create();

            await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });

            var got = await sx.Get(321, "name");

            Assert.IsNull(got);
        }

        [TestMethod]
        public async Task GetGetsByAlias()
        {
            var (db, sx) = Create();

            var created = await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });
            var aliased = await sx.Alias("name2", created);

            var got = await sx.Get(123, "name2");

            Assert.AreEqual(created.Path, got.Path);
        }

        [TestMethod]
        public async Task CannotCreateDuplicateAlias()
        {
            var (db, sx) = Create();

            var created = await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });
            await sx.Alias("name2", created);

            var thrown = false;
            try
            {
                await sx.Alias("name2", created);
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }

            if (!thrown)
                Assert.Fail();
        }

        [TestMethod]
        public async Task FindFindsBySubstring()
        {
            var (db, sx) = Create();

            var created = await sx.Create(123, "name", new byte[] { 0, 0, 0, 0 });

            var aliased1 = await sx.Alias("nam", created);
            var aliased2 = await sx.Alias("na", created);
            var aliased3 = await sx.Alias("n", created);

            var found = await (await sx.Find(123, "na")).ToArray();

            Assert.AreEqual(3, found.Length);
            Assert.IsTrue(found.Any(a => a.Name == "na"));
            Assert.IsTrue(found.Any(a => a.Name == "nam"));
            Assert.IsTrue(found.Any(a => a.Name == "name"));
        }
    }
}
