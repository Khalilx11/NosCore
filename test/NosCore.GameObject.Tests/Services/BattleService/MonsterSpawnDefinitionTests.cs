//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Services.MapInstanceGenerationService;
using NosCore.Tests.Shared;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    [TestClass]
    public class MonsterSpawnDefinitionTests
    {
        // 4x2: the second row ends in a wall.
        private static GameObject.Map.Map Map() => new()
        {
            MapId = 1,
            Data = new byte[] { 4, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
        };

        private static NpcMonsterDto Monster(int hp = 100) => new() { NpcMonsterVNum = 9, MaxHp = hp };

        private static MonsterSpawnDefinition Spawn(short x, short y) => new(1, 9, x, y, 2, false);

        [TestMethod]
        public void ASpawnFromTheDataKeepsItsPlaceAndDirection()
        {
            var spawn = MonsterSpawnDefinition.From(new MapMonsterDto { MapMonsterId = 4, VNum = 9, MapX = 3, MapY = 1, Direction = 5, IsDisabled = true });

            Assert.AreEqual(new MonsterSpawnDefinition(4, 9, 3, 1, 5, true), spawn);
        }

        [TestMethod]
        public void AMonsterWithHealthOnAWalkableCellIsHonoured()
        {
            Assert.IsNull(Spawn(2, 1).Check(Monster(), Map()));
        }

        [TestMethod]
        public void ASpawnOfAMonsterThatDoesNotExistIsRefused()
        {
            Assert.AreEqual(SpawnRefusal.UnknownMonster, Spawn(0, 0).Check(null, Map()));
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-2615)]
        public void AMonsterWithoutPositiveHealthIsRefused(int hp)
        {
            Assert.AreEqual(SpawnRefusal.NoHealth, Spawn(0, 0).Check(Monster(hp), Map()));
        }

        [DataTestMethod]
        [DataRow(4, 0)]
        [DataRow(0, 2)]
        [DataRow(-1, 0)]
        public void ASpawnOutsideTheMapIsRefused(int x, int y)
        {
            Assert.AreEqual(SpawnRefusal.OffTheMap, Spawn((short)x, (short)y).Check(Monster(), Map()));
        }

        [TestMethod]
        public void ASpawnOnACellNobodyCanWalkOnIsRefused()
        {
            Assert.AreEqual(SpawnRefusal.BlockedCell, Spawn(3, 1).Check(Monster(), Map()));
        }

        [TestMethod]
        public async Task AMapLoadsOnlyTheSpawnsThatCanBeHonoured()
        {
            await TestHelpers.ResetAsync();
            var map = TestHelpers.Instance.MapInstanceAccessorService.GetBaseMapById(0)!;
            map.Map = new GameObject.Map.Map { MapId = 0, Data = new byte[] { 4, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 1 } };

            map.LoadMonsters(
                new List<MapMonsterDto>
                {
                    new() { MapMonsterId = 1, MapId = 0, MapX = 1, MapY = 0, VNum = 9 },
                    new() { MapMonsterId = 2, MapId = 0, MapX = 3, MapY = 1, VNum = 9 },
                    new() { MapMonsterId = 3, MapId = 0, MapX = 0, MapY = 0, VNum = 8 },
                    new() { MapMonsterId = 4, MapId = 0, MapX = 0, MapY = 0, VNum = 7 },
                },
                new List<NpcMonsterDto>
                {
                    new() { NpcMonsterVNum = 9, MaxHp = 50 },
                    new() { NpcMonsterVNum = 7, MaxHp = -5 },
                });

            CollectionAssert.AreEqual(new[] { 1L }, map.Monsters.Select(m => m.VisualId).ToArray());
        }
    }
}
