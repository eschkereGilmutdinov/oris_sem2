using Common;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;

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
            Shown += (_, __) => _ = ListenAsync(_cts.Token);
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

                        BeginInvoke(new Action(() =>
                        {
                            // кто ходит
                            string turnNick = "(неизвестно)";
                            if (turnId != null && _nickById.TryGetValue(turnId, out var n))
                                turnNick = n;

                            _currentTurnId = turnId;
                            labelTurnWho.Text = $"Текущий ход: {turnNick}";
                        }));
                    }
                    else if (type == "CARD_DRAWN")
                    {
                        var cardId = root.GetProperty("payload").GetProperty("cardId").GetString() ?? "";
                        var instanceId = root.GetProperty("payload").GetProperty("instanceId").GetString() ?? "";

                        BeginInvoke(() =>
                        {
                            var img = CardImageLoader.Load(cardId);

                            imageListCards.Images.Add(instanceId, img);

                            var item = new ListViewItem
                            {
                                Text = "",
                                ImageKey = instanceId
                            };
                            item.Tag = cardId;

                            listViewHand.Items.Add(item);
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
        }
    }
}
