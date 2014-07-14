using System;
using GitBin;
using GitBin.Commands;
using Moq;
using NUnit.Framework;

namespace git_bin.Tests.Commands
{
    [TestFixture]
    public class CleanCommandTest
    {
        private Mock<IConfigurationProvider> _configurationProvider;
        private Mock<ICacheManager> _cacheManager;

        [SetUp]
        public void SetUp()
        {
            _configurationProvider = new Mock<IConfigurationProvider>();
            _cacheManager = new Mock<ICacheManager>();
        }

        [Test]
        public void GolemTgaDebugging()
        {
            var _gitExecutor = new Mock<IGitExecutor>();

            _gitExecutor.Setup(x => x.GetString("config --get-regexp git-bin"))
                .Returns("git-bin.KeyOne ValueOne\ngit-bin.KeyTwo 2");

            _gitExecutor.Setup(x => x.GetString("rev-parse --git-dir")).Returns("a");   
            var cleanCommand =new CleanCommand(
                    new ConfigurationProvider(_gitExecutor.Object),
                    _cacheManager.Object,
                    new[] { @"C:\SharedClowns\Golem_Iron.tga" });

            cleanCommand.Execute();
       } 


        [Test]
        public void Ctor_OneArgument_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => 
                new CleanCommand(
                    _configurationProvider.Object,
                    _cacheManager.Object,
                    new[] {"filename"}));
        }

        [Test]
        public void Ctor_WrongNumberOfArguments_Throws()
        {
            Assert.Throws<ArgumentException>(() => 
                new CleanCommand(
                    _configurationProvider.Object,
                    _cacheManager.Object,
                    new string[0]));

            Assert.Throws<ArgumentException>(() =>
                new CleanCommand(
                    _configurationProvider.Object,
                    _cacheManager.Object,
                    new[] {"a", "b", "c"}));
        }
    }
}