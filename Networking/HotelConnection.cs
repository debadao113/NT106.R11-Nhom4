using System.Configuration;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Web.Script.Serialization;

namespace HotelNetworking
{
    internal static class HotelConnection
    {
        public static readonly NetworkClient Client = new NetworkClient();
        private static readonly object sync = new object();
        private static StaffAccount account;
        public static event Action Authenticated;
        public static StaffAccount CurrentAccount
        {
            get { lock (sync) { return Client.IsConnected ? account : null; } }
        }

        static HotelConnection()
        {
            Client.Disconnected += delegate { lock (sync) { account = null; } };
        }

        public static async Task<StaffAccount> LoginAsync(string username, string password)
        {
            lock (sync) { account = null; }
            await ConnectAsync().ConfigureAwait(false);
            var response = await Client.SendAsync("LOGIN", new { username, password }, 90000).ConfigureAwait(false);
            if (response.success != true) throw new IOException(response.error?.message ?? "Đăng nhập thất bại.");
            var signedIn = new JavaScriptSerializer().ConvertToType<StaffAccount>(response.data);
            lock (sync)
            {
                if (!Client.IsConnected) throw new IOException("Đã mất kết nối HotelServer. Vui lòng đăng nhập lại.");
                account = signedIn;
            }
            Authenticated?.Invoke();
            return signedIn;
        }

        public static async Task EnsureAuthenticatedAsync()
        {
            await ConnectAsync().ConfigureAwait(false);
            if (CurrentAccount == null) throw new IOException("Vui lòng đăng nhập lại tại cửa sổ Kết nối server.");
        }
        public static Task ConnectAsync()
        {
            string host = ConfigurationManager.AppSettings["HotelServerHost"] ?? "127.0.0.1";
            int port = int.Parse(ConfigurationManager.AppSettings["HotelServerPort"] ?? "8888");
            return Client.ConnectAsync(host, port);
        }
    }
}
