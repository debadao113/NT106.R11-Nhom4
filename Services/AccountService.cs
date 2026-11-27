using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotelData
{
    internal static class AccountService
    {
        private static void RequireAdmin()
        {
            if (HotelSession.CurrentAccount?.IsAdmin != true)
                throw new InvalidOperationException("Chỉ admin được quản lý tài khoản admin.");
        }

        public static Task<List<StaffAccount>> GetAccountsAsync()
        {
            RequireAdmin();
            return HotelSession.SendAsync<List<StaffAccount>>(HttpMethod.Get,
                "rest/v1/profiles?select=UserId:user_id,Email:email,DisplayName:display_name,Role:role,IsActive:is_active&order=display_name");
        }

        public static Task<StaffAccount> CreateAdminAsync(AdminRegistration account)
        {
            RequireAdmin();
            return HotelSession.SendAsync<StaffAccount>(HttpMethod.Post, "functions/v1/create-admin", account);
        }
    }
}
