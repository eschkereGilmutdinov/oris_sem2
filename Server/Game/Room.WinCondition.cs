using Common;
using Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public partial class Room
    {
        private static bool IsUnicornType(CardType t) =>
            t == CardType.BabyUnicornCard ||
            t == CardType.BasicUnicornCard ||
            t == CardType.MagicalUnicornCard;

        private int CountUnicornsInStall_NoLock(string playerId)
        {
            if (!_stalls.TryGetValue(playerId, out var stall) || stall.Count == 0)
                return 0;

            int cnt = 0;
            foreach (var ci in stall)
            {
                if (_cardById.TryGetValue(ci.CardId, out var def) && IsUnicornType(def.Type))
                    cnt++;
            }
            return cnt;
        }

        private string? FindWinnerByUnicorns_NoLock()
        {
            foreach (var c in _clients)
            {
                var pid = c.Player.PlayerId;
                if (CountUnicornsInStall_NoLock(pid) >= 7)
                    return pid;
            }
            return null;
        }

        private async Task BroadcastGameResultAsync(string winnerId)
        {
            ClientConn[] snapshot;
            string winnerNick;

            lock (_locker)
            {
                snapshot = _clients.ToArray();
                winnerNick = _clients.FirstOrDefault(c => c.Player.PlayerId == winnerId)?.Player.Nickname ?? winnerId;
            }

            var msg = new
            {
                type = "GAME_RESULT",
                payload = new
                {
                    message = $"{winnerNick} победил",
                    winnerId,
                    winnerNick
                }
            };

            foreach (var c in snapshot)
            {
                try { await Protocol.WriteJsonAsync(c.Stream, msg); } catch { }
            }
        }
    }
}
