using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace HotelNetworking
{
    public sealed class NetworkClient : IDisposable
    {
        private sealed class Session
        {
            public TcpClient Socket;
            public JsonConnection Channel;
            public readonly ConcurrentDictionary<string, TaskCompletionSource<NetworkMessage>> Pending =
                new ConcurrentDictionary<string, TaskCompletionSource<NetworkMessage>>();
        }

        private readonly object sync = new object();
        private readonly SemaphoreSlim connectLock = new SemaphoreSlim(1, 1);
        private Session session;
        private bool disposed;
        public event Action<NetworkMessage> EventReceived;
        public event Action Disconnected;
        public bool IsConnected { get { lock (sync) { return session != null; } } }

        public async Task ConnectAsync(string host, int port)
        {
            await connectLock.WaitAsync().ConfigureAwait(false);
            try
            {
                lock (sync)
                {
                    if (disposed) throw new ObjectDisposedException(nameof(NetworkClient));
                    if (session != null) return;
                }
                var socket = new TcpClient();
                Session active = null;
                try
                {
                    var connecting = socket.ConnectAsync(host, port);
                    if (await Task.WhenAny(connecting, Task.Delay(5000)).ConfigureAwait(false) != connecting)
                    {
                        socket.Close();
                        try { await connecting.ConfigureAwait(false); } catch (Exception) { }
                        throw new TimeoutException("Kết nối HotelServer quá thời gian chờ.");
                    }
                    await connecting.ConfigureAwait(false);
                    active = new Session { Socket = socket, Channel = new JsonConnection(socket.GetStream()) };
                    lock (sync)
                    {
                        if (disposed) throw new ObjectDisposedException(nameof(NetworkClient));
                        session = active;
                    }
                    _ = ReceiveAsync(active);
                    var hello = await SendAsync("HELLO", null).ConfigureAwait(false);
                    if (hello.success != true) throw new IOException("HotelServer từ chối kết nối.");
                }
                catch
                {
                    if (active != null) Disconnect(active);
                    else socket.Close();
                    throw;
                }
            }
            finally { connectLock.Release(); }
        }

        public async Task<NetworkMessage> SendAsync(string action, object data, int timeoutMilliseconds = 10000)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            var request = new NetworkMessage { type = "request", requestId = Guid.NewGuid().ToString("N"), action = action, data = data };
            var completion = new TaskCompletionSource<NetworkMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            Session active;
            lock (sync)
            {
                active = session;
                if (active == null) throw new IOException("Chưa kết nối HotelServer.");
                active.Pending.TryAdd(request.requestId, completion);
            }
            try
            {
                var operation = WriteAndWaitAsync(active, request, completion.Task);
                if (await Task.WhenAny(operation, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false) != operation)
                {
                    Disconnect(active);
                    try { await operation.ConfigureAwait(false); } catch (Exception) { }
                    throw new TimeoutException("Yêu cầu quá thời gian chờ. Hãy kết nối lại và kiểm tra dữ liệu trước khi gửi lại thao tác.");
                }
                return await operation.ConfigureAwait(false);
            }
            catch (IOException) { Disconnect(active); throw; }
            catch (SocketException) { Disconnect(active); throw; }
            catch (ObjectDisposedException) { Disconnect(active); throw; }
            finally
            {
                TaskCompletionSource<NetworkMessage> removed;
                active.Pending.TryRemove(request.requestId, out removed);
            }
        }

        private static async Task<NetworkMessage> WriteAndWaitAsync(Session active, NetworkMessage request, Task<NetworkMessage> response)
        {
            await active.Channel.WriteAsync(request).ConfigureAwait(false);
            return await response.ConfigureAwait(false);
        }

        private async Task ReceiveAsync(Session active)
        {
            try
            {
                while (true)
                {
                    var message = await active.Channel.ReadAsync().ConfigureAwait(false);
                    if (message == null) break;
                    if (message.type == "event") EventReceived?.Invoke(message);
                    else if (message.type == "response" && message.requestId != null)
                    {
                        TaskCompletionSource<NetworkMessage> completion;
                        if (active.Pending.TryRemove(message.requestId, out completion)) completion.TrySetResult(message);
                    }
                }
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            finally { Disconnect(active); }
        }

        private void Disconnect(Session active)
        {
            bool notify;
            lock (sync)
            {
                notify = session == active;
                if (notify) session = null;
                active.Socket.Close();
                foreach (var pending in active.Pending.Values)
                    pending.TrySetException(new IOException("Đã ngắt kết nối HotelServer. Kiểm tra dữ liệu trước khi gửi lại thao tác."));
                active.Pending.Clear();
            }
            if (notify) Disconnected?.Invoke();
        }

        public void Disconnect()
        {
            Session active;
            lock (sync) { active = session; }
            if (active != null) Disconnect(active);
        }

        public void Dispose()
        {
            lock (sync) { disposed = true; }
            Disconnect();
        }
    }
}
