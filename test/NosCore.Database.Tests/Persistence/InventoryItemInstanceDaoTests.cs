//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Dao;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Database.Persistence;
using NosCore.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entity = NosCore.Database.Entities.InventoryItemInstance;

namespace NosCore.Database.Tests.Persistence
{
    [TestClass]
    public class InventoryItemInstanceDaoTests
    {
        private const NoscorePocketType Worn = NoscorePocketType.Wear;
        private const NoscorePocketType Bag = NoscorePocketType.Equipment;
        private const long Owner = 5;

        private DbContextOptions<NosCoreContext> _options = null!;
        private IDao<InventoryItemInstanceDto, Guid> _dao = null!;

        [TestInitialize]
        public void Setup()
        {
            _options = UniqueSlotIndex.Enforced(new DbContextOptionsBuilder<NosCoreContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())).Options;
            _dao = new InventoryItemInstanceDao(
                NullLogger<Dao<Entity, InventoryItemInstanceDto, Guid>>.Instance, () => new NosCoreContext(_options));
        }

        private static Guid Id(int n) => Guid.Parse($"00000000-0000-0000-0000-{n:D12}");

        private static InventoryItemInstanceDto Row(int n, short slot, NoscorePocketType type, long owner = Owner) => new()
        {
            Id = Id(n), CharacterId = owner, ItemInstanceId = Id(1000 + n), Slot = slot, Type = type,
        };

        private async Task Store(params InventoryItemInstanceDto[] rows) => Assert.IsTrue(await _dao.TryInsertOrUpdateAsync(rows));

        private List<(int Row, NoscorePocketType Type, short Slot)> Stored() =>
            _dao.LoadAll().OrderBy(r => r.Type).ThenBy(r => r.Slot)
                .Select(r => ((int)r.Id.ToByteArray()[15], r.Type, r.Slot)).ToList();

        private static List<(int Row, NoscorePocketType Type, short Slot)> Expect(
            params (int Row, NoscorePocketType Type, short Slot)[] rows) =>
            rows.OrderBy(r => r.Type).ThenBy(r => r.Slot).ToList();

        [TestMethod]
        public void OnlyTheInventoryRowsUseTheSwapSafeDao()
        {
            Assert.AreEqual(typeof(InventoryItemInstanceDao),
                Hosting.PersistenceModule.DaoTypeFor(typeof(Entity), typeof(InventoryItemInstanceDto), typeof(Guid)));
            Assert.AreEqual(typeof(Dao<NosCore.Database.Entities.Account, AccountDto, long>),
                Hosting.PersistenceModule.DaoTypeFor(typeof(NosCore.Database.Entities.Account), typeof(AccountDto), typeof(long)));
        }

        [TestMethod]
        public async Task TheGenericDaoCannotWriteTwoRowsThatTradePlaces()
        {
            var generic = new Dao<Entity, InventoryItemInstanceDto, Guid>(
                NullLogger<Dao<Entity, InventoryItemInstanceDto, Guid>>.Instance, () => new NosCoreContext(_options));
            await Store(Row(1, 1, Worn), Row(2, 0, Bag));

            var saved = await generic.TryInsertOrUpdateAsync(new[] { Row(1, 0, Bag), Row(2, 1, Worn) });

            Assert.IsFalse(saved);
            CollectionAssert.AreEqual(Expect((1, Worn, 1), (2, Bag, 0)), Stored());
        }

        [TestMethod]
        public async Task ARowMovedIntoAnEmptySlotIsSaved()
        {
            await Store(Row(1, 0, Bag));

            await Store(Row(1, 0, Worn));

            CollectionAssert.AreEqual(Expect((1, Worn, 0)), Stored());
        }

        [TestMethod]
        public async Task TwoRowsTradingPlacesAreBothSaved()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag));

            await Store(Row(1, 0, Bag), Row(2, 1, Worn));

            CollectionAssert.AreEqual(Expect((1, Bag, 0), (2, Worn, 1)), Stored());
        }

        [TestMethod]
        public async Task NeitherRowIsLeftOnAnAsideSlotOrDuplicated()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag));

            await Store(Row(1, 0, Bag), Row(2, 1, Worn));

            var rows = _dao.LoadAll().ToList();
            Assert.AreEqual(2, rows.Count);
            Assert.IsTrue(rows.All(r => r.Slot >= 0));
            Assert.AreEqual(2, rows.Select(r => r.Id).Distinct().Count());
            Assert.AreEqual(2, rows.Select(r => (r.CharacterId, r.Slot, r.Type)).Distinct().Count());
        }

        [TestMethod]
        public async Task TheSameTwoRowsCanTradePlacesAgainAndAgain()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag));

            for (var i = 0; i < 4; i++)
            {
                await Store(Row(1, 0, Bag), Row(2, 1, Worn));
                CollectionAssert.AreEqual(Expect((1, Bag, 0), (2, Worn, 1)), Stored());
                await Store(Row(1, 1, Worn), Row(2, 0, Bag));
                CollectionAssert.AreEqual(Expect((1, Worn, 1), (2, Bag, 0)), Stored());
            }
        }

        [TestMethod]
        public async Task SeveralPairsTradingPlacesInOneSaveAreAllSaved()
        {
            await Store(Row(1, 0, Worn), Row(2, 0, Bag), Row(3, 1, Worn), Row(4, 1, Bag), Row(5, 2, Bag));

            await Store(Row(1, 0, Bag), Row(2, 0, Worn), Row(3, 1, Bag), Row(4, 1, Worn), Row(5, 2, Bag));

            CollectionAssert.AreEqual(
                Expect((1, Bag, 0), (2, Worn, 0), (3, Bag, 1), (4, Worn, 1), (5, Bag, 2)), Stored());
        }

        [TestMethod]
        public async Task ThreeRowsRotatingAreAllSaved()
        {
            await Store(Row(1, 0, Bag), Row(2, 1, Bag), Row(3, 2, Bag));

            await Store(Row(1, 1, Bag), Row(2, 2, Bag), Row(3, 0, Bag));

            CollectionAssert.AreEqual(Expect((1, Bag, 1), (2, Bag, 2), (3, Bag, 0)), Stored());
        }

        [TestMethod]
        public async Task ASwapSavedTogetherWithUnrelatedChangesKeepsThemAll()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag), Row(3, 4, Bag), Row(4, 7, Bag));

            await Store(Row(1, 0, Bag), Row(2, 1, Worn), Row(3, 5, Bag), Row(4, 7, Bag), Row(5, 4, Bag));

            CollectionAssert.AreEqual(
                Expect((1, Bag, 0), (2, Worn, 1), (3, Bag, 5), (4, Bag, 7), (5, Bag, 4)), Stored());
        }

        [TestMethod]
        public async Task ARowOfAnotherCharacterOnTheSameSlotIsLeftAlone()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag), Row(3, 1, Worn, owner: 6));

            await Store(Row(1, 0, Bag), Row(2, 1, Worn));

            var other = _dao.LoadAll().Single(r => r.Id == Id(3));
            Assert.AreEqual(6, other.CharacterId);
            Assert.AreEqual((short)1, other.Slot);
            Assert.AreEqual(Worn, other.Type);
        }

        [TestMethod]
        public async Task ARowRemovedAfterASwapLeavesTheOtherWhereTheSwapPutIt()
        {
            await Store(Row(1, 1, Worn), Row(2, 0, Bag));
            await Store(Row(1, 0, Bag), Row(2, 1, Worn));

            await _dao.TryDeleteAsync(new[] { Id(2) });

            CollectionAssert.AreEqual(Expect((1, Bag, 0)), Stored());
        }

        [TestMethod]
        public async Task RowsThatOnlyShiftOrChangeNothingAreSavedAsBefore()
        {
            await Store(Row(1, 0, Bag), Row(2, 1, Bag), Row(3, 6, Bag));

            await Store(Row(1, 1, Bag), Row(2, 2, Bag), Row(3, 6, Bag));

            CollectionAssert.AreEqual(Expect((1, Bag, 1), (2, Bag, 2), (3, Bag, 6)), Stored());
        }
    }
}
