//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.Enumerations.Map;
using NosCore.Data.StaticEntities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Parser.Validation
{
    public enum DataIssueKind
    {
        DuplicateId,
        MissingReference,
        OutOfRange,
        BlockedCoordinate,
    }

    public sealed record DataIssue(DataIssueKind Kind, string Table, string Key, string Message);

    public sealed record ImportedData(
        IReadOnlyCollection<ItemDto> Items,
        IReadOnlyCollection<SkillDto> Skills,
        IReadOnlyCollection<CardDto> Cards,
        IReadOnlyCollection<BCardDto> BCards,
        IReadOnlyCollection<NpcMonsterDto> NpcMonsters,
        IReadOnlyCollection<NpcMonsterSkillDto> NpcMonsterSkills,
        IReadOnlyCollection<DropDto> Drops,
        IReadOnlyCollection<MapDto> Maps,
        IReadOnlyCollection<MapMonsterDto> MapMonsters,
        IReadOnlyCollection<MapNpcDto> MapNpcs,
        IReadOnlyCollection<PortalDto> Portals);

    // Reports what an import left behind; it never repairs or drops anything.
    public static class DataValidator
    {
        public static IReadOnlyList<DataIssue> Validate(ImportedData data)
        {
            var issues = new List<DataIssue>();

            var items = data.Items.Select(i => i.VNum).ToHashSet();
            var skills = data.Skills.Select(s => s.SkillVNum).ToHashSet();
            var cards = data.Cards.Select(c => c.CardId).ToHashSet();
            var monsters = data.NpcMonsters.Select(m => m.NpcMonsterVNum).ToHashSet();
            var maps = data.Maps.ToDictionary(m => m.MapId);

            Duplicates(issues, "Item", data.Items.Select(i => (int)i.VNum));
            Duplicates(issues, "Skill", data.Skills.Select(s => (int)s.SkillVNum));
            Duplicates(issues, "Card", data.Cards.Select(c => (int)c.CardId));
            Duplicates(issues, "NpcMonster", data.NpcMonsters.Select(m => (int)m.NpcMonsterVNum));

            foreach (var drop in data.Drops)
            {
                if (!items.Contains(drop.VNum))
                {
                    Missing(issues, "Drop", drop.DropId, $"item {drop.VNum} does not exist");
                }

                if (drop.MonsterVNum is { } monster && !monsters.Contains(monster))
                {
                    Missing(issues, "Drop", drop.DropId, $"monster {monster} does not exist");
                }

                if (drop.Amount <= 0 || drop.DropChance <= 0)
                {
                    Range(issues, "Drop", drop.DropId, $"amount {drop.Amount} and chance {drop.DropChance} must both be positive");
                }
            }

            foreach (var link in data.NpcMonsterSkills)
            {
                if (!monsters.Contains(link.NpcMonsterVNum))
                {
                    Missing(issues, "NpcMonsterSkill", link.NpcMonsterSkillId, $"monster {link.NpcMonsterVNum} does not exist");
                }

                if (!skills.Contains(link.SkillVNum))
                {
                    Missing(issues, "NpcMonsterSkill", link.NpcMonsterSkillId, $"skill {link.SkillVNum} does not exist");
                }
            }

            foreach (var monster in data.NpcMonsters.Where(m => m.BasicSkill > 0 && !skills.Contains(m.BasicSkill)))
            {
                Missing(issues, "NpcMonster", monster.NpcMonsterVNum, $"basic skill {monster.BasicSkill} does not exist");
            }

            foreach (var card in data.BCards)
            {
                if (card.SkillVNum is { } skill && !skills.Contains(skill))
                {
                    Missing(issues, "BCard", card.BCardId, $"skill {skill} does not exist");
                }

                if (card.CardId is { } cardId && !cards.Contains(cardId))
                {
                    Missing(issues, "BCard", card.BCardId, $"card {cardId} does not exist");
                }

                if (card.NpcMonsterVNum is { } monster && !monsters.Contains(monster))
                {
                    Missing(issues, "BCard", card.BCardId, $"monster {monster} does not exist");
                }

                if (card.ItemVNum is { } item && !items.Contains(item))
                {
                    Missing(issues, "BCard", card.BCardId, $"item {item} does not exist");
                }
            }

            foreach (var spawn in data.MapMonsters)
            {
                if (!monsters.Contains(spawn.VNum))
                {
                    Missing(issues, "MapMonster", spawn.MapMonsterId, $"monster {spawn.VNum} does not exist");
                }

                Placement(issues, "MapMonster", spawn.MapMonsterId, maps, spawn.MapId, spawn.MapX, spawn.MapY);
            }

            foreach (var spawn in data.MapNpcs)
            {
                if (!monsters.Contains(spawn.VNum))
                {
                    Missing(issues, "MapNpc", spawn.MapNpcId, $"npc {spawn.VNum} does not exist");
                }

                Placement(issues, "MapNpc", spawn.MapNpcId, maps, spawn.MapId, spawn.MapX, spawn.MapY);
            }

            foreach (var portal in data.Portals)
            {
                Placement(issues, "Portal source", portal.PortalId, maps, portal.SourceMapId, portal.SourceX, portal.SourceY);
                Placement(issues, "Portal destination", portal.PortalId, maps, portal.DestinationMapId, portal.DestinationX, portal.DestinationY);
            }

            foreach (var monster in data.NpcMonsters)
            {
                if (monster.MaxHp <= 0)
                {
                    Range(issues, "NpcMonster", monster.NpcMonsterVNum, $"max HP {monster.MaxHp} is not positive");
                }

                if (monster.DamageMinimum > monster.DamageMaximum)
                {
                    Range(issues, "NpcMonster", monster.NpcMonsterVNum, $"damage {monster.DamageMinimum}-{monster.DamageMaximum} is reversed");
                }
            }

            return issues;
        }

        private static void Duplicates(List<DataIssue> issues, string table, IEnumerable<int> keys)
        {
            foreach (var key in keys.GroupBy(k => k).Where(g => g.Count() > 1))
            {
                issues.Add(new DataIssue(DataIssueKind.DuplicateId, table, key.Key.ToString(), $"appears {key.Count()} times"));
            }
        }

        private static void Missing(List<DataIssue> issues, string table, long key, string message) =>
            issues.Add(new DataIssue(DataIssueKind.MissingReference, table, key.ToString(), message));

        private static void Range(List<DataIssue> issues, string table, long key, string message) =>
            issues.Add(new DataIssue(DataIssueKind.OutOfRange, table, key.ToString(), message));

        // A placement is valid when its map exists, the cell is inside the grid and walking is allowed there.
        private static void Placement(List<DataIssue> issues, string table, long key, IReadOnlyDictionary<short, MapDto> maps,
            short mapId, short x, short y)
        {
            if (!maps.TryGetValue(mapId, out var map))
            {
                Missing(issues, table, key, $"map {mapId} does not exist");
                return;
            }

            var data = map.Data;
            if (data is not { Length: >= 4 })
            {
                Range(issues, table, key, $"map {mapId} has no grid");
                return;
            }

            var width = BitConverter.ToInt16(data, 0);
            var height = BitConverter.ToInt16(data, 2);
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                Range(issues, table, key, $"({x},{y}) is outside map {mapId} ({width}x{height})");
                return;
            }

            if (((MapCellFlags)data[4 + y * width + x] & MapCellFlags.WalkingDisabled) != 0)
            {
                issues.Add(new DataIssue(DataIssueKind.BlockedCoordinate, table, key.ToString(), $"({x},{y}) on map {mapId} is a cell nobody can walk on"));
            }
        }
    }
}
