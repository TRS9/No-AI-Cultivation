using System;
using System.IO;
using System.Reflection;
using CultivationGame.Systems;
using NUnit.Framework;

namespace CultivationGame.Tests
{
    public class SaveSystemTests
    {
        private string _directory;
        private string _path;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "CultivationSaveTests-" + Guid.NewGuid());
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "save.json");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_directory, true);

        private void Write(string content) => typeof(SaveSystem)
            .GetMethod("WriteAtomically", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { _path, content });

        [Test]
        public void FirstSaveAndReplacementLeaveCompleteFile()
        {
            Write("{\"currentQi\":10}");
            Write("{\"currentQi\":20}");
            Assert.That(File.ReadAllText(_path), Is.EqualTo("{\"currentQi\":20}"));
            Assert.That(File.Exists(_path + ".tmp"), Is.False);
        }

        [Test]
        public void FailedTemporaryWritePreservesExistingSave()
        {
            Write("previous save");
            // A directory at the temp-file path forces a write failure on every OS.
            Directory.CreateDirectory(_path + ".tmp");
            Assert.Throws<TargetInvocationException>(() => Write("replacement"));
            Assert.That(File.ReadAllText(_path), Is.EqualTo("previous save"));
        }
    }
}
