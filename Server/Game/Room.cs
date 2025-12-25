using System.Net.Sockets;
using System.Text.Json;
using Common;
using Server.Models;
using Server.Networking;

namespace Server.Game;

public partial class Room
{
    private enum TurnPhase
    {
        MustDraw,
        AfterFirstDraw,
        Discarding
    }

    private TurnPhase _phase = TurnPhase.MustDraw;
    private bool _playedThisTurn = false;
    private int _drawsThisTurn = 0;

    private readonly object _locker = new();
    private readonly List<ClientConn> _clients = new();

    private string? _currentTurnPlayerId;
    private int _turnCounter;
    private bool _gameStarted;

    private List<CardInstance> _mainDeck = new();
    private List<CardInstance> _babyDeck = new();
    private readonly List<CardInstance> _discardPile = new();

    private Dictionary<string, List<CardInstance>> _hands = new();
    private Dictionary<string, List<CardInstance>> _stalls = new();

    private readonly Dictionary<string, CardDefinition> _cardById;
    public Dictionary<string, CardDefinition> CardById => _cardById;

    private readonly CardCatalog _catalog;

    public Room()
    {
        _catalog = CardLoader.LoadFromFile("Assets/cards.json");
        _cardById = _catalog.Cards.ToDictionary(c => c.Id, c => c);
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
                JsonDocument incDoc;
                try
                {
                    var incoming = await Protocol.ReadJsonAsync(stream);
                    incDoc = JsonDocument.Parse(incoming);
                }
                catch
                {
                    break; // disconnect
                }

                using (incDoc)
                {
                    if (!TryGetMessageType(incDoc, out var msgType) || msgType != "ACTION")
                        continue;

                    if (!TryGetPayload(incDoc, out var payload))
                        continue;

                    if (!TryGetAction(payload, out var action))
                    {
                        await SendErrorAsync(stream, "Не указано действие (payload.action).");
                        continue;
                    }

                    if (!IsGameReady())
                    {
                        await SendErrorAsync(stream, "Игра ещё не началась");
                        continue;
                    }

                    if (action.Equals("GET_HAND", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleGetHandAsync(me, stream);
                        continue;
                    }

                    if (!IsPlayersTurn(me))
                    {
                        await SendErrorAsync(stream, "Не ваш ход");
                        continue;
                    }

                    if (action.Equals("DRAW", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDrawAsync(me, stream);
                        continue;
                    }

                    if (action.Equals("DRAW_BABY", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendErrorAsync(stream, "Малышей из яслей можно брать только эффектами карт.");
                        continue;
                    }

                    if (action.Equals("PLAY_CARD", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandlePlayCardAsync(me, stream, payload);
                        continue;
                    }

                    if (action.Equals("DISCARD", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleDiscardAsync(me, stream, payload);
                        continue;
                    }

                    await SendErrorAsync(stream, $"Неизвестное действие: {action}");
                }
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            await RemoveClientAsync(me);
        }
    }

    private bool IsPlayersTurn(ClientConn me)
    {
        lock (_locker)
        {
            return me.Player.PlayerId == _currentTurnPlayerId;
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
}