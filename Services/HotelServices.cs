using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotelData
{
    internal static class HotelServices
    {
        public static Task<List<CustomerRecord>> GetCustomersAsync(string status, string name = "")
        {
            return HotelSession.SendAsync<List<CustomerRecord>>(HttpMethod.Post, "rest/v1/rpc/hotel_get_customers", new { p_status = status, p_name = name });
        }

        public static async Task<int> RegisterCustomerAsync(CustomerRegistration customer)
        {
            int id = await HotelSession.SendAsync<int>(HttpMethod.Post, "rest/v1/rpc/hotel_check_in", new { p_customer = customer }).ConfigureAwait(false);
            HotelSession.NotifyChanged("ROOMS_CHANGED");
            HotelSession.NotifyChanged("CUSTOMERS_CHANGED");
            return id;
        }

        public static async Task<bool> CheckoutAsync(int customerId, string date)
        {
            bool result = await HotelSession.SendAsync<bool>(HttpMethod.Post, "rest/v1/rpc/hotel_check_out", new { p_stay_id = customerId, p_date = date }).ConfigureAwait(false);
            HotelSession.NotifyChanged("ROOMS_CHANGED");
            HotelSession.NotifyChanged("CUSTOMERS_CHANGED");
            return result;
        }

        public static Task<List<EmployeeRecord>> GetEmployeesAsync()
        {
            return HotelSession.SendAsync<List<EmployeeRecord>>(HttpMethod.Get, "rest/v1/employees?select=Id:id,Name:name,Phone:phone,Gender:gender,Email:email&order=id");
        }

        public static async Task<int> AddEmployeeAsync(EmployeeRegistration employee)
        {
            var result = await HotelSession.SendAsync<List<EmployeeRecord>>(HttpMethod.Post, "rest/v1/employees?select=Id:id", new { name = employee.Name, phone = employee.Phone, gender = employee.Gender, email = employee.Email }).ConfigureAwait(false);
            HotelSession.NotifyChanged("EMPLOYEES_CHANGED");
            return result[0].Id;
        }

        public static async Task<bool> DeleteEmployeeAsync(int employeeId)
        {
            var deleted = await HotelSession.SendAsync<List<EmployeeRecord>>(HttpMethod.Delete, "rest/v1/employees?id=eq." + employeeId + "&select=Id:id").ConfigureAwait(false);
            if (deleted.Count == 0)
                throw new InvalidOperationException("Không tìm thấy nhân viên hoặc bạn chưa có quyền xóa.");
            HotelSession.NotifyChanged("EMPLOYEES_CHANGED");
            return true;
        }
    }
}
