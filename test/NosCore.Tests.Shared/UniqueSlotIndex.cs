//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NosCore.Data.Enumerations;
using NosCore.Database;
using NosCore.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Tests.Shared
{
    // The in-memory provider has no unique indexes and writes rows in any order. This refuses a save the way
    // the (CharacterId, Slot, Type) index does on PostgreSQL: two rows that trade places cannot be ordered, and
    // no row may end up on a slot another row holds.
    public sealed class UniqueSlotIndex : SaveChangesInterceptor
    {
        public static DbContextOptionsBuilder<NosCoreContext> Enforced(DbContextOptionsBuilder<NosCoreContext> options) =>
            options.AddInterceptors(new UniqueSlotIndex())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Verify(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Verify(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private readonly record struct Key(long CharacterId, short Slot, NoscorePocketType Type);

        private static Key Of(PropertyValues values) => new(values.GetValue<long>(nameof(InventoryItemInstance.CharacterId)),
            values.GetValue<short>(nameof(InventoryItemInstance.Slot)),
            values.GetValue<NoscorePocketType>(nameof(InventoryItemInstance.Type)));

        private static void Verify(DbContext? context)
        {
            if (context == null)
            {
                return;
            }

            var changed = context.ChangeTracker.Entries<InventoryItemInstance>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
            if (changed.Count == 0)
            {
                return;
            }

            var moves = changed.Where(e => e.State == EntityState.Modified)
                .Select(e => (e.Entity.Id, From: Of(e.OriginalValues), To: Of(e.CurrentValues)))
                .Where(m => m.From != m.To).ToList();
            var holderOf = moves.ToDictionary(m => m.From, m => m);
            foreach (var move in moves)
            {
                var current = move;
                for (var step = 0; step <= moves.Count; step++)
                {
                    if (!holderOf.TryGetValue(current.To, out var next))
                    {
                        break;
                    }

                    if (next.Id == move.Id)
                    {
                        throw new InvalidOperationException(
                            "Unable to save changes because a circular dependency was detected in the data to be saved: " +
                            "InventoryItemInstance [Modified] <- Index { 'CharacterId', 'Slot', 'Type' } InventoryItemInstance [Modified]");
                    }

                    current = next;
                }
            }

            var final = context.Set<InventoryItemInstance>().AsNoTracking()
                .Select(e => new { e.Id, e.CharacterId, e.Slot, e.Type }).ToList()
                .ToDictionary(e => e.Id, e => new Key(e.CharacterId, e.Slot, e.Type));
            foreach (var entry in changed)
            {
                if (entry.State == EntityState.Deleted)
                {
                    final.Remove(entry.Entity.Id);
                }
                else
                {
                    final[entry.Entity.Id] = Of(entry.CurrentValues);
                }
            }

            var clash = final.GroupBy(f => f.Value).FirstOrDefault(g => g.Count() > 1);
            if (clash != null)
            {
                throw new DbUpdateException("duplicate key value violates unique constraint " +
                    $"\"IX_InventoryItemInstance_CharacterId_Slot_Type\": ({clash.Key.CharacterId}, {clash.Key.Slot}, {clash.Key.Type})");
            }
        }
    }
}
