// __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

namespace NosCore.GameObject.Services.BattleService;

public record HitArguments()
{
    public short? MapX { get; set; }
    public short? MapY { get; set; }
    // A character's cast id; for a monster or NPC the vnum of one of its skills, 0 being its basic attack.
    public long SkillId { get; set; }
}
