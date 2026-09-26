//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using NosCore.Data.Enumerations.Map;

namespace NosCore.Data.Grid
{
    // The layout of a map's grid bytes: width and height as two little-endian i16, then one flag byte
    // per cell, row by row. The rules on the cell bytes live here so the server and the import agree;
    // the overloads taking a size are for callers that already hold it (the pathfinder asks per cell).
    public static class MapGrid
    {
        public static short Width(byte[] data) => BitConverter.ToInt16(data, 0);

        public static short Height(byte[] data) => BitConverter.ToInt16(data, 2);

        public static bool IsWalkable(byte[] data, int x, int y) => IsWalkable(data, Width(data), Height(data), x, y);

        public static bool IsWalkable(byte[] data, int width, int height, int x, int y) =>
            IsInside(width, height, x, y) && !Has(data, width, x, y, MapCellFlags.WalkingDisabled);

        public static bool IsMonsterAggroDisabled(byte[] data, int x, int y) =>
            IsMonsterAggroDisabled(data, Width(data), Height(data), x, y);

        public static bool IsMonsterAggroDisabled(byte[] data, int width, int height, int x, int y) =>
            IsInside(width, height, x, y) && Has(data, width, x, y, MapCellFlags.MonsterAggroDisabled);

        public static bool IsInside(int width, int height, int x, int y) =>
            x >= 0 && y >= 0 && x < width && y < height;

        private static bool Has(byte[] data, int width, int x, int y, MapCellFlags flag) =>
            ((MapCellFlags)data[4 + y * width + x] & flag) != 0;
    }
}
