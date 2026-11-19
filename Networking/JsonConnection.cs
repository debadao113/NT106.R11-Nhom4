using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HotelNetworking
{
    // Property names match the JSON wire contract.
    public sealed class NetworkMessage
    {
        public string type { get; set; }
        public string requestId { get; set; }
        public string action { get; set; }
        public object data { get; set; }
        public bool? success { get; set; }
        public ProtocolError error { get; set; }

        public static NetworkMessage Response(NetworkMessage request, object data)
        {
            return new NetworkMessage { type = "response", requestId = request.requestId, action = request.action, success = true, data = data };
        }

        public static NetworkMessage Failure(NetworkMessage request, string code, string message)
        {
            return new NetworkMessage
            {
                type = "response", requestId = request?.requestId, action = request?.action, success = false,
                error = new ProtocolError { code = code, message = message }
            };
        }
    }

    public sealed class ProtocolError
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public sealed class ProtocolException : IOException
    {
        public bool CloseConnection { get; private set; }
        public ProtocolException(string message, bool closeConnection = false) : base(message)
        {
            CloseConnection = closeConnection;
        }
    }

    public sealed class JsonConnection
    {
        public const int MaxFrameBytes = 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly Stream stream;
        private readonly byte[] buffer = new byte[4096];
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private int offset;
        private int available;

        public JsonConnection(Stream stream) { this.stream = stream; }

        private static JavaScriptSerializer CreateSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = MaxFrameBytes, RecursionLimit = 32 };
        }

        // One UTF-8 JSON object per line. Retain bytes after a newline for the next frame.
        public async Task<NetworkMessage> ReadAsync()
        {
            using (var frame = new MemoryStream())
            {
                while (true)
                {
                    if (offset == available)
                    {
                        available = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        offset = 0;
                        if (available == 0)
                        {
                            if (frame.Length != 0) throw new ProtocolException("Gói tin bị ngắt giữa chừng.", true);
                            return null;
                        }
                    }
                    int end = offset;
                    while (end < available && buffer[end] != (byte)'\n') end++;
                    int count = end - offset;
                    if (frame.Length + count > MaxFrameBytes) throw new ProtocolException("Gói tin vượt giới hạn 1 MiB.", true);
                    frame.Write(buffer, offset, count);
                    offset = end;
                    if (end == available) continue;
                    offset++;
                    try
                    {
                        var message = CreateSerializer().Deserialize<NetworkMessage>(Utf8.GetString(frame.ToArray()));
                        if (message == null) throw new ArgumentException();
                        return message;
                    }
                    catch (ArgumentException) { throw new ProtocolException("JSON hoặc UTF-8 không hợp lệ."); }
                    catch (InvalidOperationException) { throw new ProtocolException("Cấu trúc JSON không hợp lệ."); }
                }
            }
        }

        public async Task WriteAsync(NetworkMessage message)
        {
            byte[] bytes = Utf8.GetBytes(CreateSerializer().Serialize(message) + "\n");
            if (bytes.Length - 1 > MaxFrameBytes) throw new ProtocolException("Gói tin vượt giới hạn 1 MiB.", true);
            await writeLock.WaitAsync().ConfigureAwait(false);
            try { await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false); }
            finally { writeLock.Release(); }
        }
    }
}
