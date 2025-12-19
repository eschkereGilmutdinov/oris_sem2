using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Common
{
    public class Protocol
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static async Task WriteJsonAsync(NetworkStream stream, object obj, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(obj, JsonOpts);
            var data = Encoding.UTF8.GetBytes(json);

            var lenBytes = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(lenBytes, 0, lenBytes.Length, ct);
            await stream.WriteAsync(data, 0, data.Length, ct);
            await stream.FlushAsync(ct);
        }

        public static async Task<string> ReadJsonAsync(NetworkStream stream, CancellationToken ct = default)
        {
            var lenBuf = await ReadExactAsync(stream, 4, ct);
            int len = BitConverter.ToInt32(lenBuf, 0);

            var dataBuf = await ReadExactAsync(stream, len, ct);
            return Encoding.UTF8.GetString(dataBuf);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int size, CancellationToken ct)
        {
            var buf = new byte[size];
            int read = 0;
            while (read < size)
            {
                int r = await stream.ReadAsync(buf, read, size - read, ct);
                if (r == 0) throw new Exception("Disconnected");
                read += r;
            }
            return buf;
        }
    }
}
