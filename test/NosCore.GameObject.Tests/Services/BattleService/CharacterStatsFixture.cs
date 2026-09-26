//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using NosCore.Algorithm.CloseDefenceService;
using NosCore.Algorithm.DamageService;
using NosCore.Algorithm.DistanceDefenceService;
using NosCore.Algorithm.DistanceDodgeService;
using NosCore.Algorithm.HitDodgeService;
using NosCore.Algorithm.HitRateService;
using NosCore.Algorithm.MagicDefenceService;
using NosCore.Algorithm.SecondaryDamageService;
using NosCore.Algorithm.SecondaryHitRateService;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Tests.Services.BattleService
{
    public static class CharacterStatsFixture
    {
        public static ICharacterStatsCatalog Catalog() => new CharacterStatsCatalog(
            new DamageService(), new SecondaryDamageService(), new HitRateService(), new SecondaryHitRateService(),
            new CloseDefenceService(), new DistanceDefenceService(), new MagicDefenceService(),
            new HitDodgeService(), new DistanceDodgeService());
    }
}
