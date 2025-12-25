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
        private async Task HandleGetHandAsync(ClientConn me, NetworkStream stream)
        {
            object[] cards;

            lock (_locker)
            {
                if (!_hands.TryGetValue(me.Player.PlayerId, out var hand))
                    hand = new List<CardInstance>();

                cards = hand
                    .Select(ci => new { instanceId = ci.InstanceId, cardId = ci.CardId })
                    .Cast<object>()
                    .ToArray();
            }

            await Protocol.WriteJsonAsync(stream, new
            {
                type = "HAND",
                payload = new { cards }
            });
        }

        private async Task HandleDrawAsync(ClientConn me, NetworkStream stream)
        {
            CardInstance? drawn = null;
            bool allowed;
            bool empty;

            lock (_locker)
            {
                allowed = (_phase == TurnPhase.MustDraw) ||
                          (_phase == TurnPhase.AfterFirstDraw && !_playedThisTurn && _drawsThisTurn == 1);

                empty = _mainDeck.Count == 0;

                if (allowed && !empty)
                {
                    var top = _mainDeck[0];
                    _mainDeck.RemoveAt(0);

                    if (!_hands.TryGetValue(me.Player.PlayerId, out var hand))
                        _hands[me.Player.PlayerId] = hand = new List<CardInstance>();

                    hand.Add(top);
                    drawn = top;

                    _drawsThisTurn++;
                    _phase = (_drawsThisTurn == 1) ? TurnPhase.AfterFirstDraw : TurnPhase.Discarding;
                }
            }

            if (!allowed)
            {
                await SendErrorAsync(stream, "Сейчас нельзя брать карту. В начале хода обязателен 1 добор, затем либо 2-я карта, либо розыгрыш.");
                return;
            }

            if (empty || drawn == null)
            {
                await SendErrorAsync(stream, "Колода пуста");
                return;
            }

            await Protocol.WriteJsonAsync(stream, new
            {
                type = "CARD_DRAWN",
                payload = new { instanceId = drawn.InstanceId, cardId = drawn.CardId }
            });

            if (_drawsThisTurn == 1)
            {
                await BroadcastStateAsync();
                return;
            }

            await MaybeEnterDiscardPhaseOrAdvanceAsync();
        }

        private async Task HandleDrawBabyAsync(ClientConn me, NetworkStream stream)
        {
            CardInstance? drawn = null;
            bool allowed;
            bool empty;

            lock (_locker)
            {
                allowed = (_phase == TurnPhase.AfterFirstDraw && !_playedThisTurn && _drawsThisTurn == 1);
                empty = _babyDeck.Count == 0;

                if (allowed && !empty)
                {
                    var top = _babyDeck[0];
                    _babyDeck.RemoveAt(0);

                    if (!_hands.TryGetValue(me.Player.PlayerId, out var hand))
                        _hands[me.Player.PlayerId] = hand = new List<CardInstance>();

                    hand.Add(top);
                    drawn = top;

                    _drawsThisTurn++;
                    _phase = TurnPhase.Discarding;
                }
            }

            if (!allowed)
            {
                await SendErrorAsync(stream, "Малыша можно взять только после обязательного добора: затем либо 2-я карта (малыш), либо розыгрыш.");
                return;
            }

            if (empty || drawn == null)
            {
                await SendErrorAsync(stream, "Колода малышей пуста");
                return;
            }

            await Protocol.WriteJsonAsync(stream, new
            {
                type = "CARD_DRAWN",
                payload = new { instanceId = drawn.InstanceId, cardId = drawn.CardId }
            });

            await MaybeEnterDiscardPhaseOrAdvanceAsync();
        }

        private async Task HandlePlayCardAsync(ClientConn me, NetworkStream stream, JsonElement payload)
        {
            bool canPlay;
            lock (_locker)
            {
                canPlay = (_phase == TurnPhase.AfterFirstDraw &&
                           _drawsThisTurn == 1 &&
                           !_playedThisTurn);
            }

            if (!canPlay)
            {
                await SendErrorAsync(stream, "Нельзя разыграть карту сейчас. Сначала возьмите 1 карту, затем можно взять вторую ИЛИ разыграть карту.");
                return;
            }

            var instanceId = payload.TryGetProperty("instanceId", out var iidEl)
                ? (iidEl.GetString() ?? "")
                : "";

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                await SendErrorAsync(stream, "Не передан instanceId карты");
                return;
            }

            CardInstance? played = null;
            CardDefinition? playedDef = null;
            bool foundInHand = false;

            lock (_locker)
            {
                if (_hands.TryGetValue(me.Player.PlayerId, out var hand))
                {
                    played = hand.FirstOrDefault(x => x.InstanceId == instanceId);
                    if (played != null)
                    {
                        foundInHand = true;

                        _cardById.TryGetValue(played.CardId, out playedDef);
                    }
                }
            }

            if (played == null)
            {
                await SendErrorAsync(stream, "Этой карты нет в вашей руке");
                return;
            }

            if (playedDef == null)
            {
                await SendErrorAsync(stream, "Неизвестная карта");
                return;
            }

            if (!CanPlayCard(me.Player.PlayerId, playedDef))
            {
                await SendErrorAsync(stream, "Вы не можете разыграть эту карту из-за эффектов на столе");
                return;
            }

            lock (_locker)
            {
                if (!_hands.TryGetValue(me.Player.PlayerId, out var hand))
                {
                    hand = new List<CardInstance>();
                    _hands[me.Player.PlayerId] = hand;
                }

                if (!hand.Remove(played))
                {
                    return;
                }

                if (!_stalls.TryGetValue(me.Player.PlayerId, out var stall))
                    _stalls[me.Player.PlayerId] = stall = new List<CardInstance>();

                stall.Add(played);

                _playedThisTurn = true;
                _phase = TurnPhase.Discarding;
            }

            await ApplyOnPlayEffectsAsync(me.Player.PlayerId, played);

            await BroadcastStallAsync(me.Player.PlayerId);
            await BroadcastStateAsync();
            await AdvanceTurnOrWinAsync(me.Player.PlayerId);
        }

        private async Task HandleDiscardAsync(ClientConn me, NetworkStream stream, JsonElement payload)
        {
            var instanceId = payload.TryGetProperty("instanceId", out var iidEl)
                ? (iidEl.GetString() ?? "")
                : "";

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                await SendErrorAsync(stream, "Не передан instanceId для сброса");
                return;
            }

            CardInstance? removed = null;
            bool nowOk = false;

            lock (_locker)
            {
                if (_phase != TurnPhase.Discarding)
                    return;

                if (!_hands.TryGetValue(me.Player.PlayerId, out var hand))
                    _hands[me.Player.PlayerId] = hand = new List<CardInstance>();

                removed = hand.FirstOrDefault(x => x.InstanceId == instanceId);
                if (removed == null) return;

                hand.Remove(removed);
                _discardPile.Add(removed);

                nowOk = hand.Count <= 7;
                if (nowOk)
                {
                    var idx = _clients.FindIndex(c => c.Player.PlayerId == _currentTurnPlayerId);
                    if (idx < 0) idx = 0;

                    var next = (idx + 1) % _clients.Count;
                    StartTurn_NoLock(_clients[next].Player.PlayerId);
                    _turnCounter++;
                }
            }

            await HandleGetHandAsync(me, stream);
            await BroadcastStateAsync();
        }
    }

}
