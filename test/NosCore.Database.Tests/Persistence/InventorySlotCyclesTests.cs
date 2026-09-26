//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Enumerations;
using NosCore.Database.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosCore.Database.Tests.Persistence
{
    [TestClass]
    public class InventorySlotCyclesTests
    {
        private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        private static readonly Guid D = Guid.Parse("00000000-0000-0000-0000-00000000000d");

        private static InventorySlot At(short slot, NoscorePocketType type = NoscorePocketType.Main, long characterId = 5) =>
            new(characterId, slot, type);

        private static IReadOnlyList<Guid> SetAside(Dictionary<Guid, InventorySlot> stored, Dictionary<Guid, InventorySlot> wanted) =>
            InventorySlotCycles.RowsToSetAside(stored, wanted);

        [TestMethod]
        public void NothingMovingSetsNothingAside()
        {
            var rows = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1) };

            Assert.AreEqual(0, SetAside(rows, rows).Count);
        }

        [TestMethod]
        public void TwoRowsTradingPlacesSetOneAside()
        {
            var stored = new Dictionary<Guid, InventorySlot>
                { [A] = At(1, NoscorePocketType.Wear), [B] = At(0, NoscorePocketType.Equipment) };
            var wanted = new Dictionary<Guid, InventorySlot>
                { [A] = At(0, NoscorePocketType.Equipment), [B] = At(1, NoscorePocketType.Wear) };

            var aside = SetAside(stored, wanted);

            Assert.AreEqual(1, aside.Count);
            CollectionAssert.Contains(new[] { A, B }, aside[0]);
        }

        [TestMethod]
        public void ThreeRowsRotatingSetOneAside()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1), [C] = At(2) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(1), [B] = At(2), [C] = At(0) };

            Assert.AreEqual(1, SetAside(stored, wanted).Count);
        }

        [TestMethod]
        public void TwoSeparateSwapsSetOneAsideEach()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1), [C] = At(4), [D] = At(5) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(1), [B] = At(0), [C] = At(5), [D] = At(4) };

            var aside = SetAside(stored, wanted);

            Assert.AreEqual(2, aside.Count);
            Assert.IsTrue(aside.Intersect(new[] { A, B }).Count() == 1);
            Assert.IsTrue(aside.Intersect(new[] { C, D }).Count() == 1);
        }

        [TestMethod]
        public void AChainOfMovesHasNoCycleToBreak()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(1), [B] = At(2) };

            Assert.AreEqual(0, SetAside(stored, wanted).Count);
        }

        [TestMethod]
        public void ASwapNextToAChainSetsOnlyTheSwapAside()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1), [C] = At(4), [D] = At(5) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(1), [B] = At(2), [C] = At(5), [D] = At(4) };

            var aside = SetAside(stored, wanted);

            Assert.AreEqual(1, aside.Count);
            CollectionAssert.Contains(new[] { C, D }, aside[0]);
        }

        [TestMethod]
        public void ARowMovingToAFreeSlotOrANewRowIsNotACycle()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0), [B] = At(1) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(5), [B] = At(1), [C] = At(0) };

            Assert.AreEqual(0, SetAside(stored, wanted).Count);
        }

        [TestMethod]
        public void SlotNumbersOfTwoCharactersDoNotCollide()
        {
            var stored = new Dictionary<Guid, InventorySlot> { [A] = At(0, characterId: 5), [B] = At(1, characterId: 6) };
            var wanted = new Dictionary<Guid, InventorySlot> { [A] = At(1, characterId: 5), [B] = At(0, characterId: 6) };

            Assert.AreEqual(0, SetAside(stored, wanted).Count);
        }

        [TestMethod]
        public void TheSlotsRowsAreSetAsideOnAreNegativeAndKeepTheRowsOwnerAndPocket()
        {
            var stored = At(3, NoscorePocketType.Wear, 9);

            var first = InventorySlotCycles.Neutral(stored, 0);
            var second = InventorySlotCycles.Neutral(stored, 1);

            Assert.IsTrue(first.Slot < 0 && second.Slot < 0);
            Assert.AreNotEqual(first.Slot, second.Slot);
            Assert.AreEqual(9, first.CharacterId);
            Assert.AreEqual(NoscorePocketType.Wear, first.Type);
        }
    }
}
