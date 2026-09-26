//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.ExperienceService;
using NosCore.Algorithm.FairyExperienceService;
using NosCore.Algorithm.HeroExperienceService;
using NosCore.Algorithm.JobExperienceService;
using NosCore.Algorithm.SpExperienceService;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.ExperienceService;
using NosCore.GameObject.Services.SkillService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.ExperienceService
{
    [TestClass]
    public class ExperienceProgressionStatsTests
    {
        private ClientSession _session = null!;
        private Mock<ICharacterStatsService> _stats = null!;
        private ExperienceProgressionService _service = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            await TestHelpers.ResetAsync();
            _session = await TestHelpers.Instance.GenerateSessionAsync();
            _session.Character.MapInstance = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(1)!;
            _stats = new Mock<ICharacterStatsService>();
            _service = new ExperienceProgressionService(
                new NosCore.Algorithm.ExperienceService.ExperienceService(),
                new JobExperienceService(),
                new HeroExperienceService(),
                new SpExperienceService(),
                new FairyExperienceService(),
                new Mock<ISkillService>().Object,
                new Mock<IVitalityService>().Object,
                _stats.Object);
        }

        [TestMethod]
        public async Task ALevelUpSendsTheStatsWindow()
        {
            _session.Character.Level = 1;
            _session.Character.LevelXp = 0;

            await _service.AddExperienceAsync(_session.Character, levelXpDelta: 1_000_000, jobXpDelta: 0, heroXpDelta: 0,
                spXpDelta: 0, fairyXpDelta: 0);

            var character = _session.Character;
            Assert.IsTrue(character.Level > 1);
            _stats.Verify(s => s.SendAsync(character), Times.Once);
        }

        [TestMethod]
        public async Task ExperienceShortOfALevelDoesNotSendIt()
        {
            _session.Character.Level = 1;
            _session.Character.LevelXp = 0;

            await _service.AddExperienceAsync(_session.Character, levelXpDelta: 1, jobXpDelta: 0, heroXpDelta: 0,
                spXpDelta: 0, fairyXpDelta: 0);

            var character = _session.Character;
            Assert.AreEqual(1, character.Level);
            _stats.Verify(s => s.SendAsync(character), Times.Never);
        }
    }
}
