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
using System.Collections.Generic;

namespace HotelServer
{
    public sealed class ClientInfo
    {
        public string Id { get; set; }
        public string Endpoint { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string Username { get; set; }
    }

    public sealed class TcpHotelServer
    {
        private sealed class Connection
        {
            public TcpClient Socket;
            public ClientInfo Info;
            public JsonConnection Channel;
            public StaffAccount Account;
            public int FailedLogins;
            public readonly TaskCompletionSource<bool> Completion =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly object lifecycle = new object();
        private readonly ConcurrentDictionary<string, Connection> clients = new ConcurrentDictionary<string, Connection>();
        private TcpListener listener;
        private Task acceptTask = Task.CompletedTask;
        private Task stopTask = Task.CompletedTask;
        private readonly RoomRepository rooms;
        private readonly StaffAccountRepository accounts;
        public event Action<string> Logged;

        public TcpHotelServer(string connectionString = null)
        {
            rooms = new RoomRepository(connectionString ?? HotelDatabase.GetConnectionString());
            accounts = new StaffAccountRepository(connectionString);
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
                        Channel = new JsonConnection(socket.GetStream()),
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
                var channel = connection.Channel;
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
                    try { response = await HandleRequestAsync(request, connection).ConfigureAwait(false); }
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
                    if (connection.FailedLogins >= 5) break;
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

        private async Task<NetworkMessage> HandleRequestAsync(NetworkMessage request, Connection connection)
        {
            if (request.type != "request" || string.IsNullOrWhiteSpace(request.requestId) || request.requestId.Length > 100
                || string.IsNullOrWhiteSpace(request.action) || request.action.Length > 64)
                return NetworkMessage.Failure(null, "INVALID_REQUEST", "Cần type=request, requestId tối đa 100 ký tự và action tối đa 64 ký tự.");
            switch (request.action)
            {
                case "HELLO":
                    return NetworkMessage.Response(request, new { connectionId = connection.Info.Id, protocolVersion = 1 });
                case "PING":
                    return NetworkMessage.Response(request, new { serverTimeUtc = DateTime.UtcNow.ToString("O") });
                case "GET_ROOMS":
                    if (connection.Account == null) return AuthenticationRequired(request);
                    return NetworkMessage.Response(request, await rooms.GetRoomsAsync().ConfigureAwait(false));
                case "ADD_ROOM":
                    if (connection.Account == null) return AuthenticationRequired(request);
                    return await AddRoomAsync(request).ConfigureAwait(false);
                case "LOGIN":
                    connection.Account = null;
                    connection.Info.Username = null;
                    var data = request.data as Dictionary<string, object>;
                    object username, password;
                    if (data == null || !data.TryGetValue("username", out username) || !data.TryGetValue("password", out password)
                        || !(username is string) || !(password is string))
                        return NetworkMessage.Failure(request, "VALIDATION_ERROR", "Cần tên đăng nhập và mật khẩu.");
                    var account = await accounts.AuthenticateAsync((string)username, (string)password).ConfigureAwait(false);
                    if (account == null)
                    {
                        connection.FailedLogins++;
                        await Task.Delay(500).ConfigureAwait(false);
                        return NetworkMessage.Failure(request, "INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không đúng.");
                    }
                    connection.Account = account;
                    connection.Info.Username = account.username;
                    connection.FailedLogins = 0;
                    return NetworkMessage.Response(request, account);
                default:
                    return NetworkMessage.Failure(request, "UNKNOWN_ACTION", "Lệnh chưa được hỗ trợ.");
            }
        }

        private static NetworkMessage AuthenticationRequired(NetworkMessage request)
        {
            return NetworkMessage.Failure(request, "AUTH_REQUIRED", "Vui lòng đăng nhập tài khoản lễ tân.");
        }

        private async Task<NetworkMessage> AddRoomAsync(NetworkMessage request)
        {
            var data = request.data as Dictionary<string, object>;
            object roomNo, roomType, bed, price;
            if (data == null || !data.TryGetValue("roomNo", out roomNo) || !data.TryGetValue("roomType", out roomType)
                || !data.TryGetValue("bed", out bed) || !data.TryGetValue("price", out price)
                || !(roomNo is string) || !(roomType is string) || !(bed is string) || !(price is int || price is long))
                return NetworkMessage.Failure(request, "VALIDATION_ERROR", "Cần số phòng, loại phòng, loại giường và giá là số nguyên.");
            var room = new Room
            {
                roomNo = ((string)roomNo).Trim(), roomType = ((string)roomType).Trim(),
                bed = ((string)bed).Trim(), price = Convert.ToInt64(price)
            };
            if (room.roomNo.Length == 0 || room.roomNo.Length > 50 || room.roomType.Length == 0 || room.roomType.Length > 100
                || room.bed.Length == 0 || room.bed.Length > 50 || room.price < 0)
                return NetworkMessage.Failure(request, "VALIDATION_ERROR", "Số phòng tối đa 50 ký tự, loại phòng 100, loại giường 50. Các trường phải có nội dung và giá phải không âm.");
            try
            {
                var saved = await rooms.AddRoomAsync(room).ConfigureAwait(false);
                var changed = new NetworkMessage { type = "event", action = "ROOMS_CHANGED", data = new { roomId = saved.roomid } };
                await Task.WhenAll(clients.Values.Where(client => client.Account != null).Select(client => SendEventAsync(client, changed))).ConfigureAwait(false);
                return NetworkMessage.Response(request, saved);
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                return NetworkMessage.Failure(request, "ROOM_EXISTS", "Số phòng đã tồn tại. Hãy tải lại danh sách phòng.");
            }
        }

        private static async Task SendEventAsync(Connection connection, NetworkMessage message)
        {
            try
            {
                var sending = connection.Channel.WriteAsync(message);
                if (await Task.WhenAny(sending, Task.Delay(2000)).ConfigureAwait(false) != sending)
                    connection.Socket.Close();
                await sending.ConfigureAwait(false);
            }
            catch (IOException) { connection.Socket.Close(); }
            catch (SocketException) { connection.Socket.Close(); }
            catch (ObjectDisposedException) { }
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
