namespace HotelNetworking
{
    public sealed class StaffAccount
    {
        public int accountId { get; set; }
        public string username { get; set; }
        public string displayName { get; set; }
        public string accountType { get; set; } = "staff";
        public int? employeeId { get; set; }
    }
}
