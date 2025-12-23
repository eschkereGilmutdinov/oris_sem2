using Common;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Linq;

namespace ClientWinForm
{
    public partial class FormGame : Form
    {
        private readonly TcpClient _tcp;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();

        private string? _myId;
        private readonly Dictionary<string, string> _nickById = new();

        private string? _currentTurnId;

        private readonly Dictionary<string, List<(string cardId, string instanceId)>> _stallByPlayerId = new();

        public FormGame(TcpClient tcp, NetworkStream stream, string? myId, Dictionary<string, string> nickById, List<string>? initialPlayers)
        {
            InitializeComponent();
            _tcp = tcp;
            _stream = stream;
            Text = "Unstable Unicorns";

            _myId = myId;
            _nickById.Clear();
            foreach (var kv in nickById)
                _nickById[kv.Key] = kv.Value;

            if (initialPlayers != null && initialPlayers.Count > 0)
            {
                listBoxPlayers.Items.Clear();
                foreach (var s in initialPlayers)
                    listBoxPlayers.Items.Add(s);
            }

            UpdateMyNickLabel();
            Shown += async (_, __) =>
            {
                _ = ListenAsync(_cts.Token);
                await RequestHandAsync();
            };
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var json = await Protocol.ReadJsonAsync(_stream, ct);

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    var type = root.GetProperty("type").GetString() ?? "";

                    if (type == "WELCOME")
                    {
                        _myId = root.GetProperty("payload").GetProperty("yourPlayerId").GetString();

                        BeginInvoke(new Action(UpdateMyNickLabel));
                    }
                    if (string.Equals(type, "JOINED", StringComparison.OrdinalIgnoreCase))
                    {
                        var playersEl = root.GetProperty("payload").GetProperty("players");

                        var items = new List<string>();
                        var map = new Dictionary<string, string>();

                        foreach (var p in playersEl.EnumerateArray())
                        {
                            var id = p.GetProperty("playerId").GetString() ?? "";
                            var nick = p.GetProperty("nickname").GetString() ?? "";
                            var email = p.GetProperty("email").GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(id))
                                map[id] = nick;
                            items.Add($"{nick} ({email})");
                        }

                        BeginInvoke(() =>
                        {
                            listBoxPlayers.Items.Clear();
                            foreach (var s in items) listBoxPlayers.Items.Add(s);

                            _nickById.Clear();
                            foreach (var kv in map) _nickById[kv.Key] = kv.Value;

                            UpdateMyNickLabel();
                        });
                    }
                    else if (type == "STATE")
                    {
                        var turnId = root.GetProperty("payload").GetProperty("turn").GetString();
                        var turnNum = root.GetProperty("payload").GetProperty("turnNumber").GetInt32();

                        var payload = root.GetProperty("payload");
                        var phase = payload.TryGetProperty("phase", out var ph) ? (ph.GetString() ?? "") : "";

                        string? discardTopCardId = null;
                        if (payload.TryGetProperty("discardTop", out var dt) && dt.ValueKind != JsonValueKind.Null)
                            discardTopCardId = dt.GetProperty("cardId").GetString();

                        BeginInvoke(new Action(() =>
                        {
                            bool myTurn = _myId != null && turnId != null && _myId == turnId;

                            buttonPlayCard.Enabled = myTurn && phase == "AfterFirstDraw";
                            buttonDiscard.Enabled = myTurn && phase == "Discarding";

                            _currentTurnId = turnId;
                            // кто ходит
                            string turnNick = "(неизвестно)";
                            if (turnId != null && _nickById.TryGetValue(turnId, out var n))
                                turnNick = n;

                            _currentTurnId = turnId;
                            labelTurnWho.Text = $"Текущий ход: {turnNick}";

                            if (!string.IsNullOrWhiteSpace(discardTopCardId))
                                pictureBoxDiscard.Image = CardImageLoader.Load(discardTopCardId);
                        }));
                    }
                    else if (type == "CARD_DRAWN")
                    {
                        var cardId = root.GetProperty("payload").GetProperty("cardId").GetString() ?? "";
                        var instanceId = root.GetProperty("payload").GetProperty("instanceId").GetString() ?? "";

                        BeginInvoke(() =>
                        {
                            var img = CardImageLoader.Load(cardId);

                            if (!imageListCards.Images.ContainsKey(instanceId))
                                imageListCards.Images.Add(instanceId, img);

                            listViewHand.Items.Add(new ListViewItem
                            {
                                Text = "",
                                ImageKey = instanceId,
                                Tag = (cardId, instanceId)
                            });
                        });
                    }
                    else if (type == "HAND")
                    {
                        var cardsEl = root.GetProperty("payload").GetProperty("cards");

                        var cards = new List<(string cardId, string instanceId)>();
                        foreach (var c in cardsEl.EnumerateArray())
                        {
                            var cardId = c.GetProperty("cardId").GetString() ?? "";
                            var instanceId = c.TryGetProperty("instanceId", out var iid)
                                ? (iid.GetString() ?? "")
                                : "";
                            cards.Add((cardId, instanceId));
                        }

                        BeginInvoke(() => ShowHandInListView(cards));
                    }
                    else if (type == "ERROR")
                    {
                        var msg = root.GetProperty("payload").GetProperty("message").GetString() ?? "Unknown error";
                        BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + msg)));
                    }
                    else if (type == "GAME_RESULT")
                    {
                        var msg = root.GetProperty("payload").GetProperty("message").GetString() ?? "";
                        BeginInvoke(() => MessageBox.Show(msg));
                    }
                    else if (type == "STALL")
                    {
                        var payload = root.GetProperty("payload");
                        var playerId = payload.GetProperty("playerId").GetString() ?? "";
                        var cardsEl = payload.GetProperty("cards");

                        var cards = new List<(string cardId, string instanceId)>();
                        foreach (var c in cardsEl.EnumerateArray())
                        {
                            var cardId = c.GetProperty("cardId").GetString() ?? "";
                            var instanceId = c.GetProperty("instanceId").GetString() ?? "";
                            cards.Add((cardId, instanceId));
                        }

                        BeginInvoke(() =>
                        {
                            _stallByPlayerId[playerId] = cards;
                            UpdateAllStallsUI();
                        });
                    }
                    else
                    {
                        BeginInvoke(new Action(() =>
                        {
                        }));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // норм
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("ListenAsync упал:\n" + ex);
                    }));
                }
            }
        }

        private async void pictureBoxDeck_Click(object? sender, EventArgs e)
        {
            if (_myId == null || _currentTurnId == null || _myId != _currentTurnId)
            {
                MessageBox.Show("Сейчас не ваш ход");
                return;
            }

            try
            {
                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "ACTION",
                    payload = new { action = "DRAW" }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось отправить запрос: " + ex.Message);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _cts.Cancel();
            base.OnFormClosed(e);
        }

        private void UpdateMyNickLabel()
        {
            if (labelMyNick == null) return;

            if (_myId != null && _nickById.TryGetValue(_myId, out var myNick))
                labelMyNick.Text = $"Твой никнейм: {myNick}";
            else
                labelMyNick.Text = "Твой никнейм: (неизвестно)";

            UpdateOtherPlayersLabels();
            UpdateAllStallsUI();
        }

        private void UpdateOtherPlayersLabels()
        {
            if (nickname1 == null || nickname2 == null || nickname3 == null)
                return;

            string? myNick = null;
            if (_myId != null && _nickById.TryGetValue(_myId, out var n))
                myNick = n;

            var allNicks = _nickById.Values
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            allNicks.Sort((a, b) =>
            {
                var ai = int.TryParse(a, out var an);
                var bi = int.TryParse(b, out var bn);
                if (ai && bi) return an.CompareTo(bn);
                if (ai) return -1;
                if (bi) return 1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            if (!string.IsNullOrWhiteSpace(myNick))
                allNicks.RemoveAll(x => string.Equals(x, myNick, StringComparison.Ordinal));

            var labels = new[] { nickname1, nickname2, nickname3 };
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].Text = i < allNicks.Count ? allNicks[i] : "";
            }
        }

        private void UpdateAllStallsUI()
        {
            if (_myId != null && _stallByPlayerId.TryGetValue(_myId, out var myCards))
                ShowStallInListView(myCards, removeFromHand: true);
            else
                ShowStallInListView(new List<(string cardId, string instanceId)>(), removeFromHand: false);

            UpdateOtherPlayersStallsUI();
        }

        private void UpdateOtherPlayersStallsUI()
        {
            var map = new (Label nickLabel, ListView view)[]
            {
                (nickname1, listView1),
                (nickname2, listView2),
                (nickname3, listView3),
            };

            foreach (var (nickLabel, view) in map)
            {
                var nick = nickLabel.Text?.Trim();

                if (string.IsNullOrWhiteSpace(nick))
                {
                    ClearListView(view);
                    continue;
                }

                var playerId = GetPlayerIdByNick(nick);
                if (playerId == null)
                {
                    ClearListView(view);
                    continue;
                }

                if (_stallByPlayerId.TryGetValue(playerId, out var cards))
                    ShowStallInListView(view, cards);
                else
                    ClearListView(view);
            }
        }

        private string? GetPlayerIdByNick(string nick)
        {
            foreach (var kv in _nickById)
                if (string.Equals(kv.Value, nick, StringComparison.Ordinal))
                    return kv.Key;

            return null;
        }

        private void ClearListView(ListView view)
        {
            view.BeginUpdate();
            view.Items.Clear();
            view.EndUpdate();
        }

        private void ShowHandInListView(List<(string cardId, string instanceId)> cards)
        {
            listViewHand.BeginUpdate();
            listViewHand.Items.Clear();

            foreach (var (cardId, instanceIdRaw) in cards)
            {
                if (string.IsNullOrWhiteSpace(instanceIdRaw))
                    continue;

                var img = CardImageLoader.Load(cardId);
                var key = instanceIdRaw;

                if (!imageListCards.Images.ContainsKey(key))
                    imageListCards.Images.Add(key, img);

                listViewHand.Items.Add(new ListViewItem
                {
                    Text = "",
                    ImageKey = key,
                    Tag = (cardId, key)
                });
            }

            listViewHand.EndUpdate();
        }

        private void ShowStallInListView(List<(string cardId, string instanceId)> cards, bool removeFromHand)
        {
            listViewStall.LargeImageList = imageListCards;
            listViewStall.View = View.LargeIcon;

            listViewStall.BeginUpdate();
            listViewStall.Items.Clear();

            foreach (var (cardId, instanceId) in cards)
            {
                var img = CardImageLoader.Load(cardId);

                if (!imageListCards.Images.ContainsKey(instanceId))
                    imageListCards.Images.Add(instanceId, img);

                listViewStall.Items.Add(new ListViewItem
                {
                    Text = "",
                    ImageKey = instanceId,
                    Tag = (cardId, instanceId)
                });

                if (removeFromHand)
                {
                    for (int i = listViewHand.Items.Count - 1; i >= 0; i--)
                    {
                        var t = ((string cardId2, string instanceId2))listViewHand.Items[i].Tag!;
                        if (t.instanceId2 == instanceId)
                            listViewHand.Items.RemoveAt(i);
                    }
                }
            }

            listViewStall.EndUpdate();
        }

        private void ShowStallInListView(ListView target, List<(string cardId, string instanceId)> cards)
        {
            target.LargeImageList = imageListCards;
            target.View = View.LargeIcon;

            target.BeginUpdate();
            target.Items.Clear();

            foreach (var (cardId, instanceId) in cards)
            {
                var img = CardImageLoader.Load(cardId);

                if (!imageListCards.Images.ContainsKey(instanceId))
                    imageListCards.Images.Add(instanceId, img);

                target.Items.Add(new ListViewItem
                {
                    Text = "",
                    ImageKey = instanceId,
                    Tag = (cardId, instanceId)
                });
            }

            target.EndUpdate();
        }

        private async Task RequestHandAsync()
        {
            try
            {
                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "ACTION",
                    payload = new { action = "GET_HAND" }
                });
            }
            catch
            {
            }
        }

        private async void buttonPlayCard_Click(object sender, EventArgs e)
        {
            if (listViewHand.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выбери карту в руке.");
                return;
            }

            var item = listViewHand.SelectedItems[0];
            var (cardId, instanceId) = ((string cardId, string instanceId))item.Tag!;

            try
            {
                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "ACTION",
                    payload = new { action = "PLAY_CARD", instanceId }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отправки: " + ex.Message);
            }
        }

        private async void pictureBoxBabyDeck_Click(object? sender, EventArgs e)
        {
            if (_myId == null || _currentTurnId == null || _myId != _currentTurnId)
            {
                MessageBox.Show("Сейчас не ваш ход");
                return;
            }

            try
            {
                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "ACTION",
                    payload = new { action = "DRAW_BABY" }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось отправить запрос: " + ex.Message);
            }
        }

        private async void buttonDiscard_Click(object sender, EventArgs e)
        {
            if (listViewHand.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выбери карту для сброса.");
                return;
            }

            var item = listViewHand.SelectedItems[0];
            var (_, instanceId) = ((string cardId, string instanceId))item.Tag!;

            try
            {
                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "ACTION",
                    payload = new { action = "DISCARD", instanceId }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отправки: " + ex.Message);
            }
        }
    }
}
