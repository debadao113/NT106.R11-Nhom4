using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HotelNetworking;
using HotelData;
using System.Data.SqlClient;

namespace HotelServer
{
    public sealed class ClientInfo
    {
        public string Id { get; set; }
        public string Endpoint { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public sealed class TcpHotelServer
    {
        private sealed class Connection
        {
            public TcpClient Socket;
            public ClientInfo Info;
            public readonly TaskCompletionSource<bool> Completion =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly object lifecycle = new object();
        private readonly ConcurrentDictionary<string, Connection> clients = new ConcurrentDictionary<string, Connection>();
        private TcpListener listener;
        private Task acceptTask = Task.CompletedTask;
        private Task stopTask = Task.CompletedTask;
        private readonly RoomRepository rooms;
        public event Action<string> Logged;

        public TcpHotelServer(string connectionString = null)
        {
            rooms = new RoomRepository(connectionString ?? HotelDatabase.GetConnectionString());
        }

        public bool IsRunning { get { lock (lifecycle) { return listener != null; } } }

        public ClientInfo[] GetClients()
        {
            return clients.Values.Select(client => client.Info).OrderBy(client => client.ConnectedAt).ToArray();
        }

        public void Start(IPAddress address, int port)
        {
            lock (lifecycle)
            {
                if (listener != null || !stopTask.IsCompleted)
                    throw new InvalidOperationException("Server đang chạy hoặc đang dừng.");
                var candidate = new TcpListener(address, port);
                try
                {
                    candidate.ExclusiveAddressUse = true;
                    candidate.Start();
                    listener = candidate;
                    acceptTask = AcceptClientsAsync(candidate);
                    Log("Lắng nghe TCP tại " + candidate.LocalEndpoint);
                }
                catch
                {
                    candidate.Stop();
                    throw;
                }
            }
        }

        public Task StopAsync()
        {
            lock (lifecycle)
            {
                if (listener == null) return stopTask;
                var activeListener = listener;
                listener = null;
                activeListener.Stop();
                stopTask = DrainClientsAsync(acceptTask);
                return stopTask;
            }
        }

        private async Task AcceptClientsAsync(TcpListener activeListener)
        {
            try
            {
                while (true)
                {
                    var socket = await activeListener.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (clients.Count >= 100)
                    {
                        socket.Close();
                        Log("Từ chối kết nối vì đã đạt giới hạn 100 client.");
                        continue;
                    }
                    var connection = new Connection
                    {
                        Socket = socket,
                        Info = new ClientInfo
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Endpoint = socket.Client.RemoteEndPoint.ToString(),
                            ConnectedAt = DateTime.Now
                        }
                    };
                    clients.TryAdd(connection.Info.Id, connection);
                    Log("Client kết nối: " + connection.Info.Endpoint);
                    // Each connection owns an asynchronous read loop, keeping the accept loop available.
                    _ = ReceiveClientAsync(connection);
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException ex)
            {
                if (IsRunning) Log("Lỗi nhận kết nối: " + ex.SocketErrorCode);
            }
            finally
            {
                lock (lifecycle)
                {
                    if (listener == activeListener)
                    {
                        listener = null;
                        activeListener.Stop();
                        foreach (var client in clients.Values) client.Socket.Close();
                    }
                }
            }
        }

        private async Task ReceiveClientAsync(Connection connection)
        {
            try
            {
                var channel = new JsonConnection(connection.Socket.GetStream());
                while (true)
                {
                    NetworkMessage request;
                    try { request = await channel.ReadAsync().ConfigureAwait(false); }
                    catch (ProtocolException ex)
                    {
                        await channel.WriteAsync(NetworkMessage.Failure(null, "INVALID_REQUEST", ex.Message)).ConfigureAwait(false);
                        if (ex.CloseConnection) break;
                        continue;
                    }
                    if (request == null) break;
                    NetworkMessage response;
                    try { response = await HandleRequestAsync(request, connection.Info.Id).ConfigureAwait(false); }
                    catch (SqlException ex)
                    {
                        Log("Lỗi SQL Server " + ex.Number + ". Kiểm tra kết nối và quyền truy cập CSDL.");
                        response = NetworkMessage.Failure(request, "DATABASE_UNAVAILABLE", "Không thể truy cập cơ sở dữ liệu. Kiểm tra cấu hình SQL Server tại HotelServer.");
                    }
                    catch (ArgumentException)
                    {
                        response = NetworkMessage.Failure(request, "SERVER_CONFIGURATION", "Cấu hình dữ liệu tại HotelServer không hợp lệ.");
                    }
                    catch (InvalidOperationException)
                    {
                        response = NetworkMessage.Failure(request, "SERVER_CONFIGURATION", "Cấu hình dữ liệu tại HotelServer không hợp lệ.");
                    }
                    await channel.WriteAsync(response).ConfigureAwait(false);
                    Log("Đã xử lý yêu cầu từ " + connection.Info.Endpoint + (response.success == true ? "." : " (lỗi " + response.error.code + ")."));
                }
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                connection.Socket.Close();
                Connection removed;
                clients.TryRemove(connection.Info.Id, out removed);
                Log("Client ngắt kết nối: " + connection.Info.Endpoint);
                connection.Completion.TrySetResult(true);
            }
        }

        private async Task<NetworkMessage> HandleRequestAsync(NetworkMessage request, string connectionId)
        {
            if (request.type != "request" || string.IsNullOrWhiteSpace(request.requestId) || request.requestId.Length > 100
                || string.IsNullOrWhiteSpace(request.action) || request.action.Length > 64)
                return NetworkMessage.Failure(null, "INVALID_REQUEST", "Cần type=request, requestId tối đa 100 ký tự và action tối đa 64 ký tự.");
            switch (request.action)
            {
                case "HELLO":
                    return NetworkMessage.Response(request, new { connectionId, protocolVersion = 1 });
                case "PING":
                    return NetworkMessage.Response(request, new { serverTimeUtc = DateTime.UtcNow.ToString("O") });
                case "GET_ROOMS":
                    return NetworkMessage.Response(request, await rooms.GetRoomsAsync().ConfigureAwait(false));
                default:
                    return NetworkMessage.Failure(request, "UNKNOWN_ACTION", "Lệnh chưa được hỗ trợ.");
            }
        }

        private async Task DrainClientsAsync(Task accepting)
        {
            await accepting.ConfigureAwait(false);
            var activeClients = clients.Values.ToArray();
            foreach (var client in activeClients) client.Socket.Close();
            await Task.WhenAll(activeClients.Select(client => client.Completion.Task)).ConfigureAwait(false);
            Log("Server đã dừng, các kết nối đã đóng.");
        }

        private void Log(string message)
        {
            Logged?.Invoke(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        }
    }
}
