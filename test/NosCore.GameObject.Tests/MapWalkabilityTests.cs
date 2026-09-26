//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.PathFinder.Heuristic;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class MapWalkabilityTests
    {
        private static GameObject.Map.Map OpenMap(byte width, byte height)
        {
            var data = new byte[4 + width * height];
            data[0] = width;
            data[2] = height;
            return new GameObject.Map.Map { MapId = 1, Data = data };
        }

        [TestMethod]
        public void TheCellsOnTheMapAreWalkable()
        {
            var map = OpenMap(3, 2);

            Assert.IsTrue(map.IsWalkable(0, 0));
            Assert.IsTrue(map.IsWalkable(2, 1));
        }

        [TestMethod]
        public void TheCellsJustPastTheMapEdgeAreNot()
        {
            var map = OpenMap(3, 2);

            Assert.IsFalse(map.IsWalkable(3, 0), "x == width would read the next row's first cell");
            Assert.IsFalse(map.IsWalkable(3, 1), "x == width on the last row would read past the data");
            Assert.IsFalse(map.IsWalkable(0, 2), "y == height would read past the data");
            Assert.IsFalse(map.IsWalkable(-1, 0));
            Assert.IsFalse(map.IsWalkable(0, -1));
        }

        // Every value the imported client maps contain, by whether the walking-disabled bit is set.
        [DataTestMethod]
        [DataRow(0, true)]
        [DataRow(2, true)]
        [DataRow(8, true)]
        [DataRow(10, true)]
        [DataRow(12, true)]
        [DataRow(14, true)]
        [DataRow(16, true)]
        [DataRow(18, true)]
        [DataRow(30, true)]
        [DataRow(1, false)]
        [DataRow(3, false)]
        [DataRow(7, false)]
        [DataRow(9, false)]
        [DataRow(11, false)]
        [DataRow(13, false)]
        [DataRow(15, false)]
        [DataRow(17, false)]
        [DataRow(19, false)]
        [DataRow(31, false)]
        public void ACellIsWalkableUnlessItsWalkingDisabledBitIsSet(int cell, bool walkable)
        {
            var map = OpenMap(2, 1);
            map.Data[4] = (byte)cell;

            Assert.AreEqual(walkable, map.IsWalkable(0, 0));
        }

        [DataTestMethod]
        [DataRow(8, true)]
        [DataRow(9, true)]
        [DataRow(10, true)]
        [DataRow(11, true)]
        [DataRow(31, true)]
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(2, false)]
        [DataRow(16, false)]
        [DataRow(17, false)]
        public void ACellDisablesMonsterAggroWhenItsAggroBitIsSet(int cell, bool disabled)
        {
            var map = OpenMap(2, 1);
            map.Data[4] = (byte)cell;

            Assert.AreEqual(disabled, map.IsMonsterAggroDisabled(0, 0));
        }

        [TestMethod]
        public void ACellPastTheEdgeNeverDisablesAggro()
        {
            Assert.IsFalse(OpenMap(2, 1).IsMonsterAggroDisabled(2, 0));
        }

        [TestMethod]
        public void ThePathfinderStaysOnAMapWhoseEdgeIsOpen()
        {
            var map = OpenMap(8, 8);
            var pathfinder = new GameObject.Services.PathfindingService.PathfindingService(new OctileDistanceHeuristic()).ForMap(map);

            var path = pathfinder.FindPath((1, 1), (6, 7)).ToList();

            Assert.IsTrue(path.Count > 1);
            Assert.AreEqual(((short)6, (short)7), path.Last());
        }
    }
}
