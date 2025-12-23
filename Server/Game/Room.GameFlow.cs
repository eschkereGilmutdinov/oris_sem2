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
        private void StartTurn_NoLock(string playerId)
        {
            _currentTurnPlayerId = playerId;
            _phase = TurnPhase.MustDraw;
            _drawsThisTurn = 0;
            _playedThisTurn = false;
        }

        private async Task MaybeStartAsync()
        {
            ClientConn[] snapshot;
            lock (_locker)
            {
                if (_clients.Count != 4) return;

                snapshot = _clients.ToArray();
                _gameStarted = true;
                _turnCounter = 0;
                StartTurn_NoLock(snapshot[0].Player.PlayerId);

                var (mainDeck, babyDeck) = DeckGenerator.GenerateMainAndBabyDeck(_catalog);
                _mainDeck = mainDeck;
                _babyDeck = babyDeck;

                _hands = new Dictionary<string, List<CardInstance>>();
                _stalls = new Dictionary<string, List<CardInstance>>();

                foreach (var c in snapshot)
                {
                    var pid = c.Player.PlayerId;
                    _hands[pid] = new List<CardInstance>();
                    _stalls[pid] = new List<CardInstance>();

                    // стартовая раздача: 1 малыш + 4 из основной колоды
                    DrawBabyToHand_NoLock(pid);
                    for (int i = 0; i < 4; i++)
                        DrawToHand_NoLock(pid);
                }
            }

            var start = new { type = "START", payload = new { firstPlayerId = snapshot[0].Player.PlayerId } };
            foreach (var c in snapshot)
            {
                try { await Protocol.WriteJsonAsync(c.Stream, start); } catch { }
            }

            Console.WriteLine("START sent (4 players connected)");
            await BroadcastStateAsync();

            foreach (var c in snapshot)
                await BroadcastStallAsync(c.Player.PlayerId);
        }

        private void DrawToHand_NoLock(string playerId)
        {
            if (_mainDeck.Count == 0) return;
            var top = _mainDeck[0];
            _mainDeck.RemoveAt(0);
            _hands[playerId].Add(top);
        }

        private void DrawBabyToHand_NoLock(string playerId)
        {
            if (_babyDeck.Count == 0) return;
            var top = _babyDeck[0];
            _babyDeck.RemoveAt(0);
            _hands[playerId].Add(top);
        }

        private async Task MaybeEnterDiscardPhaseOrAdvanceAsync()
        {
            bool needDiscard;
            bool endTurnNow = false;

            lock (_locker)
            {
                if (_currentTurnPlayerId == null) return;

                needDiscard = _hands.TryGetValue(_currentTurnPlayerId, out var hand) && hand.Count > 7;

                if (needDiscard)
                {
                    _phase = TurnPhase.Discarding;
                }
                else
                {
                    if (_phase == TurnPhase.Discarding)
                    {
                        var idx = _clients.FindIndex(c => c.Player.PlayerId == _currentTurnPlayerId);
                        if (idx < 0) idx = 0;

                        var next = (idx + 1) % _clients.Count;
                        StartTurn_NoLock(_clients[next].Player.PlayerId);
                        _turnCounter++;
                        endTurnNow = true;
                    }
                }
            }

            await BroadcastStateAsync();
        }

        private async Task AdvanceTurnOrWinAsync(string actorPlayerId)
        {
            string? winnerId;

            lock (_locker)
            {
                winnerId = FindWinnerByUnicorns_NoLock();
                if (winnerId != null)
                {
                    _gameStarted = false;
                }
                else if (_hands.TryGetValue(actorPlayerId, out var hand) && hand.Count > 7)
                {
                    _phase = TurnPhase.Discarding;
                }
                else
                {
                    var idx = _clients.FindIndex(c => c.Player.PlayerId == _currentTurnPlayerId);
                    if (idx < 0) idx = 0;

                    var next = (idx + 1) % _clients.Count;
                    StartTurn_NoLock(_clients[next].Player.PlayerId);
                    _turnCounter++;
                }
            }

            if (winnerId != null)
                await BroadcastGameResultAsync(winnerId);
            else
                await BroadcastStateAsync();
        }
    }
}
