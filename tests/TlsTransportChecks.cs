using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HotelNetworking;
using HotelServer;

class TlsTransportChecks
{
    static int Main(string[] args)
    {
        try { RunAsync(args[0]).GetAwaiter().GetResult(); return 0; }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    static int FreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    static TlsSettings ClientSettings(string pin, string name = "hotelserver")
    {
        return new TlsSettings { Enabled = true, CertificateSha256 = pin, TargetHost = name };
    }

    static async Task RejectedAsync(TlsSettings settings, int port, string label)
    {
        using (var client = new NetworkClient(settings))
        {
            try { await client.ConnectAsync("127.0.0.1", port); }
            catch (AuthenticationException) { Console.WriteLine("PASS: " + label); return; }
        }
        throw new Exception("Unexpected TLS acceptance: " + label);
    }

    static async Task RunAsync(string thumbprint)
    {
        var settings = new TlsSettings { Enabled = true, ServerCertificateThumbprint = thumbprint };
        using (var certificate = TlsTransport.LoadServerCertificate(settings))
        {
            string pin = TlsTransport.CertificatePin(certificate);
            var server = new TcpHotelServer(tlsSettings: settings);
            int port = FreePort();
            server.Start(IPAddress.Loopback, port);
            try
            {
                using (var client = new NetworkClient(ClientSettings(pin)))
                {
                    await client.ConnectAsync("127.0.0.1", port);
                    if ((await client.SendAsync("PING", null)).success != true) throw new Exception("TLS PING failed.");
                    if ((await client.SendAsync("GET_ROOMS", null)).success != false) throw new Exception("Authentication bypass.");
                    Console.WriteLine("PASS: Pinned self-signed TLS accepts HELLO/PING and still requires login.");
                }
                await RejectedAsync(ClientSettings(new string('0', 64)), port, "Wrong certificate pin rejected.");
                await RejectedAsync(ClientSettings(pin, "wrong-hostname"), port, "Wrong server name rejected.");
                await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
                {
                    using (var client = new NetworkClient(ClientSettings(pin)))
                    {
                        await client.ConnectAsync("127.0.0.1", port);
                        if ((await client.SendAsync("PING", null)).success != true) throw new Exception("Concurrent PING failed.");
                    }
                }));
                Console.WriteLine("PASS: Eight concurrent TLS clients.");
                using (var raw = new TcpClient())
                {
                    await raw.ConnectAsync(IPAddress.Loopback, port);
                    byte[] request = Encoding.UTF8.GetBytes("{\"type\":\"request\",\"requestId\":\"raw\",\"action\":\"HELLO\"}\n");
                    await raw.GetStream().WriteAsync(request, 0, request.Length);
                    var bytes = new byte[128];
                    var read = raw.GetStream().ReadAsync(bytes, 0, bytes.Length);
                    if (await Task.WhenAny(read, Task.Delay(12000)) != read) throw new Exception("Plaintext connection not closed.");
                    try
                    {
                        int count = await read;
                        if (Encoding.UTF8.GetString(bytes, 0, count).Contains("response")) throw new Exception("Plaintext accepted.");
                    }
                    catch (IOException) { }
                    Console.WriteLine("PASS: Plaintext does not receive an application response on TLS port.");
                }
                using (var stalled = new TcpClient())
                {
                    await stalled.ConnectAsync(IPAddress.Loopback, port);
                    await Task.Delay(100);
                    var stopping = server.StopAsync();
                    if (await Task.WhenAny(stopping, Task.Delay(3000)) != stopping) throw new Exception("Stalled TLS blocked stop.");
                    await stopping;
                    Console.WriteLine("PASS: Stop drains clients during an unfinished handshake.");
                }
                port = FreePort();
                server.Start(IPAddress.Loopback, port);
                using (var client = new NetworkClient(ClientSettings(pin)))
                    await client.ConnectAsync("127.0.0.1", port);
                Console.WriteLine("PASS: TLS server restarts successfully.");
            }
            finally { await server.StopAsync(); }

            var legacy = new TcpHotelServer(tlsSettings: new TlsSettings());
            port = FreePort();
            legacy.Start(IPAddress.Loopback, port);
            try
            {
                using (var client = new NetworkClient(new TlsSettings()))
                    await client.ConnectAsync("127.0.0.1", port);
                Console.WriteLine("PASS: Existing local TCP mode still connects.");
            }
            finally { await legacy.StopAsync(); }
        }
    }
}
