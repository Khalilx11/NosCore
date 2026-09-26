//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Messaging.Events;
using NosCore.GameObject.Services.BattleService;

namespace NosCore.GameObject.Messaging.Handlers.Map
{
    // Entering the world or a map is the moment the character's own client needs its stats.
    [UsedImplicitly]
    public sealed class CharacterStatsEnteredHandler(ICharacterStatsService stats)
    {
        [UsedImplicitly]
        public Task Handle(MapInstanceEnteredEvent evt) => stats.SendAsync(evt.ClientSession.Character);
    }
}
