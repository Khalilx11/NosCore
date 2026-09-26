//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.GameObject.Services.InventoryService;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.Enumerations;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosCore.GameObject.Tests.Services.SaveService
{
    public partial class SaveServiceTests
    {
        private const short MainWeapon = (short)EquipmentType.MainWeapon;
        private const short Armor = (short)EquipmentType.Armor;

        private IInventoryService Inventory => Session.Character.InventoryService;

        private IItemInstance Weapon() => ItemProvider.Create(1, 1);

        private InventoryItemInstance Wear(IItemInstance item, short slot)
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(item, Session.Character.CharacterId),
                NoscorePocketType.Wear, slot);
            return RowOf(item);
        }

        private InventoryItemInstance Carry(IItemInstance item)
        {
            Inventory.AddItemToPocket(InventoryItemInstance.Create(item, Session.Character.CharacterId));
            return RowOf(item);
        }

        private InventoryItemInstance RowOf(IItemInstance item) => Inventory.Values.Single(v => v.ItemInstanceId == item.Id);

        private void WearOver(InventoryItemInstance carried, short slot) =>
            Assert.IsNotNull(Inventory.MoveInPocket(carried.Slot, carried.Type, NoscorePocketType.Wear, slot, true));

        private async Task Save()
        {
            await Service.SaveAsync(Session);
        }

        // What the character loads on its next login is what the rows hold, so this is the relog check.
        private void PersistedShouldBeWhatTheCharacterHolds()
        {
            var held = Inventory.Values.ToDictionary(v => v.ItemInstanceId, v => (v.Type, v.Slot));
            var characterId = Session.Character.CharacterId;
            var rows = InventoryDao.Where(r => r.CharacterId == characterId)!.ToList();

            Assert.AreEqual(held.Count, rows.Count, "a row was lost or duplicated");
            CollectionAssert.AreEquivalent(held.ToList(), rows.ToDictionary(r => r.ItemInstanceId, r => (r.Type, r.Slot)).ToList());
            Assert.AreEqual(rows.Count, rows.Select(r => (r.Slot, r.Type)).Distinct().Count(), "two rows share a slot");
            Assert.IsTrue(rows.All(r => r.Slot >= 0), "a row was left on an aside slot");
        }

        private void ShouldBeAt(IItemInstance item, NoscorePocketType type, short slot)
        {
            var row = InventoryDao.Where(r => r.ItemInstanceId == item.Id)!.Single();
            Assert.AreEqual(type, row.Type);
            Assert.AreEqual(slot, row.Slot);
        }

        [TestMethod]
        public async Task WearingAnItemIntoAnEmptySlotIsPersisted()
        {
            var item = Weapon();
            var row = Carry(item);
            await Save();

            WearOver(row, MainWeapon);
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            ShouldBeAt(item, NoscorePocketType.Wear, MainWeapon);
        }

        [TestMethod]
        public async Task WearingAnItemOverAnotherIsPersistedForBoth()
        {
            var worn = Weapon();
            var carried = Weapon();
            Wear(worn, MainWeapon);
            var carriedRow = Carry(carried);
            var bagSlot = carriedRow.Slot;
            await Save();

            WearOver(carriedRow, MainWeapon);
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            ShouldBeAt(carried, NoscorePocketType.Wear, MainWeapon);
            ShouldBeAt(worn, NoscorePocketType.Equipment, bagSlot);
        }

        [TestMethod]
        public async Task ASwapDoesNotDisturbTheRestOfTheBag()
        {
            var worn = Weapon();
            var carried = Weapon();
            var stack = ItemProvider.Create(1012, 40);
            Wear(worn, MainWeapon);
            var carriedRow = Carry(carried);
            Carry(stack);
            await Save();
            var stackBefore = InventoryDao.Where(r => r.ItemInstanceId == stack.Id)!.Single();

            WearOver(carriedRow, MainWeapon);
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            ShouldBeAt(stack, stackBefore.Type, stackBefore.Slot);
        }

        [TestMethod]
        public async Task TheSameSwapBackAndForthIsPersistedEveryTime()
        {
            var a = Weapon();
            var b = Weapon();
            Wear(a, MainWeapon);
            var bRow = Carry(b);
            var bagSlot = bRow.Slot;
            await Save();

            for (var round = 0; round < 3; round++)
            {
                WearOver(RowOf(b), MainWeapon);
                await Save();
                PersistedShouldBeWhatTheCharacterHolds();
                ShouldBeAt(b, NoscorePocketType.Wear, MainWeapon);
                ShouldBeAt(a, NoscorePocketType.Equipment, bagSlot);

                WearOver(RowOf(a), MainWeapon);
                await Save();
                PersistedShouldBeWhatTheCharacterHolds();
                ShouldBeAt(a, NoscorePocketType.Wear, MainWeapon);
                ShouldBeAt(b, NoscorePocketType.Equipment, bagSlot);
            }
        }

        [TestMethod]
        public async Task SwapsInTwoEquipmentSlotsAtOnceArePersisted()
        {
            var wornWeapon = Weapon();
            var wornArmor = Weapon();
            var carriedWeapon = Weapon();
            var carriedArmor = Weapon();
            Wear(wornWeapon, MainWeapon);
            Wear(wornArmor, Armor);
            var carriedWeaponRow = Carry(carriedWeapon);
            var carriedArmorRow = Carry(carriedArmor);
            await Save();

            WearOver(carriedWeaponRow, MainWeapon);
            WearOver(carriedArmorRow, Armor);
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            ShouldBeAt(carriedWeapon, NoscorePocketType.Wear, MainWeapon);
            ShouldBeAt(carriedArmor, NoscorePocketType.Wear, Armor);
        }

        [TestMethod]
        public async Task RemovingTheItemThatWasSwappedInIsPersisted()
        {
            var worn = Weapon();
            var carried = Weapon();
            Wear(worn, MainWeapon);
            var carriedRow = Carry(carried);
            await Save();
            WearOver(carriedRow, MainWeapon);
            await Save();

            Assert.IsNotNull(Inventory.MoveInPocket(MainWeapon, NoscorePocketType.Wear, NoscorePocketType.Equipment));
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            Assert.IsTrue(InventoryDao.Where(r => r.ItemInstanceId == worn.Id || r.ItemInstanceId == carried.Id)!
                .All(r => r.Type == NoscorePocketType.Equipment));
        }

        [TestMethod]
        public async Task ASwapSavedWithOtherInventoryChangesKeepsThemAll()
        {
            var worn = Weapon();
            var carried = Weapon();
            var dropped = ItemProvider.Create(1013, 1);
            var picked = ItemProvider.Create(1012, 5);
            Wear(worn, MainWeapon);
            var carriedRow = Carry(carried);
            var droppedRow = Carry(dropped);
            await Save();

            WearOver(carriedRow, MainWeapon);
            Inventory.DeleteById(droppedRow.ItemInstanceId);
            Carry(picked);
            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            Assert.AreEqual(0, InventoryDao.Where(r => r.ItemInstanceId == dropped.Id)!.Count());
            Assert.AreEqual(1, InventoryDao.Where(r => r.ItemInstanceId == picked.Id)!.Count());
        }

        [TestMethod]
        public async Task AnInventoryWithoutAnySwapIsPersistedAsBefore()
        {
            var worn = Weapon();
            Wear(worn, MainWeapon);
            Carry(Weapon());
            Carry(ItemProvider.Create(1012, 3));

            await Save();

            PersistedShouldBeWhatTheCharacterHolds();
            ShouldBeAt(worn, NoscorePocketType.Wear, MainWeapon);
        }
    }
}
