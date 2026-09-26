//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;
using NosCore.Parser.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Tests
{
    [TestClass]
    public class DataValidatorTests
    {
        private static MapDto Map(short id, byte[] cells, byte width = 4)
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes((short)width));
            data.AddRange(BitConverter.GetBytes((short)(cells.Length / width)));
            data.AddRange(cells);
            return new MapDto { MapId = id, Data = data.ToArray() };
        }

        // Two rows of four cells: the last cell of the second row is a wall.
        private static readonly byte[] OpenMapCells = { 0, 0, 0, 0, 0, 0, 0, 1 };

        private static ImportedData Data(
            IEnumerable<ItemDto>? items = null, IEnumerable<SkillDto>? skills = null, IEnumerable<CardDto>? cards = null,
            IEnumerable<BCardDto>? bCards = null, IEnumerable<NpcMonsterDto>? monsters = null,
            IEnumerable<NpcMonsterSkillDto>? links = null, IEnumerable<DropDto>? drops = null,
            IEnumerable<MapDto>? maps = null, IEnumerable<MapMonsterDto>? mapMonsters = null,
            IEnumerable<MapNpcDto>? mapNpcs = null, IEnumerable<PortalDto>? portals = null) => new(
            (items ?? Array.Empty<ItemDto>()).ToList(), (skills ?? Array.Empty<SkillDto>()).ToList(),
            (cards ?? Array.Empty<CardDto>()).ToList(), (bCards ?? Array.Empty<BCardDto>()).ToList(),
            (monsters ?? Array.Empty<NpcMonsterDto>()).ToList(), (links ?? Array.Empty<NpcMonsterSkillDto>()).ToList(),
            (drops ?? Array.Empty<DropDto>()).ToList(), (maps ?? Array.Empty<MapDto>()).ToList(),
            (mapMonsters ?? Array.Empty<MapMonsterDto>()).ToList(), (mapNpcs ?? Array.Empty<MapNpcDto>()).ToList(),
            (portals ?? Array.Empty<PortalDto>()).ToList());

        private static NpcMonsterDto Monster(short vnum, int hp = 100, short basicSkill = 0) =>
            new() { NpcMonsterVNum = vnum, MaxHp = hp, DamageMinimum = 1, DamageMaximum = 2, BasicSkill = basicSkill };

        [TestMethod]
        public void CleanDataHasNoIssues()
        {
            var issues = DataValidator.Validate(Data(
                items: new[] { new ItemDto { VNum = 10 } },
                monsters: new[] { Monster(1) },
                drops: new[] { new DropDto { DropId = 1, VNum = 10, MonsterVNum = 1, Amount = 1, DropChance = 500 } },
                maps: new[] { Map(1, OpenMapCells) },
                mapMonsters: new[] { new MapMonsterDto { MapMonsterId = 1, MapId = 1, VNum = 1, MapX = 1, MapY = 1 } }));

            Assert.AreEqual(0, issues.Count, string.Join(Environment.NewLine, issues));
        }

        [TestMethod]
        public void ADuplicatedIdIsReported()
        {
            var issues = DataValidator.Validate(Data(items: new[] { new ItemDto { VNum = 10 }, new ItemDto { VNum = 10 } }));

            Assert.AreEqual(1, issues.Count(i => i.Kind == DataIssueKind.DuplicateId && i.Table == "Item" && i.Key == "10"));
        }

        [TestMethod]
        public void ADropOfAnUnknownItemOrMonsterIsReported()
        {
            var issues = DataValidator.Validate(Data(
                items: new[] { new ItemDto { VNum = 10 } },
                monsters: new[] { Monster(1) },
                drops: new[]
                {
                    new DropDto { DropId = 1, VNum = 99, MonsterVNum = 1, Amount = 1, DropChance = 5 },
                    new DropDto { DropId = 2, VNum = 10, MonsterVNum = 77, Amount = 1, DropChance = 5 },
                }));

            Assert.IsTrue(issues.Any(i => i.Table == "Drop" && i.Key == "1" && i.Message.Contains("item 99")));
            Assert.IsTrue(issues.Any(i => i.Table == "Drop" && i.Key == "2" && i.Message.Contains("monster 77")));
        }

        [TestMethod]
        public void ADropWithoutAnAmountOrAChanceIsOutOfRange()
        {
            var issues = DataValidator.Validate(Data(
                items: new[] { new ItemDto { VNum = 10 } },
                drops: new[] { new DropDto { DropId = 1, VNum = 10, Amount = 0, DropChance = 300 } }));

            Assert.AreEqual(1, issues.Count(i => i.Kind == DataIssueKind.OutOfRange && i.Table == "Drop"));
        }

        [TestMethod]
        public void AMonsterSkillLinkToNothingIsReported()
        {
            var issues = DataValidator.Validate(Data(
                monsters: new[] { Monster(1) },
                links: new[] { new NpcMonsterSkillDto { NpcMonsterSkillId = 5, NpcMonsterVNum = 1, SkillVNum = 4 } }));

            Assert.IsTrue(issues.Any(i => i.Table == "NpcMonsterSkill" && i.Message.Contains("skill 4")));
        }

        [TestMethod]
        public void AMonstersBasicSkillThatNamesNoSkillIsReported()
        {
            var issues = DataValidator.Validate(Data(monsters: new[] { Monster(1, basicSkill: 4630) }));

            Assert.IsTrue(issues.Any(i => i.Table == "NpcMonster" && i.Message.Contains("basic skill 4630")));
        }

        [TestMethod]
        public void ABCardPointingAtNothingIsReportedForEachOwnerKind()
        {
            var issues = DataValidator.Validate(Data(bCards: new[]
            {
                new BCardDto { BCardId = 1, SkillVNum = 1 },
                new BCardDto { BCardId = 2, CardId = 2 },
                new BCardDto { BCardId = 3, NpcMonsterVNum = 3 },
                new BCardDto { BCardId = 4, ItemVNum = 4 },
            }));

            Assert.AreEqual(4, issues.Count(i => i.Table == "BCard" && i.Kind == DataIssueKind.MissingReference));
        }

        [TestMethod]
        public void ANonPositiveMaxHpAndAReversedDamageRangeAreOutOfRange()
        {
            var issues = DataValidator.Validate(Data(monsters: new[]
            {
                Monster(1, hp: -5),
                new NpcMonsterDto { NpcMonsterVNum = 2, MaxHp = 10, DamageMinimum = 9, DamageMaximum = 3 },
            }));

            Assert.IsTrue(issues.Any(i => i.Key == "1" && i.Message.Contains("max HP")));
            Assert.IsTrue(issues.Any(i => i.Key == "2" && i.Message.Contains("reversed")));
        }

        [TestMethod]
        public void ASpawnMustNameAKnownMonsterOnAKnownMap()
        {
            var issues = DataValidator.Validate(Data(
                maps: new[] { Map(1, OpenMapCells) },
                mapMonsters: new[]
                {
                    new MapMonsterDto { MapMonsterId = 1, MapId = 1, VNum = 9, MapX = 0, MapY = 0 },
                    new MapMonsterDto { MapMonsterId = 2, MapId = 8, VNum = 9, MapX = 0, MapY = 0 },
                }));

            Assert.IsTrue(issues.Any(i => i.Table == "MapMonster" && i.Key == "1" && i.Message.Contains("monster 9")));
            Assert.IsTrue(issues.Any(i => i.Table == "MapMonster" && i.Key == "2" && i.Message.Contains("map 8")));
        }

        [DataTestMethod]
        [DataRow(4, 0, DataIssueKind.OutOfRange)]
        [DataRow(0, 2, DataIssueKind.OutOfRange)]
        [DataRow(-1, 0, DataIssueKind.OutOfRange)]
        [DataRow(3, 1, DataIssueKind.BlockedCoordinate)]
        public void ASpawnMustStandOnAWalkableCellInsideItsMap(int x, int y, DataIssueKind expected)
        {
            var issues = DataValidator.Validate(Data(
                monsters: new[] { Monster(1) },
                maps: new[] { Map(1, OpenMapCells) },
                mapMonsters: new[] { new MapMonsterDto { MapMonsterId = 1, MapId = 1, VNum = 1, MapX = (short)x, MapY = (short)y } }));

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(expected, issues[0].Kind);
        }

        [TestMethod]
        public void ACellWithAnyBitButWalkingDisabledStillCountsAsWalkable()
        {
            var issues = DataValidator.Validate(Data(
                monsters: new[] { Monster(1) },
                maps: new[] { Map(1, new byte[] { 10, 0, 0, 0 }) },
                mapMonsters: new[] { new MapMonsterDto { MapMonsterId = 1, MapId = 1, VNum = 1, MapX = 0, MapY = 0 } }));

            Assert.AreEqual(0, issues.Count);
        }

        [TestMethod]
        public void BothEndsOfAPortalAreChecked()
        {
            var issues = DataValidator.Validate(Data(
                maps: new[] { Map(1, OpenMapCells), Map(2, OpenMapCells) },
                portals: new[] { new PortalDto { PortalId = 5, SourceMapId = 1, SourceX = 1, SourceY = 1, DestinationMapId = 2, DestinationX = 3, DestinationY = 1 } }));

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual("Portal destination", issues[0].Table);
            Assert.AreEqual(DataIssueKind.BlockedCoordinate, issues[0].Kind);
        }

        [TestMethod]
        public void ValidationNeverChangesTheData()
        {
            var monsters = new[] { Monster(1, hp: -5) };

            DataValidator.Validate(Data(monsters: monsters));

            Assert.AreEqual(-5, monsters[0].MaxHp);
        }
    }
}
