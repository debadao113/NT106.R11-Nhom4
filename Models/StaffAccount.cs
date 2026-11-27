namespace HotelData
{
    public sealed class StaffAccount
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdmin => Role == "admin";
    }
}
