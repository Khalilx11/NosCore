//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.Database.Persistence
{
    // The generic batch upsert is one SaveChanges, and EF refuses to order two updates that swap the
    // (CharacterId, Slot, Type) of two rows. Every other batch is left to it untouched.
    public sealed class InventoryItemInstanceDao : Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>,
        IDao<InventoryItemInstanceDto, Guid>
    {
        private readonly ILogger<Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>> _logger;
        private readonly Func<DbContext> _contextBuilder;

        public InventoryItemInstanceDao(ILogger<Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>> logger,
            Func<DbContext> contextBuilder) : base(logger, contextBuilder)
        {
            _logger = logger;
            _contextBuilder = contextBuilder;
        }

        public new async Task<bool> TryInsertOrUpdateAsync(IEnumerable<InventoryItemInstanceDto> dtos)
        {
            var rows = dtos.ToList();
            try
            {
                var aside = await RowsToSetAsideAsync(rows);
                return aside.Count == 0
                    ? await base.TryInsertOrUpdateAsync(rows)
                    : await SaveSettingAsideAsync(rows, aside);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "TryInsertOrUpdateAsync<{Entity}> batch failed", nameof(InventoryItemInstance));
                return false;
            }
        }

        private async Task<List<InventoryItemInstanceDto>> RowsToSetAsideAsync(List<InventoryItemInstanceDto> rows)
        {
            var ids = rows.Select(r => r.Id).ToList();
            await using var context = _contextBuilder();
            var stored = (await context.Set<InventoryItemInstance>().AsNoTracking()
                    .Where(e => ids.Contains(e.Id))
                    .Select(e => new { e.Id, e.CharacterId, e.Slot, e.Type })
                    .ToListAsync())
                .ToDictionary(e => e.Id, e => new InventorySlot(e.CharacterId, e.Slot, e.Type));
            var wanted = rows.ToDictionary(r => r.Id, r => new InventorySlot(r.CharacterId, r.Slot, r.Type));

            return InventorySlotCycles.RowsToSetAside(stored, wanted)
                .Select((id, index) =>
                {
                    var neutral = InventorySlotCycles.Neutral(stored[id], index);
                    var row = rows.First(r => r.Id == id);
                    return new InventoryItemInstanceDto
                    {
                        Id = id,
                        CharacterId = neutral.CharacterId,
                        ItemInstanceId = row.ItemInstanceId,
                        Slot = neutral.Slot,
                        Type = neutral.Type,
                    };
                })
                .ToList();
        }

        // Both writes share one context and one transaction: the neutral slot is never visible to anybody else
        // and a failure of either leaves every row where it was.
        private async Task<bool> SaveSettingAsideAsync(List<InventoryItemInstanceDto> rows,
            List<InventoryItemInstanceDto> aside)
        {
            await using var context = _contextBuilder();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var inTransaction = new Dao<InventoryItemInstance, InventoryItemInstanceDto, Guid>(_logger, () => context);
            if (!await inTransaction.TryInsertOrUpdateAsync(aside) || !await inTransaction.TryInsertOrUpdateAsync(rows))
            {
                await transaction.RollbackAsync();
                return false;
            }

            await transaction.CommitAsync();
            return true;
        }
    }
}
