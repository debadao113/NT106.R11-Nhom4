namespace HotelData
{
    public sealed class Room
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Type { get; set; }
        public string Bed { get; set; }
        public long Price { get; set; }
        public bool IsOccupied { get; set; }
    }
}
