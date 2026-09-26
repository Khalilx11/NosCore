//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;

namespace NosCore.Data.Enumerations.Map
{
    // The bits of a map grid cell the game reads. The client checks exactly these two; the
    // other bits of the byte are carried along and not interpreted.
    [Flags]
    public enum MapCellFlags : byte
    {
        WalkingDisabled = 0x01,
        MonsterAggroDisabled = 0x08,
    }
}
