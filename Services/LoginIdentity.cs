using System;
using System.Text.RegularExpressions;

namespace HotelData
{
    internal static class LoginIdentity
    {
        // Staff usernames map deterministically to Auth accounts. No public API
        // needs to reveal another user's email address before login.
        private const string StaffDomain = "staff.hoteldesk.invalid";

        public static string ToEmail(string usernameOrEmail)
        {
            string value = (usernameOrEmail ?? "").Trim();
            if (value.Contains("@")) return value;
            if (!Regex.IsMatch(value, @"\A[A-Za-z0-9][A-Za-z0-9._-]{0,49}\z"))
                throw new ArgumentException("Tên đăng nhập dùng 1–50 ký tự: chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.");
            return value.ToLowerInvariant() + "@" + StaffDomain;
        }

        public static string DisplayName(string email)
        {
            string suffix = "@" + StaffDomain;
            return email != null && email.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? email.Substring(0, email.Length - suffix.Length) : email;
        }
    }
}
