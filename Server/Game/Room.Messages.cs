using Common;
using Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Server.Game
{
    public partial class Room
    {
        private static bool TryGetMessageType(JsonDocument doc, out string type)
        {
            type = "";
            if (!doc.RootElement.TryGetProperty("type", out var t)) return false;
            type = t.GetString() ?? "";
            return true;
        }

        private static bool TryGetPayload(JsonDocument doc, out JsonElement payload)
        {
            payload = default;
            return doc.RootElement.TryGetProperty("payload", out payload);
        }

        private static bool TryGetAction(JsonElement payload, out string action)
        {
            action = "";
            if (!payload.TryGetProperty("action", out var a)) return false;
            action = a.GetString() ?? "";
            return !string.IsNullOrWhiteSpace(action);
        }

        private static Task SendErrorAsync(NetworkStream stream, string message)
        {
            return Protocol.WriteJsonAsync(stream, new
            {
                type = "ERROR",
                payload = new { message }
            });
        }

        private async Task BroadcastJoinedAsync()
        {
            ClientConn[] snapshot;
            object[] players;

            lock (_locker)
            {
                snapshot = _clients.ToArray();
                players = _clients.Select(c => new
                {
                    playerId = c.Player.PlayerId,
                    nickname = c.Player.Nickname,
                    email = c.Player.Email
                }).Cast<object>().ToArray();
            }

            var msg = new { type = "JOINED", payload = new { players } };
            foreach (var c in snapshot)
            {
                try { await Protocol.WriteJsonAsync(c.Stream, msg); } catch { }
            }
        }

        private async Task BroadcastStateAsync()
        {
            ClientConn[] snapshot;
            string? turn;
            int turnNumber;
            string phase;
            object? discardTop = null;

            lock (_locker)
            {
                snapshot = _clients.ToArray();
                turn = _currentTurnPlayerId;
                turnNumber = _turnCounter;
                phase = _phase.ToString();

                if (_discardPile.Count > 0)
                {
                    var top = _discardPile[^1];
                    discardTop = new { instanceId = top.InstanceId, cardId = top.CardId };
                }
            }

            var state = new
            {
                type = "STATE",
                payload = new
                {
                    turn,
                    turnNumber,
                    phase,
                    discardTop
                }
            };

            foreach (var c in snapshot)
            {
                try { await Protocol.WriteJsonAsync(c.Stream, state); } catch { }
            }
        }

        private async Task BroadcastStallAsync(string playerId)
        {
            ClientConn[] snapshot;
            object[] cards;

            lock (_locker)
            {
                snapshot = _clients.ToArray();
                cards = _stalls[playerId]
                    .Select(ci => new { instanceId = ci.InstanceId, cardId = ci.CardId })
                    .Cast<object>()
                    .ToArray();
            }

            var msg = new
            {
                type = "STALL",
                payload = new { playerId, cards }
            };

            foreach (var c in snapshot)
            {
                try { await Protocol.WriteJsonAsync(c.Stream, msg); } catch { }
            }
        }
    }
}
