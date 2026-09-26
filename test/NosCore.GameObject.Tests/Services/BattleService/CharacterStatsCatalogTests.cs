//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Algorithm.CloseDefenceService;
using NosCore.Algorithm.DamageService;
using NosCore.Algorithm.HitRateService;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.GameObject.Services.EquipmentService;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CharacterStatsCatalogTests
    {
        private static readonly CharacterClassType[] Classes =
        {
            CharacterClassType.Adventurer, CharacterClassType.Swordsman, CharacterClassType.Archer,
            CharacterClassType.Mage, CharacterClassType.MartialArtist,
        };

        [TestMethod]
        public void EveryFigureIsTheAlgorithmPackagesFigure()
        {
            var catalog = CharacterStatsFixture.Catalog();
            var damage = new DamageService();
            var hit = new HitRateService();
            var defence = new CloseDefenceService();

            foreach (var characterClass in Classes)
            {
                foreach (byte level in new byte[] { 1, 5, 20, 60, 99 })
                {
                    var definition = catalog.Get(characterClass, level);

                    Assert.AreEqual((int)damage.GetMinDamage(characterClass, level), definition.MinHit);
                    Assert.AreEqual((int)damage.GetMaxDamage(characterClass, level), definition.MaxHit);
                    Assert.AreEqual((int)hit.GetHitRate(characterClass, level), definition.HitRate);
                    Assert.AreEqual((int)defence.GetCloseDefence(characterClass, level), definition.Defence);
                    Assert.AreEqual(characterClass, definition.Class);
                    Assert.AreEqual(level, definition.Level);
                }
            }
        }

        [TestMethod]
        public void TheSameClassAndLevelGiveTheSameDefinition()
        {
            var catalog = CharacterStatsFixture.Catalog();

            Assert.AreSame(catalog.Get(CharacterClassType.Mage, 30), catalog.Get(CharacterClassType.Mage, 30));
        }

        [TestMethod]
        public void ALevelOutsideTheTablesIsClampedToTheirEnds()
        {
            var catalog = CharacterStatsFixture.Catalog();

            Assert.AreEqual(1, catalog.Get(CharacterClassType.Swordsman, 0).Level);
            Assert.AreEqual(99, catalog.Get(CharacterClassType.Swordsman, 200).Level);
        }

        [TestMethod]
        public void TheMartialArtistHasItsOwnCurveRatherThanTheAdventurers()
        {
            var catalog = CharacterStatsFixture.Catalog();

            Assert.AreNotEqual(catalog.Get(CharacterClassType.Adventurer, 40).DefenceDodge,
                catalog.Get(CharacterClassType.MartialArtist, 40).DefenceDodge);
        }

        private static CombatStats NakedStatsOf(CharacterClassType characterClass, byte level)
        {
            var character = new Mock<ICharacterEntity>();
            character.SetupGet(c => c.Level).Returns(level);
            character.SetupGet(c => c.HeroLevel).Returns((byte)0);
            character.SetupGet(c => c.Class).Returns(characterClass);

            var buffs = new Mock<IBuffService>();
            buffs.Setup(b => b.GetActiveBuffs(It.IsAny<IAliveEntity>())).Returns(new List<BuffInstance>());
            var equipment = new Mock<IEquipmentStatsService>();
            equipment.Setup(s => s.Resolve(It.IsAny<IAliveEntity>())).Returns(EquipmentStats.None);

            return new BattleStatsProvider(buffs.Object, equipment.Object, CharacterStatsFixture.Catalog()).GetStats(character.Object);
        }

        [TestMethod]
        public void ANakedCharactersRuntimeStatsAreItsDefinition()
        {
            var definition = CharacterStatsFixture.Catalog().Get(CharacterClassType.Archer, 30);

            var stats = NakedStatsOf(CharacterClassType.Archer, 30);

            Assert.AreEqual(definition.MinHit, stats.MinHit);
            Assert.AreEqual(definition.MaxHit, stats.MaxHit);
            Assert.AreEqual(definition.MinDistance, stats.MinDistance);
            Assert.AreEqual(definition.MaxDistance, stats.MaxDistance);
            Assert.AreEqual(definition.HitRate, stats.HitRate);
            Assert.AreEqual(definition.DistanceRate, stats.DistanceRate);
            Assert.AreEqual(definition.Defence, stats.Defence);
            Assert.AreEqual(definition.DistanceDefence, stats.DistanceDefence);
            Assert.AreEqual(definition.MagicDefence, stats.MagicDefence);
            Assert.AreEqual(definition.DefenceDodge, stats.DefenceDodge);
            Assert.AreEqual(definition.DistanceDefenceDodge, stats.DistanceDefenceDodge);
        }

        [TestMethod]
        public void TheRuntimeStatsRiseWithTheLevel()
        {
            Assert.IsTrue(NakedStatsOf(CharacterClassType.Swordsman, 50).MaxHit > NakedStatsOf(CharacterClassType.Swordsman, 10).MaxHit);
        }
    }
}
