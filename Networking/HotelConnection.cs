using System.Configuration;
using System.Threading.Tasks;

namespace HotelNetworking
{
    internal static class HotelConnection
    {
        public static readonly NetworkClient Client = new NetworkClient();
        public static Task ConnectAsync()
        {
            string host = ConfigurationManager.AppSettings["HotelServerHost"] ?? "127.0.0.1";
            int port = int.Parse(ConfigurationManager.AppSettings["HotelServerPort"] ?? "8888");
            return Client.ConnectAsync(host, port);
        }
    }
}
