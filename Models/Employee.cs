namespace HotelData
{
    // Personnel records are separate from Supabase Auth login credentials.
    public class EmployeeRegistration
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
    }

    public sealed class EmployeeRecord : EmployeeRegistration
    {
        public int Id { get; set; }
    }
}
