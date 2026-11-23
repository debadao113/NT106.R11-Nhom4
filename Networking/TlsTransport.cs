using System;
using System.Configuration;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace HotelNetworking
{
    public sealed class TlsSettings
    {
        public bool Enabled { get; set; }
        public string TargetHost { get; set; }
        public string CertificateSha256 { get; set; }
        public string ServerCertificateThumbprint { get; set; }

        public static TlsSettings FromConfiguration()
        {
            string mode = ConfigurationManager.AppSettings["TransportSecurity"] ?? "None";
            if (mode != "None" && mode != "Tls12")
                throw new InvalidOperationException("TransportSecurity phải là None hoặc Tls12.");
            return new TlsSettings
            {
                Enabled = mode == "Tls12",
                TargetHost = ConfigurationManager.AppSettings["TlsTargetHost"],
                CertificateSha256 = ConfigurationManager.AppSettings["TlsCertificateSha256"],
                ServerCertificateThumbprint = ConfigurationManager.AppSettings["TlsServerCertificateThumbprint"]
            };
        }
    }

    public static class TlsTransport
    {
        public static string CertificatePin(X509Certificate certificate)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(certificate.GetRawCertData())).Replace("-", "");
        }

        private static string NormalizePin(string pin)
        {
            pin = (pin ?? "").Replace(" ", "").Replace(":", "").Replace("-", "").ToUpperInvariant();
            if (pin.Length != 64) throw new InvalidOperationException("Cần SHA-256 của chứng chỉ HotelServer (64 ký tự hex).");
            foreach (char digit in pin)
                if (!Uri.IsHexDigit(digit)) throw new InvalidOperationException("SHA-256 chứng chỉ không hợp lệ.");
            return pin;
        }

        public static X509Certificate2 LoadServerCertificate(TlsSettings settings)
        {
            if (!settings.Enabled) return null;
            if (string.IsNullOrWhiteSpace(settings.ServerCertificateThumbprint))
                throw new InvalidOperationException("Chạy Configure-ServerTls.ps1 trước khi bật TLS.");
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                var matches = store.Certificates.Find(X509FindType.FindByThumbprint, settings.ServerCertificateThumbprint, false);
                foreach (var certificate in matches)
                    if (certificate.HasPrivateKey && certificate.NotBefore.ToUniversalTime() <= DateTime.UtcNow
                        && certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow)
                        return certificate;
            }
            throw new InvalidOperationException("Không tìm thấy chứng chỉ TLS còn hạn và có private key trong CurrentUser/My.");
        }

        public static async Task<Stream> ConnectClientAsync(TcpClient socket, TlsSettings settings)
        {
            if (!settings.Enabled) return socket.GetStream();
            string expectedPin = NormalizePin(settings.CertificateSha256);
            if (string.IsNullOrWhiteSpace(settings.TargetHost))
                throw new InvalidOperationException("Cần TlsTargetHost để xác minh tên HotelServer.");
            var tls = new SslStream(socket.GetStream(), false, (sender, certificate, chain, errors) =>
            {
                if (certificate == null || (errors & (SslPolicyErrors.RemoteCertificateNotAvailable
                    | SslPolicyErrors.RemoteCertificateNameMismatch)) != 0) return false;
                using (var leaf = new X509Certificate2(certificate))
                {
                    if (leaf.NotBefore.ToUniversalTime() > DateTime.UtcNow || leaf.NotAfter.ToUniversalTime() <= DateTime.UtcNow)
                        return false;
                    // The explicitly distributed SHA-256 pin is the trust anchor for
                    // this project's self-signed server certificate; never trust any leaf.
                    string actualPin = CertificatePin(leaf);
                    int difference = 0;
                    for (int i = 0; i < expectedPin.Length; i++) difference |= expectedPin[i] ^ actualPin[i];
                    return difference == 0;
                }
            });
            try
            {
                await CompleteHandshakeAsync(socket,
                    tls.AuthenticateAsClientAsync(settings.TargetHost, null, SslProtocols.Tls12, false)).ConfigureAwait(false);
                return tls;
            }
            catch { tls.Dispose(); throw; }
        }

        public static async Task<Stream> AcceptServerAsync(TcpClient socket, X509Certificate2 certificate)
        {
            if (certificate == null) return socket.GetStream();
            var tls = new SslStream(socket.GetStream(), false);
            try
            {
                await CompleteHandshakeAsync(socket,
                    tls.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false)).ConfigureAwait(false);
                return tls;
            }
            catch { tls.Dispose(); throw; }
        }

        private static async Task CompleteHandshakeAsync(TcpClient socket, Task handshake)
        {
            if (await Task.WhenAny(handshake, Task.Delay(10000)).ConfigureAwait(false) != handshake)
            {
                socket.Close();
                _ = handshake.ContinueWith(task => { var ignored = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException("Bắt tay TLS quá thời gian chờ.");
            }
            await handshake.ConfigureAwait(false);
        }
    }
}
