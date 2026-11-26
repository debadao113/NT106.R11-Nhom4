using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HotelData
{
    // One reusable HTTP connection pool. Tokens are attached to individual requests,
    // never to DefaultRequestHeaders, so concurrent requests cannot mix sessions.
    internal sealed class SupabaseClient : IDisposable
    {
        private readonly HttpClient http;
        private readonly SupabaseSettings settings;
        public SupabaseClient(SupabaseSettings settings, HttpMessageHandler handler = null)
        {
            this.settings = settings;
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<T> SendAsync<T>(HttpMethod method, string path, object body = null, string token = null)
        {
            using (var request = new HttpRequestMessage(method, new Uri(settings.ProjectUrl, path)))
            {
                request.Headers.Add("apikey", settings.PublishableKey);
                if (path.StartsWith("rest/v1/", StringComparison.Ordinal))
                    request.Headers.Add("Prefer", "return=representation");
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (body != null)
                    request.Content = new StringContent(new JavaScriptSerializer().Serialize(body), Encoding.UTF8, "application/json");
                try
                {
                    using (var response = await http.SendAsync(request).ConfigureAwait(false))
                    {
                        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            throw new SupabaseException(response.StatusCode, ReadError(response.StatusCode, content,
                                path.StartsWith("functions/v1/", StringComparison.Ordinal)));
                        if (string.IsNullOrWhiteSpace(content))
                            return default(T);
                        return new JavaScriptSerializer
                        {
                            MaxJsonLength = 8 * 1024 * 1024
                        }.Deserialize<T>(content);
                    }
                }
                catch (TaskCanceledException)
                {
                    throw new IOException("Supabase phản hồi quá thời gian chờ. Kiểm tra dữ liệu trước khi gửi lại thao tác.");
                }
                catch (HttpRequestException)
                {
                    throw new IOException("Không thể kết nối Supabase. Kiểm tra Internet và Project URL.");
                }
            }
        }

        private static string ReadError(HttpStatusCode status, string content, bool fromFunction)
        {
            if (fromFunction && status == HttpStatusCode.NotFound)
                return "Chức năng quản lý tài khoản chưa được triển khai trên Supabase.";
            if (fromFunction && status == HttpStatusCode.ServiceUnavailable)
            {
                try
                {
                    var body = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(content);
                    object message;
                    if (body != null && body.TryGetValue("message", out message) && message is string)
                        return (string)message;
                }
                catch (Exception) { }
            }
            if (status == HttpStatusCode.Unauthorized)
                return "Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Hãy đăng nhập lại.";
            if (status == HttpStatusCode.Forbidden)
                return "Tài khoản chưa được cấp quyền thực hiện thao tác này.";
            if ((int)status == 429)
                return "Có quá nhiều yêu cầu. Vui lòng thử lại sau.";
            if ((int)status >= 500)
                return "Supabase đang tạm thời không sẵn sàng. Vui lòng thử lại sau.";
            try
            {
                var error = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(content);
                object code;
                if (error.TryGetValue("error_code", out code) && Convert.ToString(code) == "invalid_credentials")
                    return "Email hoặc mật khẩu không đúng.";
                if (error.TryGetValue("code", out code) && Convert.ToString(code) == "23505")
                    return "Thông tin đã tồn tại hoặc phòng vừa được người khác nhận. Hãy tải lại dữ liệu.";
                object message;
                if (error.TryGetValue("message", out message) || error.TryGetValue("msg", out message) || error.TryGetValue("error_description", out message))
                    return Convert.ToString(message);
            }
            catch (Exception)
            { /* Proxies may return HTML instead of JSON. */
            }

            return "Supabase từ chối yêu cầu (HTTP " + (int)status + ").";
        }

        public void Dispose()
        {
            http.Dispose();
        }
    }

    internal sealed class SupabaseException : IOException
    {
        public HttpStatusCode StatusCode { get; }

        public SupabaseException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
