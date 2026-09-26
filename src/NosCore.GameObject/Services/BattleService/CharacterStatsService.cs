//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using NosCore.GameObject.Ecs.Interfaces;
using NosCore.GameObject.Infastructure;
using NosCore.GameObject.Services.BattleService.Model;
using NosCore.Networking;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Shared.Enumerations;

namespace NosCore.GameObject.Services.BattleService;

public interface ICharacterStatsService
{
    // Tells the character's own client what it currently has: the stats window reads it.
    Task SendAsync(ICharacterEntity character);
}

public sealed class CharacterStatsService(IBattleStatsProvider statsProvider) : ICharacterStatsService, ISingletonService
{
    public Task SendAsync(ICharacterEntity character) =>
        character.SendPacketAsync(ToPacket(statsProvider.GetStats(character), character.Class));

    // The kind of attack each weapon profile makes, as Skill.Type reads: 0 melee, 1 ranged, 2 magic. The
    // rows for the four classes of the original game agree with which weapon each class holds; the martial
    // artist is not settled by anything and is sent as melee on both.
    public static (byte Primary, byte Secondary) WeaponKinds(CharacterClassType characterClass) => characterClass switch
    {
        CharacterClassType.Archer => (1, 0),
        CharacterClassType.Mage => (2, 1),
        CharacterClassType.MartialArtist => (0, 0),
        _ => (0, 1),
    };

    public static ScPacket ToPacket(CombatStats stats, CharacterClassType characterClass)
    {
        var (primary, secondary) = WeaponKinds(characterClass);
        return new ScPacket
        {
            Type = primary,
            MainWeaponUpgrade = (byte)Math.Clamp(stats.MeleeUpgrade, 0, byte.MaxValue),
            MinHit = stats.MinHit,
            MaxHit = stats.MaxHit,
            HitRate = stats.HitRate,
            CriticalHitRate = Short(stats.CriticalChance),
            CriticalHitMultiplier = stats.CriticalRate,
            Type2 = secondary,
            SecondaryWeaponUpgrade = Short(stats.RangedUpgrade),
            SecondaryMinHit = Short(stats.MinDistance),
            SecondaryMaxHit = Short(stats.MaxDistance),
            SecondaryHitRate = Short(stats.DistanceRate),
            SecondaryCriticalHitRate = Short(stats.DistanceCriticalChance),
            SecondaryCriticalHitMultiplier = Short(stats.DistanceCriticalRate),
            ArmorUpgrade = (byte)Math.Clamp(stats.DefenceUpgrade, 0, byte.MaxValue),
            Defence = Short(stats.Defence),
            DefenceRate = Short(stats.DefenceDodge),
            DistanceDefence = Short(stats.DistanceDefence),
            DistanceDefenceRate = Short(stats.DistanceDefenceDodge),
            MagicalDefence = Short(stats.MagicDefence),
            FireResistance = Short(stats.FireResistance),
            WaterResistance = Short(stats.WaterResistance),
            LightResistance = Short(stats.LightResistance),
            DarkResistance = Short(stats.DarkResistance),
        };
    }

    private static short Short(int value) => (short)Math.Clamp(value, short.MinValue, short.MaxValue);
}
