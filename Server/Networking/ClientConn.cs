using Server.Models;

using System.Net.Sockets;


namespace Server.Networking
{
    public class ClientConn
    {
        public TcpClient Tcp { get; }
        public NetworkStream Stream { get; }
        public Player Player { get; }

        public ClientConn(TcpClient tcp, NetworkStream stream, Player player)
        {
            Tcp = tcp;
            Stream = stream;
            Player = player;
        }
    }
}
