using System.Net.Sockets;
using System.Text.Json;
using Common;
using Server.Models;
using Server.Networking;

namespace Server.Game;

public class Room
{
    private readonly object _locker = new();
    private readonly List<ClientConn> _clients = new();

    private string? _currentTurnPlayerId;
    private int _turnCounter;
    private bool _gameStarted;

    private List<CardInstance> _deck = new();
    private Dictionary<string, List<CardInstance>> _hands = new();

    private readonly CardCatalog _catalog;

    public Room()
    {
        _catalog = CardLoader.LoadFromFile("Assets/cards.json");
    }

    public async Task<bool> TryJoinAsync(TcpClient tcp)
    {
        var stream = tcp.GetStream();

        // ждём JOIN
        var json = await Protocol.ReadJsonAsync(stream);
        using var doc = JsonDocument.Parse(json);

        var type = doc.RootElement.GetProperty("type").GetString();
        if (!string.Equals(type, "JOIN", StringComparison.OrdinalIgnoreCase))
        {
            await Protocol.WriteJsonAsync(stream, new
            {
                type = "ERROR",
                payload = new { code = "EXPECTED_JOIN", message = "Первое сообщение должно быть JOIN" }
            });
            tcp.Close();
            return false;
        }

        var payload = doc.RootElement.GetProperty("payload");
        var nickname = payload.GetProperty("nickname").GetString() ?? "Unknown";
        var email = payload.GetProperty("email").GetString() ?? "";

        ClientConn me;

        lock (_locker)
        {
            if (_clients.Count >= 4)
            {
                _ = Protocol.WriteJsonAsync(stream, new
                {
                    type = "ERROR",
                    payload = new { code = "ROOM_FULL", message = "Комната заполнена (4 игрока)" }
                });
                tcp.Close();
                return false;
            }

            var player = new Player(Guid.NewGuid().ToString(), nickname, email);
            me = new ClientConn(tcp, stream, player);
            _clients.Add(me);
        }

        Console.WriteLine($"JOIN: {me.Player.Nickname} ({me.Player.Email})");

        await Protocol.WriteJsonAsync(stream, new
        {
            type = "WELCOME",
            payload = new { yourPlayerId = me.Player.PlayerId }
        });

        await BroadcastJoinedAsync();
        await MaybeStartAsync();

        _ = Task.Run(() => ClientLoopAsync(me));
        return true;
    }

    private async Task ClientLoopAsync(ClientConn me)
    {
        var stream = me.Stream;

        try
        {
            while (true)
            {
                var incoming = await Protocol.ReadJsonAsync(stream);
                using var incDoc = JsonDocument.Parse(incoming);

                var msgType = incDoc.RootElement.GetProperty("type").GetString();
                if (msgType != "ACTION") continue;

                var payload = incDoc.RootElement.GetProperty("payload");
                var action = payload.TryGetProperty("action", out var a)
                    ? a.GetString()
                    : null;

                    // проверки
                    if (!IsGameReady())
                    {
                        await Protocol.WriteJsonAsync(stream, new
                        {
                            type = "ERROR",
                            payload = new { message = "Игра ещё не началась" }
                        });
                        continue;
                    }

                    if (me.Player.PlayerId != _currentTurnPlayerId)
                    {
                        await Protocol.WriteJsonAsync(stream, new
                        {
                            type = "ERROR",
                            payload = new { message = "Не ваш ход" }
                        });
                        continue;
                    }

                if (string.Equals(action, "DRAW", StringComparison.OrdinalIgnoreCase))
                {
                    CardInstance? drawn = null;

                    lock (_locker)
                    {
                        if (_deck.Count > 0)
                        {
                            var top = _deck[0];
                            _deck.RemoveAt(0);

                            _hands[me.Player.PlayerId].Add(top);
                            drawn = top;
                        }
                    }

                    if (drawn == null)
                    {
                        await Protocol.WriteJsonAsync(stream, new
                        {
                            type = "ERROR",
                            payload = new { message = "Колода пуста" }
                        });
                        continue;
                    }

                    await Protocol.WriteJsonAsync(stream, new
                    {
                        type = "CARD_DRAWN",
                        payload = new
                        {
                            instanceId = drawn.InstanceId,
                            cardId = drawn.CardId
                        }
                    });

                    bool isWin;
                    lock (_locker)
                    {
                        _turnCounter++;
                        isWin = _turnCounter >= 10;

                        if (!isWin)
                        {
                            var idx = _clients.FindIndex(c => c.Player.PlayerId == _currentTurnPlayerId);
                            if (idx < 0) idx = 0;
                            var next = (idx + 1) % _clients.Count;
                            _currentTurnPlayerId = _clients[next].Player.PlayerId;
                        }
                    }

                    if (isWin) await BroadcastWinAsync(me.Player.PlayerId);
                    else await BroadcastStateAsync();

                    continue;
                }
            }
        }
        catch
        {
            // отключение
        }
        finally
        {
            await RemoveClientAsync(me);
        }
    }

    private bool IsGameReady()
    {
        lock (_locker)
            return _gameStarted && _currentTurnPlayerId != null && _clients.Count == 4;
    }

    private async Task RemoveClientAsync(ClientConn me)
    {
        lock (_locker)
        {
            _clients.RemoveAll(c => c.Player.PlayerId == me.Player.PlayerId);

            if (_currentTurnPlayerId == me.Player.PlayerId)
                _currentTurnPlayerId = _clients.FirstOrDefault()?.Player.PlayerId;
        }

        await BroadcastJoinedAsync();
        try { me.Tcp.Close(); } catch { }

        Console.WriteLine($"Disconnected: {me.Player.Nickname}");
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
            _currentTurnPlayerId = snapshot[0].Player.PlayerId;

            _deck = DeckGenerator.GenerateFullDeck(_catalog);

            _hands.Clear();
            foreach (var c in snapshot)
            {
                _hands[c.Player.PlayerId] = new List<CardInstance>();
                for (int i = 0; i < 5; i++)
                    DrawToHand_NoLock(c.Player.PlayerId);
            }
        }

        var start = new { type = "START", payload = new { firstPlayerId = snapshot[0].Player.PlayerId } };
        foreach (var c in snapshot)
        {
            try { await Protocol.WriteJsonAsync(c.Stream, start); } catch { }
        }

        Console.WriteLine("START sent (4 players connected)");
        await BroadcastStateAsync();
    }

    private void DrawToHand_NoLock(string playerId)
    {
        if (_deck.Count == 0) return;
        var top = _deck[0];
        _deck.RemoveAt(0);
        _hands[playerId].Add(top);
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

        lock (_locker)
        {
            snapshot = _clients.ToArray();
            turn = _currentTurnPlayerId;
            turnNumber = _turnCounter;
        }

        var state = new { type = "STATE", payload = new { turn, turnNumber } };
        foreach (var c in snapshot)
        {
            try { await Protocol.WriteJsonAsync(c.Stream, state); } catch { }
        }
    }

    private async Task BroadcastWinAsync(string winnerId)
    {
        ClientConn[] snapshot;
        lock (_locker) snapshot = _clients.ToArray();

        var win = new { type = "WIN", payload = new { winnerId } };
        foreach (var c in snapshot)
        {
            try { await Protocol.WriteJsonAsync(c.Stream, win); } catch { }
        }

        Console.WriteLine($"WINNER: {winnerId}");
    }
}