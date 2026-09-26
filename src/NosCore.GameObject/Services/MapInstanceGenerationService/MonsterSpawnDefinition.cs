//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Dto;
using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.MapInstanceGenerationService
{
    public enum SpawnRefusal
    {
        UnknownMonster,
        NoHealth,
        OffTheMap,
        BlockedCell,
    }

    // Where a monster stands when its map loads. The data only carries the position and direction: how
    // far it wanders, how often it comes back and whether it is hostile are the monster's own.
    public sealed record MonsterSpawnDefinition(int MapMonsterId, short MonsterVNum, short X, short Y, byte Direction, bool IsDisabled)
    {
        public static MonsterSpawnDefinition From(MapMonsterDto spawn) =>
            new(spawn.MapMonsterId, spawn.VNum, spawn.MapX, spawn.MapY, spawn.Direction, spawn.IsDisabled);

        // Nothing is repaired: a spawn that cannot be honoured is refused and reported by the caller.
        public SpawnRefusal? Check(NpcMonsterDto? monster, Map.Map map)
        {
            if (monster == null)
            {
                return SpawnRefusal.UnknownMonster;
            }

            if (monster.MaxHp <= 0)
            {
                return SpawnRefusal.NoHealth;
            }

            if (X < 0 || Y < 0 || X >= map.Width || Y >= map.Height)
            {
                return SpawnRefusal.OffTheMap;
            }

            return map.IsWalkable(X, Y) ? null : SpawnRefusal.BlockedCell;
        }
    }
}
