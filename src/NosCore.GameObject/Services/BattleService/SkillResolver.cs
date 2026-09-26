//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Collections.Generic;
using System.Linq;
using NosCore.Data.Enumerations.Battle;
using NosCore.Data.StaticEntities;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Services.BattleService.Model;

namespace NosCore.GameObject.Services.BattleService;

// Characters index their skills by CastId (matches client packets). Non-player entities
// pick from the catalog's NpcMonsterSkill table and fall back to BasicSkill when the
// requested cast is not in their roster — this is also what "monster attacks something"
// ends up calling with castId == 0.
public sealed class SkillResolver : ISkillResolver
{
    private readonly IReadOnlyDictionary<short, SkillDto> _skillsByVnum;
    private readonly INpcCombatCatalog _catalog;

    // Bootstrap registration path: gets the pre-loaded List<SkillDto> from the DI
    // container and indexes it here. Tests use the dictionary overload.
    public SkillResolver(List<SkillDto> skills, INpcCombatCatalog catalog)
        : this(skills.ToDictionary(s => s.SkillVNum, s => s), catalog)
    {
    }

    public SkillResolver(IReadOnlyDictionary<short, SkillDto> skillsByVnum, INpcCombatCatalog catalog)
    {
        _skillsByVnum = skillsByVnum;
        _catalog = catalog;
    }

    public SkillInfo? Resolve(IAliveEntity caster, long castId)
    {
        return caster switch
        {
            ICharacterEntity character => ResolveCharacter(character, castId),
            INonPlayableEntity npc => ResolveNpc(npc, castId),
            _ => null,
        };
    }

    private SkillInfo? ResolveCharacter(ICharacterEntity character, long castId)
    {
        // The same skill family can have multiple upgrades; client sends the base CastId
        // and the server picks the highest-tier version the character owns.
        var match = character.Skills.Values
            .Where(s => s.Skill != null && s.Skill.CastId == castId && s.Skill.UpgradeSkill == 0)
            .FirstOrDefault();
        if (match?.Skill == null) return null;

        var upgrade = character.Skills.Values
            .Select(s => s.Skill)
            .Where(s => s != null && s!.UpgradeSkill == match.Skill!.SkillVNum && s.Effect > 0 && s.SkillType == 2)
            .OrderBy(s => s!.SkillVNum)
            .FirstOrDefault();

        return BuildInfo(match.Skill!, upgrade, castId);
    }

    // For a monster or NPC the id is the vnum of one of its own skills, 0 being its basic attack
    // (1043 of the 1108 monster skill links have cast id 0, so a cast id cannot tell its skills apart). The basic attack
    // is built from the monster's own range, area, cooldown and damage; its BasicSkill column is an
    // effect id of the client, not a skill.
    private SkillInfo? ResolveNpc(INonPlayableEntity npc, long skillVnum)
    {
        var mob = npc.NpcMonster;
        if (mob == null) return null;

        if (skillVnum == 0)
        {
            return BasicAttack(mob);
        }

        var linked = _catalog.GetSkills(mob.NpcMonsterVNum).Any(l => l.SkillVNum == skillVnum);
        return linked && _skillsByVnum.TryGetValue((short)skillVnum, out var skill)
            ? BuildInfo(skill, null, skillVnum)
            : null;
    }

    private static SkillInfo BasicAttack(NpcMonsterDto mob) => new(
        SkillVnum: 0,
        CastId: 0,
        Cooldown: mob.BasicCooldown,
        AttackAnimation: 0,
        CastEffect: 0,
        Effect: 0,
        Type: mob.AttackClass,
        HitType: TargetHitType.SingleTargetHit,
        Range: mob.BasicRange,
        TargetRange: mob.BasicArea,
        TargetType: 1,
        Element: mob.Element,
        Duration: 0,
        MpCost: 0,
        BCards: System.Array.Empty<BCardDto>());

    private SkillInfo BuildInfo(SkillDto main, SkillDto? upgrade, long castId)
    {
        return new SkillInfo(
            SkillVnum: main.SkillVNum,
            CastId: castId,
            Cooldown: main.Cooldown,
            AttackAnimation: main.AttackAnimation,
            CastEffect: upgrade?.CastEffect ?? main.CastEffect,
            Effect: main.Effect,
            Type: main.Type,
            HitType: (TargetHitType)main.HitType,
            Range: main.Range,
            TargetRange: main.TargetRange,
            TargetType: main.TargetType,
            Element: main.Element,
            Duration: main.Duration,
            MpCost: main.MpCost,
            BCards: _catalog.GetSkillBCards(main.SkillVNum),
            CellPattern: main.CellPattern);
    }
}
