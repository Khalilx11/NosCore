//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCore.Data.Dto;

namespace NosCore.GameObject.Tests
{
    [TestClass]
    public class MapTests
    {
        [TestMethod]
        public void WalkingDisabledBitBlocksTheCellRegardlessOfOtherBits()
        {
            // Bit 0 is WALKING_DISABLED; the other bits carry unrelated terrain data
            // and must not affect the verdict. 0x16 (22) has bit 0 clear despite being
            // outside the old {0, 2, 16-19} value list - one of the disagreeing cells
            // the client's own bitmask fix already accounts for.
            var map = TestMap(0x00, 0x01, 0x16, 0x03);

            Assert.IsTrue(map.IsWalkable(0, 0), "bit 0 clear (0x00)");
            Assert.IsFalse(map.IsWalkable(1, 0), "bit 0 set (0x01)");
            Assert.IsTrue(map.IsWalkable(2, 0), "bit 0 clear despite being outside the old value list (0x16)");
            Assert.IsFalse(map.IsWalkable(3, 0), "bit 0 set (0x03)");
        }

        [TestMethod]
        public void CoordinateEqualToWidthOrHeightIsNotWalkable()
        {
            var map = TestMap(0, 0, 0, 0);

            Assert.IsFalse(map.IsWalkable(4, 0), "x == Width must be out of bounds, not an off-by-one pass-through");
            Assert.IsFalse(map.IsWalkable(0, 1), "y == Height must be out of bounds, not an off-by-one pass-through");
        }

        [TestMethod]
        public void InBoundsCoordinatesStillResolveNormally()
        {
            var map = TestMap(0, 1, 0, 0);

            Assert.IsTrue(map.IsWalkable(0, 0));
            Assert.IsFalse(map.IsWalkable(1, 0));
        }

        private static global::NosCore.GameObject.Map.Map TestMap(params byte[] cells)
        {
            var data = new byte[4 + cells.Length];
            data[0] = (byte)cells.Length; // width low byte
            data[2] = 1; // height low byte
            cells.CopyTo(data, 4);

            return new global::NosCore.GameObject.Map.Map
            {
                MapId = 1,
                Data = data,
                Name = new I18NString(),
            };
        }
    }
}
