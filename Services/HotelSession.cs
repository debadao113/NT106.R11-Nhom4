using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HotelData
{
    internal static class HotelSession
    {
        private static readonly SemaphoreSlim sessionLock = new SemaphoreSlim(1, 1);
        private static SupabaseClient client;
        private static string accessToken;
        private static string refreshToken;
        private static DateTime expiresAt;
        public static StaffAccount CurrentAccount { get; private set; }

        public static event Action Authenticated;
        public static event Action AuthenticationLost;
        public static event Action<string> DataChanged;
        private static SupabaseClient Client => client ?? (client = new SupabaseClient(SupabaseSettings.Load()));

        public static async Task<StaffAccount> LoginAsync(string usernameOrEmail, string password)
        {
            string email = LoginIdentity.ToEmail(usernameOrEmail);
            await sessionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                ClearSession();
                AuthenticationLost?.Invoke();
                var session = await Client.SendAsync<AuthResponse>(HttpMethod.Post, "auth/v1/token?grant_type=password", new { email, password }).ConfigureAwait(false);
                var profiles = await Client.SendAsync<List<StaffAccount>>(HttpMethod.Get, "rest/v1/profiles?select=UserId:user_id,Email:email,DisplayName:display_name,Role:role,IsActive:is_active&is_active=eq.true&user_id=eq." + Uri.EscapeDataString(session.user.id), token: session.access_token).ConfigureAwait(false);
                if (profiles.Count != 1)
                    throw new IOException("Tài khoản chưa có hồ sơ nhân viên hoạt động. Nhờ quản trị viên cấp quyền trong Supabase.");
                SetTokens(session);
                CurrentAccount = profiles[0];
            }
            finally
            {
                sessionLock.Release();
            }

            Authenticated?.Invoke();
            return CurrentAccount;
        }

        public static async Task<T> SendAsync<T>(HttpMethod method, string path, object body = null)
        {
            string token = await GetAccessTokenAsync().ConfigureAwait(false);
            try
            {
                return await Client.SendAsync<T>(method, path, body, token).ConfigureAwait(false);
            }
            catch (SupabaseException ex)when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                await LogoutAsync(false).ConfigureAwait(false);
                throw;
            }
        }

        private static async Task<string> GetAccessTokenAsync()
        {
            await sessionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (CurrentAccount == null)
                    throw new IOException("Vui lòng đăng nhập Supabase.");
                if (DateTime.UtcNow >= expiresAt)
                {
                    try
                    {
                        var renewed = await Client.SendAsync<AuthResponse>(HttpMethod.Post, "auth/v1/token?grant_type=refresh_token", new { refresh_token = refreshToken }).ConfigureAwait(false);
                        SetTokens(renewed);
                    }
                    catch (SupabaseException ex)when (ex.StatusCode == HttpStatusCode.BadRequest || ex.StatusCode == HttpStatusCode.Unauthorized || ex.StatusCode == HttpStatusCode.Forbidden)
                    {
                        ClearSession();
                        AuthenticationLost?.Invoke();
                        throw;
                    }
                }

                return accessToken;
            }
            finally
            {
                sessionLock.Release();
            }
        }

        public static Task<object> CheckConnectionAsync()
        {
            return SendAsync<object>(HttpMethod.Get, "rest/v1/profiles?select=user_id&limit=1");
        }

        public static async Task LogoutAsync(bool revoke = true)
        {
            await sessionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (revoke && accessToken != null)
                {
                    try
                    {
                        await Client.SendAsync<object>(HttpMethod.Post, "auth/v1/logout?scope=local", token: accessToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ClearSession();
                    }
                }
                else
                    ClearSession();
            }
            finally
            {
                sessionLock.Release();
                AuthenticationLost?.Invoke();
            }
        }

        public static void NotifyChanged(string topic)
        {
            DataChanged?.Invoke(topic);
        }

        private static void SetTokens(AuthResponse response)
        {
            accessToken = response.access_token;
            refreshToken = response.refresh_token;
            expiresAt = DateTime.UtcNow.AddSeconds(Math.Max(0, response.expires_in - 60));
        }

        private static void ClearSession()
        {
            CurrentAccount = null;
            accessToken = refreshToken = null;
        }

        public static void Dispose()
        {
            ClearSession();
            client?.Dispose();
        }

        private sealed class AuthResponse
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
            public AuthUser user { get; set; }
        }

        private sealed class AuthUser
        {
            public string id { get; set; }
        }
    }
}
