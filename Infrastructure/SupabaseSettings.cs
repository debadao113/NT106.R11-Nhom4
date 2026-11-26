using System;
using System.Configuration;
using System.Web.Script.Serialization;

namespace HotelData
{
    internal sealed class SupabaseSettings
    {
        public Uri ProjectUrl { get; }
        public string PublishableKey { get; }

        public SupabaseSettings(string url, string key)
        {
            Uri projectUrl;
            if (!Uri.TryCreate(url, UriKind.Absolute, out projectUrl) || projectUrl.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(projectUrl.UserInfo) || projectUrl.AbsolutePath != "/" || !string.IsNullOrEmpty(projectUrl.Query) || !string.IsNullOrEmpty(projectUrl.Fragment))
                throw new ConfigurationErrorsException("Điền SupabaseUrl dạng https://PROJECT.supabase.co trong App.local.config.");
            if (string.IsNullOrWhiteSpace(key) || key.Contains("YOUR_"))
                throw new ConfigurationErrorsException("Chưa có SupabasePublishableKey. Xem docs/SUPABASE_SETUP.md.");
            // A desktop app may contain a public key, never an administrator/secret key.
            bool publicKey = key.StartsWith("sb_publishable_", StringComparison.Ordinal);
            if (!publicKey && key.StartsWith("eyJ", StringComparison.Ordinal))
            {
                try
                {
                    string payload = key.Split('.')[1].Replace('-', '+').Replace('_', '/');
                    payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
                    var claims = new JavaScriptSerializer().DeserializeObject(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload))) as System.Collections.Generic.Dictionary<string, object>;
                    publicKey = claims != null && claims.ContainsKey("role") && (string)claims["role"] == "anon";
                }
                catch (Exception)
                {
                    publicKey = false;
                }
            }

            if (!publicKey)
                throw new ConfigurationErrorsException("Chỉ dùng publishable key hoặc anon key; không dùng secret/service_role key trong app.");
            ProjectUrl = projectUrl;
            PublishableKey = key;
        }

        public static SupabaseSettings Load()
        {
            return new SupabaseSettings(ConfigurationManager.AppSettings["SupabaseUrl"], ConfigurationManager.AppSettings["SupabasePublishableKey"]);
        }
    }
}
