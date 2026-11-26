using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotelData;

internal static class SupabaseChecks
{
    private const string UserId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private static int refreshCount;
    private static int writeCount;
    private static bool denyProfile;
    private static bool rejectWrite;
    private static string expectedEmail = "staff@example.com";
    private static int Main()
    {
        try
        {
            RunAsync().GetAwaiter().GetResult();
            Console.WriteLine("PASS: Supabase authentication, refresh, requests, errors and public-key validation.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            HotelSession.Dispose();
        }
    }

    private static async Task RunAsync()
    {
        Assert(LoginIdentity.ToEmail(" nhom4 ") == "nhom4@staff.hoteldesk.invalid", "Username normalization failed.");
        Assert(LoginIdentity.ToEmail("Long") == "long@staff.hoteldesk.invalid", "Username case mapping failed.");
        Assert(LoginIdentity.ToEmail("staff@example.com") == "staff@example.com", "Existing email login must keep working.");
        Assert(LoginIdentity.DisplayName("long@staff.hoteldesk.invalid") == "long", "Internal email must be hidden in the username field.");
        bool invalidUsernameRejected = false;
        try { LoginIdentity.ToEmail("name with spaces"); } catch (ArgumentException) { invalidUsernameRejected = true; }
        Assert(invalidUsernameRejected, "Invalid username accepted.");
        Assert(new StaffAccount { Role = "admin" }.IsAdmin, "Admin permissions missing.");
        Assert(!new StaffAccount { Role = "receptionist" }.IsAdmin, "Receptionist must not gain admin permissions.");
        foreach (var key in new[]
        {
            "sb_secret_forbidden",
            "YOUR_PUBLISHABLE_KEY",
            CreateKey("service_role")
        })
            Throws(() => new SupabaseSettings("https://example.supabase.co", key));
        Throws(() => new SupabaseSettings("http://example.supabase.co", "sb_publishable_test"));
        new SupabaseSettings("https://example.supabase.co", CreateKey("anon"));
        var client = new SupabaseClient(new SupabaseSettings("https://example.supabase.co", "sb_publishable_test"), new FakeHandler());
        typeof(HotelSession).GetField("client", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, client);
        denyProfile = true;
        await ThrowsAsync(() => HotelSession.LoginAsync("staff@example.com", "not-a-real-password"));
        Assert(HotelSession.CurrentAccount == null, "Denied users must have no local session.");
        denyProfile = false;
        var account = await HotelSession.LoginAsync("staff@example.com", "not-a-real-password");
        Assert(account.IsAdmin && account.UserId == UserId, "Auth/profile mapping failed.");
        account.Role = "receptionist";
        bool accountCreationDenied = false;
        try { await AccountService.CreateAdminAsync(new AdminRegistration()); }
        catch (InvalidOperationException) { accountCreationDenied = true; }
        Assert(accountCreationDenied, "Receptionists must not create accounts from the client service.");
        account.Role = "admin";
        // The login token expires immediately in this fixture. Only one concurrent
        // request may refresh it; the others must reuse the refreshed token.
        var reads = new List<Task<List<Room>>>();
        for (int i = 0; i < 6; i++)
            reads.Add(RoomService.GetRoomsAsync());
        await Task.WhenAll(reads);
        Assert(refreshCount == 1, "Refresh must be serialized.");
        Assert(reads[0].Result[0].IsOccupied && reads[0].Result[0].Price == 500000, "Room response mapping failed.");
        var newAdmin = await AccountService.CreateAdminAsync(new AdminRegistration { UsernameOrEmail = "Long", DisplayName = "Long", Password = "not-a-real-password", RequestId = Guid.NewGuid().ToString() });
        Assert(newAdmin.IsAdmin, "Admin created from the client must have normal admin permissions.");
        await RoomService.AddRoomAsync(new Room { Number = "101", Type = "Double", Bed = "Double", Price = 500000 });
        Assert(writeCount == 1, "Insert must not be duplicated.");
        rejectWrite = true;
        await ThrowsAsync(() => RoomService.AddRoomAsync(new Room { Number = "101", Type = "Double", Bed = "Double", Price = 500000 }));
        Assert(writeCount == 2, "Failed writes must never be retried automatically.");
        await HotelSession.LogoutAsync();
        Assert(HotelSession.CurrentAccount == null, "Logout must clear the session.");
        await ThrowsAsync(() => RoomService.GetRoomsAsync());
        foreach (string username in new[] { "nhom4", "Long" })
        {
            expectedEmail = LoginIdentity.ToEmail(username);
            await HotelSession.LoginAsync(username, "not-a-real-password");
            await HotelSession.LogoutAsync(false);
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert(request.RequestUri.Scheme == "https", "HTTPS required.");
            Assert(string.Join("", request.Headers.GetValues("apikey")) == "sb_publishable_test", "Missing API key.");
            string path = request.RequestUri.PathAndQuery;
            if (path.Contains("grant_type=password"))
            {
                Assert(request.Headers.Authorization == null, "Login must not inherit a stale token.");
                var payload = new System.Web.Script.Serialization.JavaScriptSerializer()
                    .Deserialize<Dictionary<string, string>>(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert(payload["email"] == expectedEmail, "Username was not mapped to the expected Auth account.");
                Assert(payload["password"] == "not-a-real-password", "Login must never transform the password.");
                return Json("{\"access_token\":\"first-token\",\"refresh_token\":\"first-refresh\",\"expires_in\":1,\"user\":{\"id\":\"" + UserId + "\"}}");
            }

            if (path.Contains("grant_type=refresh_token"))
            {
                Interlocked.Increment(ref refreshCount);
                return Json("{\"access_token\":\"renewed-token\",\"refresh_token\":\"next-refresh\",\"expires_in\":3600,\"user\":{\"id\":\"" + UserId + "\"}}");
            }

            if (path.StartsWith("/rest/v1/profiles"))
            {
                Assert(request.Headers.Authorization.Parameter == "first-token", "Profile requires login JWT.");
                return Json(denyProfile ? "[]" : "[{\"UserId\":\"" + UserId + "\",\"Email\":\"staff@example.com\",\"DisplayName\":\"Staff\",\"Role\":\"admin\"}]");
            }

            Assert(request.Headers.Authorization.Parameter == "renewed-token", "Request must use refreshed JWT.");
            if (path.StartsWith("/rest/v1/rpc/hotel_get_rooms"))
                return Json("[{\"Id\":1,\"Number\":\"101\",\"Type\":\"Double\",\"Bed\":\"Double\",\"Price\":500000,\"IsOccupied\":true}]");
            if (path == "/functions/v1/create-admin")
                return Json("{\"UserId\":\"new-admin\",\"Email\":\"long@staff.hoteldesk.invalid\",\"DisplayName\":\"Long\",\"Role\":\"admin\"}");
            if (path.StartsWith("/rest/v1/rooms"))
            {
                Interlocked.Increment(ref writeCount);
                Assert(string.Join("", request.Headers.GetValues("Prefer")).Contains("return=representation"), "Writes must request inserted rows.");
                return rejectWrite ? Json("<html>Bad Gateway</html>", HttpStatusCode.BadGateway) : Json("[{\"Id\":2,\"Number\":\"101\"}]");
            }

            if (path.StartsWith("/auth/v1/logout"))
                return Json("");
            throw new Exception("Unexpected request: " + path);
        }
    }

    private static Task<HttpResponseMessage> Json(string body, HttpStatusCode status = HttpStatusCode.OK) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    private static string CreateKey(string role) => "eyJhbGciOiJIUzI1NiJ9." + Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"role\":\"" + role + "\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private static void Throws(Action operation)
    {
        try
        {
            operation();
        }
        catch (System.Configuration.ConfigurationErrorsException)
        {
            return;
        }

        throw new Exception("Invalid configuration was accepted.");
    }

    private static async Task ThrowsAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (IOException)
        {
            return;
        }

        throw new Exception("Expected operation to fail.");
    }
}
