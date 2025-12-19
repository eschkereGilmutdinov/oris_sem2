
using Server.Networking;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await new ServerHost(8080).RunAsync();
        }
    }
}