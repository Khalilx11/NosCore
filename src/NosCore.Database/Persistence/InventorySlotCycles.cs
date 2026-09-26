//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Database.Persistence
{
    public readonly record struct InventorySlot(long CharacterId, short Slot, NoscorePocketType Type);

    // Rows that trade places cannot be written in any order against the unique (CharacterId, Slot, Type)
    // index: each has to wait for the other to leave. Setting one row of every such group aside breaks it.
    public static class InventorySlotCycles
    {
        public static IReadOnlyList<Guid> RowsToSetAside(IReadOnlyDictionary<Guid, InventorySlot> stored,
            IReadOnlyDictionary<Guid, InventorySlot> wanted)
        {
            var moving = wanted.Where(w => stored.TryGetValue(w.Key, out var was) && was != w.Value)
                .Select(w => w.Key).ToList();
            var holderOf = moving.ToDictionary(id => stored[id], id => id);

            var classified = new HashSet<Guid>();
            var setAside = new List<Guid>();
            foreach (var start in moving)
            {
                var path = new HashSet<Guid>();
                var current = start;
                while (!classified.Contains(current))
                {
                    if (!path.Add(current))
                    {
                        setAside.Add(current);
                        break;
                    }

                    if (!holderOf.TryGetValue(wanted[current], out current))
                    {
                        break;
                    }
                }

                classified.UnionWith(path);
            }

            return setAside;
        }

        // Neutral because no row is ever stored at a negative slot.
        public static InventorySlot Neutral(InventorySlot stored, int index) =>
            stored with { Slot = (short)(-1 - index) };
    }
}
