namespace HotelData
{
    public class CustomerRegistration
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public string BirthDate { get; set; }
        public string IdentityDocument { get; set; }
        public string Address { get; set; }
        public string CheckIn { get; set; }
    }

    // One record represents one stay, including the room price at check-in.
    public sealed class CustomerRecord : CustomerRegistration
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string Bed { get; set; }
        public long Price { get; set; }
        public string CheckOut { get; set; }
        public bool HasCheckedOut => !string.IsNullOrEmpty(CheckOut);
    }
}
