//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Collections.Concurrent;
using NosCore.Algorithm.CloseDefenceService;
using NosCore.Algorithm.DamageService;
using NosCore.Algorithm.DistanceDefenceService;
using NosCore.Algorithm.DistanceDodgeService;
using NosCore.Algorithm.HitDodgeService;
using NosCore.Algorithm.HitRateService;
using NosCore.Algorithm.MagicDefenceService;
using NosCore.Algorithm.SecondaryDamageService;
using NosCore.Algorithm.SecondaryHitRateService;
using NosCore.GameObject.Infastructure;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

// What a character of a class and level has before anything is worn or applied: the static side of
// CombatStats. The primary weapon feeds the melee profile, the secondary weapon the ranged one.
public sealed record CharacterStatsDefinition(
    CharacterClassType Class,
    byte Level,
    int MinHit,
    int MaxHit,
    int HitRate,
    int MinDistance,
    int MaxDistance,
    int DistanceRate,
    int Defence,
    int DistanceDefence,
    int MagicDefence,
    int DefenceDodge,
    int DistanceDefenceDodge);

public interface ICharacterStatsCatalog
{
    CharacterStatsDefinition Get(CharacterClassType characterClass, byte level);
}

// The curves live in NosCore.Algorithm; this only gathers them once per class and level.
public sealed class CharacterStatsCatalog(
    IDamageService damage,
    ISecondaryDamageService secondaryDamage,
    IHitRateService hitRate,
    ISecondaryHitRateService secondaryHitRate,
    ICloseDefenceService closeDefence,
    IDistanceDefenceService distanceDefence,
    IMagicDefenceService magicDefence,
    IHitDodgeService hitDodge,
    IDistanceDodgeService distanceDodge) : ICharacterStatsCatalog, ISingletonService
{
    private const byte MinLevel = 1;
    private const byte MaxLevel = 99;

    private readonly ConcurrentDictionary<(CharacterClassType, byte), CharacterStatsDefinition> _definitions = new();

    public CharacterStatsDefinition Get(CharacterClassType characterClass, byte level)
    {
        var clamped = Math.Clamp(level, MinLevel, MaxLevel);
        return _definitions.GetOrAdd((characterClass, clamped), key => Build(key.Item1, key.Item2));
    }

    private CharacterStatsDefinition Build(CharacterClassType characterClass, byte level) => new(
        characterClass,
        level,
        (int)damage.GetMinDamage(characterClass, level),
        (int)damage.GetMaxDamage(characterClass, level),
        (int)hitRate.GetHitRate(characterClass, level),
        (int)secondaryDamage.GetSecondaryMinDamage(characterClass, level),
        (int)secondaryDamage.GetSecondaryMaxDamage(characterClass, level),
        (int)secondaryHitRate.GetSecondaryHitRate(characterClass, level),
        (int)closeDefence.GetCloseDefence(characterClass, level),
        (int)distanceDefence.GetDistanceDefence(characterClass, level),
        (int)magicDefence.GetMagicDefence(characterClass, level),
        (int)hitDodge.GetHitDodge(characterClass, level),
        (int)distanceDodge.GetDistanceDodge(characterClass, level));
}
