namespace HotelNetworking
{
    public sealed class CustomerRegistration
    {
        public int roomid { get; set; }
        public string cname { get; set; }
        public long mobile { get; set; }
        public string nationality { get; set; }
        public string gender { get; set; }
        public string dob { get; set; }
        public string idproof { get; set; }
        public string address { get; set; }
        public string checkin { get; set; }
    }
    public sealed class CustomerRecord
    {
        public int cid { get; set; }
        public string cname { get; set; }
        public long mobile { get; set; }
        public string nationality { get; set; }
        public string gender { get; set; }
        public string dob { get; set; }
        public string idproof { get; set; }
        public string address { get; set; }
        public string checkin { get; set; }
        public string roomNo { get; set; }
        public string roomType { get; set; }
        public string bed { get; set; }
        public long price { get; set; }
        public string checkout { get; set; }
        public string chekout { get; set; }
    }
    public sealed class EmployeeRegistration
    {
        public string ename { get; set; }
        public long mobile { get; set; }
        public string gender { get; set; }
        public string emailid { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public sealed class EmployeeRecord
    {
        public int eid { get; set; }
        public string ename { get; set; }
        public long mobile { get; set; }
        public string gender { get; set; }
        public string emailid { get; set; }
        public string username { get; set; }
    }
}
