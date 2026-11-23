using System;
using System.Configuration;
using System.Threading.Tasks;
using HotelNetworking;

class HotelServerProbe
{
    static int Main()
    {
        try { RunAsync().GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.WriteLine("ERROR: " + ex.Message); return 1; }
        finally { Console.WriteLine("Press Enter to close."); Console.ReadLine(); }
    }

    static async Task RunAsync()
    {
        var settings = TlsSettings.FromConfiguration();
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.CertificateSha256))
            throw new InvalidOperationException("Configure TLS with Configure-ClientTls.ps1 first.");
        string host = ConfigurationManager.AppSettings["HotelServerHost"];
        int port = int.Parse(ConfigurationManager.AppSettings["HotelServerPort"] ?? "8888");
        using (var client = new NetworkClient(settings))
        {
            Console.WriteLine("Connecting with TLS to " + host + ":" + port);
            await client.ConnectAsync(host, port);
            var ping = await client.SendAsync("PING", null);
            if (ping.success != true) throw new Exception("PING failed.");
            Console.WriteLine("TLS + HELLO + PING: OK");
            Console.Write("Staff username: ");
            string username = Console.ReadLine();
            Console.Write("Staff password: ");
            string password = "";
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password = password.Substring(0, password.Length - 1); }
                else if (!char.IsControl(key.KeyChar) && password.Length < 128) password += key.KeyChar;
            }
            Console.WriteLine();
            var login = await client.SendAsync("LOGIN", new { username, password }, 90000);
            password = null;
            if (login.success != true) throw new Exception(login.error?.message ?? "LOGIN failed.");
            Console.WriteLine("LOGIN: OK");
            var rooms = await client.SendAsync("GET_ROOMS", null, 90000);
            if (rooms.success != true) throw new Exception(rooms.error?.message ?? "GET_ROOMS failed.");
            Console.WriteLine("GET_ROOMS from Azure SQL through HotelServer: OK");
        }
    }
}
