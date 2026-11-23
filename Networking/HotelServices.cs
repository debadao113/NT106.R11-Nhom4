using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HotelNetworking
{
    internal static class HotelServices
    {
        public static async Task<T> SendAsync<T>(string action, object data = null)
        {
            await HotelConnection.EnsureAuthenticatedAsync().ConfigureAwait(false);
            // Azure SQL serverless can take time to resume. Never retry a write automatically.
            var response = await HotelConnection.Client.SendAsync(action, data, 90000).ConfigureAwait(false);
            if (response.success != true) throw new IOException(response.error?.message ?? "HotelServer không thể xử lý yêu cầu.");
            return new JavaScriptSerializer().ConvertToType<T>(response.data);
        }
        public static Task<List<CustomerRecord>> GetCustomersAsync(string status, string name = "")
            => SendAsync<List<CustomerRecord>>("GET_CUSTOMERS", new { status, name });
        public static Task<int> RegisterCustomerAsync(CustomerRegistration customer)
            => SendAsync<int>("REGISTER_CUSTOMER", customer);
        public static Task<bool> CheckoutAsync(int cid, string date)
            => SendAsync<bool>("CHECKOUT_CUSTOMER", new { cid, date });
        public static Task<List<EmployeeRecord>> GetEmployeesAsync()
            => SendAsync<List<EmployeeRecord>>("GET_EMPLOYEES");
        public static Task<int> AddEmployeeAsync(EmployeeRegistration employee)
            => SendAsync<int>("ADD_EMPLOYEE", employee);
        public static Task<bool> DeleteEmployeeAsync(int eid)
            => SendAsync<bool>("DELETE_EMPLOYEE", new { eid });
    }
}
