using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common;

namespace ClientWinForm
{
    public partial class FormRegister : Form
    {
        private readonly int _index;
        private TcpClient? _tcp;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        private List<string> _lastPlayersItems = new();

        private string? _myId;
        private Dictionary<string, string> _nickById = new();
        private List<string> _playerLines = new();

        public FormRegister(int index = 1)
        {
            InitializeComponent();
            _index = index;
        }

        private async void buttonConnect_Click(object? sender, EventArgs e)
        {
            var nick = textBoxNick.Text.Trim();
            var email = textBoxEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(nick) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Введите никнейм и почту");
                return;
            }

            buttonConnect.Enabled = false;

            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync("127.0.0.1", 8080);
                _stream = _tcp.GetStream();

                await Protocol.WriteJsonAsync(_stream, new
                {
                    type = "JOIN",
                    payload = new { nickname = nick, email = email }
                });

                labelLobbyStatus.Text = "Подключено. Ждём остальных игроков...";
                labelLobbyStatus.Visible = true;

                _cts = new CancellationTokenSource();
                _ = ListenAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                try { _tcp?.Close(); } catch { }
                _tcp = null;
                _stream = null;

                MessageBox.Show("Ошибка: " + ex.Message);
                buttonConnect.Enabled = true;
            }
        }

        private async Task ListenAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var json = await Protocol.ReadJsonAsync(_stream!, ct);
                    using var doc = JsonDocument.Parse(json);

                    var root = doc.RootElement;
                    var type = root.GetProperty("type").GetString();

                    if (type == "WELCOME")
                    {
                        _myId = root.GetProperty("payload").GetProperty("yourPlayerId").GetString();
                    }

                    if (type == "JOINED")
                    {
                        var players = root.GetProperty("payload").GetProperty("players");
                        int count = players.GetArrayLength();

                        var map = new Dictionary<string, string>();
                        var lines = new List<string>();

                        var items = new List<string>();
                        foreach (var p in players.EnumerateArray())
                        {
                            var id = p.GetProperty("playerId").GetString() ?? "";
                            var n = p.GetProperty("nickname").GetString() ?? "";
                            var e = p.GetProperty("email").GetString() ?? "";
                            if (!string.IsNullOrWhiteSpace(id))
                                map[id] = n;
                            items.Add($"{n} ({e})");
                        }
                        _lastPlayersItems = items;

                        _nickById = map;
                        _playerLines = items;

                        BeginInvoke(new Action(() =>
                        {
                            labelLobbyStatus.Text = $"Подключилось {count} из 4 игроков. Ждём остальных...";
                            labelLobbyStatus.Visible = true;

                            textBoxNick.Enabled = false;
                            textBoxEmail.Enabled = false;
                            buttonConnect.Enabled = false;
                        }));
                    }
                    else if (type == "START")
                    {
                        BeginInvoke(new Action(() =>
                        {
                            _cts?.Cancel();

                            var game = new FormGame(_tcp!, _stream!, _myId, _nickById, _playerLines);
                            game.FormClosed += (_, __) => this.Close();

                            game.Show();
                            this.Hide();
                        }));
                        return;
                    }
                    else if (type == "ERROR")
                    {
                        var msg = root.GetProperty("payload").GetProperty("message").GetString() ?? "Unknown error";
                        BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + msg)));
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => MessageBox.Show("Ошибка соединения:\n" + ex)));
            }
        }
    }
}
