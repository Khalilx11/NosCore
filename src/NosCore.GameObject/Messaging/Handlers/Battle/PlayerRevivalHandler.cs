//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NosCore.GameObject.Ecs;
using NosCore.GameObject.Ecs.Extensions;
using NosCore.GameObject.Messaging.Events;
using NosCore.Networking;
using NosCore.Packets.ClientPackets.Event;
using NosCore.Packets.Enumerations;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Shared.Enumerations;
using Microsoft.Extensions.Logging;

namespace NosCore.GameObject.Messaging.Handlers.Battle
{
    // Player death flow: zero HP/MP, apply the dignity penalty, broadcast the
    // refreshed stats, then ask the client which revive branch to take via a `dlgi`
    // dialog. The client routes the yes/no answer back as a `revival` client packet,
    // which RevivalPacketHandler turns into the actual tp + revive + stat sequence.
    //
    // Dialog text id depends on level:
    //   - lvl <= 20: ContinueHereFree (570)      — revive in place is free.
    //   - lvl >  20: ContinueHereTenSeeds (571)  — costs 10x Seed of Power (item 1012).
    //
    // TODO: post-revive protection (card 684, ~30s invuln) isn't granted — the
    // buff/card application layer isn't wired up yet. See death.txt line 6352.
    [UsedImplicitly]
    public sealed class PlayerRevivalHandler(ILogger<PlayerRevivalHandler> logger)
    {
        // Canonical NosTale fork default for dignity penalty on death.
        private const short DignityLossPerDeath = 50;
        private const short DignityFloor = -1000;

        [UsedImplicitly]
        public async Task Handle(EntityDiedEvent evt)
        {
            if (evt.Victim.VisualType != VisualType.Player) return;
            if (evt.Victim is not PlayerComponentBundle player) return;

            if (evt.RevivalMode == RevivalMode.ResumeInPlace)
            {
                // The character was already dead when it was loaded (died, then
                // disconnected before answering the dialog) - the penalty for this
                // death was already applied the first time EntityDiedEvent fired, so
                // re-running the normal body below would charge it a second time and
                // re-send a dialog the player has no fresh reason to answer again.
                // Just wake them up where they are.
                try
                {
                    player.IsAlive = true;
                    player.Hp = 1;
                    player.Mp = 1;

                    await player.SendPacketAsync(player.GenerateStat());
                    await player.SendPacketAsync(player.GenerateFd());
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "PlayerRevivalHandler ResumeInPlace failed for {CharacterId}", player.CharacterId);
                }

                return;
            }

            try
            {
                player.IsAlive = false;
                player.Hp = 0;
                player.Mp = 0;

                player.Dignity = (short)Math.Max(DignityFloor, player.Dignity - DignityLossPerDeath);

                if (evt.Killer is PlayerComponentBundle killer && killer.CharacterId != player.CharacterId
                    && player.Reput >= 50000)
                {
                    var transfer = player.Level * 50L;
                    player.Reput -= transfer;
                    killer.Reput += transfer;
                    await killer.SendPacketAsync(killer.GenerateFd()).ConfigureAwait(false);
                }

                await player.SendPacketAsync(player.GenerateStat()).ConfigureAwait(false);
                await player.SendPacketAsync(player.GenerateFd()).ConfigureAwait(false);

                var question = player.Level > 20
                    ? Game18NConstString.ContinueHereTenSeeds
                    : Game18NConstString.ContinueHereFree;

                await player.SendPacketAsync(new DlgiPacket
                {
                    YesPacket = new RevivalPacket { Type = 0 },
                    NoPacket = new RevivalPacket { Type = 1 },
                    Question = question,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "PlayerRevivalHandler failed for {CharacterId}", player.CharacterId);
            }
        }
    }
}
