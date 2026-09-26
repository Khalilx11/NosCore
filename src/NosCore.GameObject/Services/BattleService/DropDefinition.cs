//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Data.StaticEntities;

namespace NosCore.GameObject.Services.BattleService;

// One item a monster can drop. The data states the chance in hundred-thousandths, so 14000 is 14%
// and 1750 is 1.75%; the map's drop rate multiplies it.
public sealed record DropDefinition(short ItemVNum, int Amount, int Chance)
{
    public const int ChanceScale = 100_000;

    public static DropDefinition From(DropDto drop) => new(drop.VNum, drop.Amount, drop.DropChance);

    // The roll is a whole number in [0, ChanceScale).
    public bool Lands(int roll, int dropRate) => roll < (long)Chance * dropRate;
}
