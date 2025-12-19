using System.Net;
using System.Net.Sockets;
using Server.Game;

namespace Server.Networking;

public class ServerHost
{
    private readonly TcpListener _listener;
    private readonly Room _room;

    public ServerHost(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _room = new Room();
    }

    public async Task RunAsync()
    {
        _listener.Start();
        Console.WriteLine("Server started");

        while (true)
        {
            var tcp = await _listener.AcceptTcpClientAsync();
            Console.WriteLine("TCP connected");
            _ = _room.TryJoinAsync(tcp);
        }
    }
}