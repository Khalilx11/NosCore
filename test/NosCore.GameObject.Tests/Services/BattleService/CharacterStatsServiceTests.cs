//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Packets.Interfaces;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class CharacterStatsServiceTests
    {
        private static CombatStats Stats() => new(
            Level: 30, HeroLevel: 0, Class: CharacterClassType.Archer, Morale: 30,
            MinHit: 11, MaxHit: 22, HitRate: 33, CriticalChance: 4, CriticalRate: 70, MeleeUpgrade: 3,
            MinDistance: 44, MaxDistance: 55, DistanceRate: 66, DistanceCriticalChance: 5, DistanceCriticalRate: 80, RangedUpgrade: 6,
            Element: 0, ElementRate: 0, ElementRateSp: 0,
            Defence: 77, DefenceRate: 0, DistanceDefence: 88, DistanceDefenceRate: 0, MagicDefence: 99,
            DefenceDodge: 12, DistanceDefenceDodge: 13, DefenceUpgrade: 2,
            FireResistance: 1, WaterResistance: 2, LightResistance: 3, DarkResistance: 4);

        [TestMethod]
        public void ThePacketCarriesTheRuntimeStatsInTheirWireSlots()
        {
            var packet = CharacterStatsService.ToPacket(Stats(), CharacterClassType.Archer);

            Assert.AreEqual(3, packet.MainWeaponUpgrade);
            Assert.AreEqual(11, packet.MinHit);
            Assert.AreEqual(22, packet.MaxHit);
            Assert.AreEqual(33, packet.HitRate);
            Assert.AreEqual(4, packet.CriticalHitRate);
            Assert.AreEqual(70, packet.CriticalHitMultiplier);
            Assert.AreEqual(6, packet.SecondaryWeaponUpgrade);
            Assert.AreEqual(44, packet.SecondaryMinHit);
            Assert.AreEqual(55, packet.SecondaryMaxHit);
            Assert.AreEqual(66, packet.SecondaryHitRate);
            Assert.AreEqual(5, packet.SecondaryCriticalHitRate);
            Assert.AreEqual(80, packet.SecondaryCriticalHitMultiplier);
            Assert.AreEqual(2, packet.ArmorUpgrade);
            Assert.AreEqual(77, packet.Defence);
            Assert.AreEqual(12, packet.DefenceRate);
            Assert.AreEqual(88, packet.DistanceDefence);
            Assert.AreEqual(13, packet.DistanceDefenceRate);
            Assert.AreEqual(99, packet.MagicalDefence);
            Assert.AreEqual(1, packet.FireResistance);
            Assert.AreEqual(4, packet.DarkResistance);
        }

        [DataTestMethod]
        [DataRow(CharacterClassType.Adventurer, 0, 1)]
        [DataRow(CharacterClassType.Swordsman, 0, 1)]
        [DataRow(CharacterClassType.Archer, 1, 0)]
        [DataRow(CharacterClassType.Mage, 2, 1)]
        public void EachClassNamesTheAttackKindOfItsTwoWeaponProfiles(CharacterClassType characterClass, int primary, int secondary)
        {
            var packet = CharacterStatsService.ToPacket(Stats(), characterClass);

            Assert.AreEqual(primary, packet.Type);
            Assert.AreEqual(secondary, packet.Type2);
        }

        [TestMethod]
        public void ValuesBeyondTheWireRangeAreClampedNotWrapped()
        {
            var packet = CharacterStatsService.ToPacket(Stats() with { Defence = 70_000, MeleeUpgrade = 400, FireResistance = -70_000 },
                CharacterClassType.Mage);

            Assert.AreEqual(short.MaxValue, packet.Defence);
            Assert.AreEqual(byte.MaxValue, packet.MainWeaponUpgrade);
            Assert.AreEqual(short.MinValue, packet.FireResistance);
        }

        [TestMethod]
        public async Task TheCharacterIsSentItsOwnCurrentStats()
        {
            var character = new Mock<ICharacterEntity>();
            character.SetupGet(c => c.Class).Returns(CharacterClassType.Archer);
            var provider = new Mock<IBattleStatsProvider>();
            provider.Setup(p => p.GetStats(character.Object)).Returns(Stats());

            await new CharacterStatsService(provider.Object).SendAsync(character.Object);

            character.Verify(c => c.SendPacketAsync(It.Is<IPacket>(p => p is ScPacket && ((ScPacket)p).MinHit == 11)), Times.Once);
        }
    }
}
