using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HotelData;
using HotelNetworking;

namespace HotelServer
{
    internal sealed class OperationException : Exception
    {
        public string Code { get; }
        public OperationException(string code, string message) : base(message) { Code = code; }
    }

    internal sealed class HotelOperationsRepository
    {
        private readonly string connectionString;
        private readonly SemaphoreSlim initializeLock = new SemaphoreSlim(1, 1);
        private bool initialized;
        // Preserve both legacy checkout columns when reading existing data.
        private const string CheckedOut = "(ISNULL(c.chekout,'NO')='YES' OR ISNULL(c.checkout,'NO') NOT IN ('NO',''))";
        public HotelOperationsRepository(string connectionString) { this.connectionString = connectionString; }

        private async Task<SqlConnection> OpenAsync()
        {
            await initializeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!initialized) { await HotelDatabase.InitializeAsync(connectionString).ConfigureAwait(false); initialized = true; }
            }
            finally { initializeLock.Release(); }
            var connection = new SqlConnection(connectionString);
            try { await connection.OpenAsync().ConfigureAwait(false); return connection; }
            catch { connection.Dispose(); throw; }
        }

        internal static string Required(string value, int max, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
                throw new OperationException("VALIDATION_ERROR", field + " cần có nội dung, tối đa " + max + " ký tự.");
            return value.Trim();
        }
        internal static string Date(string value)
        {
            DateTime date;
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                throw new OperationException("VALIDATION_ERROR", "Ngày phải có định dạng yyyy-MM-dd.");
            return value;
        }
        internal static void Positive(long value)
        {
            if (value <= 0) throw new OperationException("VALIDATION_ERROR", "Mã và số điện thoại phải là số nguyên dương.");
        }
        private static void Text(SqlCommand command, string name, string value, int size, bool unicode = false)
            => command.Parameters.Add(name, unicode ? SqlDbType.NVarChar : SqlDbType.VarChar, size).Value = value;

        public async Task<List<CustomerRecord>> GetCustomersAsync(string status, string name)
        {
            if (status != "all" && status != "active" && status != "checkedout")
                throw new OperationException("VALIDATION_ERROR", "Bộ lọc khách hàng không hợp lệ.");
            if (name == null || name.Length > 250) throw new OperationException("VALIDATION_ERROR", "Tên tìm kiếm tối đa 250 ký tự.");
            using (var connection = await OpenAsync().ConfigureAwait(false))
            using (var command = new SqlCommand(@"SELECT c.cid,c.cname,c.mobile,c.nationality,c.gender,c.dob,c.idproof,c.address,c.checkin,
r.roomNo,r.roomType,r.bed,r.price,c.checkout,c.chekout FROM dbo.customer c JOIN dbo.rooms r ON c.roomid=r.roomid
WHERE LEFT(c.cname,LEN(@name))=@name" + (status == "active" ? " AND NOT " + CheckedOut : status == "checkedout" ? " AND " + CheckedOut : "") + " ORDER BY c.cid", connection))
            {
                Text(command, "@name", name, 250);
                var result = new List<CustomerRecord>();
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        result.Add(new CustomerRecord {
                            cid=reader.GetInt32(0), cname=reader.GetString(1), mobile=reader.GetInt64(2), nationality=reader.GetString(3),
                            gender=reader.GetString(4), dob=reader.GetString(5), idproof=reader.GetString(6), address=reader.GetString(7),
                            checkin=reader.GetString(8), roomNo=reader.GetString(9), roomType=reader.GetString(10), bed=reader.GetString(11),
                            price=reader.GetInt64(12), checkout=reader.IsDBNull(13)?null:reader.GetString(13), chekout=reader.IsDBNull(14)?null:reader.GetString(14) });
                return result;
            }
        }

        public async Task<int> RegisterAsync(CustomerRegistration data)
        {
            Positive(data.roomid); Positive(data.mobile);
            var name=Required(data.cname,250,"Họ tên"); var nationality=Required(data.nationality,250,"Quốc tịch");
            var gender=Required(data.gender,50,"Giới tính"); var proof=Required(data.idproof,250,"Giấy tờ");
            var address=Required(data.address,350,"Địa chỉ"); Date(data.dob); Date(data.checkin);
            if (string.CompareOrdinal(data.dob, data.checkin) > 0) throw new OperationException("VALIDATION_ERROR", "Ngày sinh phải trước ngày nhận phòng.");
            using (var connection = await OpenAsync().ConfigureAwait(false))
            using (var transaction = connection.BeginTransaction())
            {
                // Always lock the room before touching its customer rows, including checkout.
                using (var command = new SqlCommand(@"UPDATE dbo.rooms WITH (UPDLOCK,HOLDLOCK) SET booked='YES' WHERE roomid=@room AND ISNULL(booked,'NO')='NO'
AND NOT EXISTS (SELECT 1 FROM dbo.customer c WHERE c.roomid=@room AND NOT " + CheckedOut + ")", connection, transaction))
                {
                    command.Parameters.Add("@room", SqlDbType.Int).Value=data.roomid;
                    if (await command.ExecuteNonQueryAsync().ConfigureAwait(false) != 1)
                        throw new OperationException("ROOM_UNAVAILABLE", "Phòng không còn trống. Hãy tải lại danh sách phòng.");
                }
                int id;
                using (var command = new SqlCommand(@"INSERT dbo.customer(cname,mobile,nationality,gender,dob,idproof,address,checkin,roomid,checkout,chekout)
OUTPUT INSERTED.cid VALUES(@name,@mobile,@nationality,@gender,@dob,@proof,@address,@checkin,@room,'NO','NO')", connection, transaction))
                {
                    Text(command,"@name",name,250); Text(command,"@nationality",nationality,250); Text(command,"@gender",gender,50);
                    Text(command,"@dob",data.dob,50); Text(command,"@proof",proof,250); Text(command,"@address",address,350); Text(command,"@checkin",data.checkin,250);
                    command.Parameters.Add("@mobile",SqlDbType.BigInt).Value=data.mobile; command.Parameters.Add("@room",SqlDbType.Int).Value=data.roomid;
                    id=Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false));
                }
                transaction.Commit(); return id;
            }
        }

        public async Task<bool> CheckoutAsync(int cid, string date)
        {
            Positive(cid); Date(date);
            using (var connection = await OpenAsync().ConfigureAwait(false))
            using (var transaction = connection.BeginTransaction())
            {
                int room;
                using (var command = new SqlCommand("SELECT roomid FROM dbo.customer WHERE cid=@id",connection,transaction))
                {
                    command.Parameters.Add("@id",SqlDbType.Int).Value=cid;
                    var value=await command.ExecuteScalarAsync().ConfigureAwait(false);
                    if (value==null) throw new OperationException("NOT_FOUND","Khách hàng không tồn tại.");
                    room=Convert.ToInt32(value);
                }
                using (var command = new SqlCommand("SELECT roomid FROM dbo.rooms WITH (UPDLOCK,HOLDLOCK) WHERE roomid=@room",connection,transaction))
                {
                    command.Parameters.Add("@room",SqlDbType.Int).Value=room;
                    await command.ExecuteScalarAsync().ConfigureAwait(false);
                }
                using (var command = new SqlCommand("SELECT checkin FROM dbo.customer WHERE cid=@id",connection,transaction))
                {
                    command.Parameters.Add("@id",SqlDbType.Int).Value=cid;
                    string checkin=Convert.ToString(await command.ExecuteScalarAsync().ConfigureAwait(false));
                    DateTime parsed;
                    if(DateTime.TryParseExact(checkin,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out parsed)
                        && string.CompareOrdinal(date,checkin)<0)
                        throw new OperationException("VALIDATION_ERROR","Ngày trả phòng phải từ ngày nhận phòng trở đi.");
                }
                using (var command = new SqlCommand("UPDATE c SET chekout='YES',checkout=@date FROM dbo.customer c WHERE c.cid=@id AND NOT " + CheckedOut,connection,transaction))
                {
                    Text(command,"@date",date,250); command.Parameters.Add("@id",SqlDbType.Int).Value=cid;
                    if (await command.ExecuteNonQueryAsync().ConfigureAwait(false)!=1)
                        throw new OperationException("ALREADY_CHECKED_OUT","Khách hàng đã trả phòng. Hãy tải lại dữ liệu.");
                }
                using (var command = new SqlCommand("UPDATE dbo.rooms SET booked='NO' WHERE roomid=@room AND NOT EXISTS(SELECT 1 FROM dbo.customer c WHERE c.roomid=@room AND NOT " + CheckedOut + ")",connection,transaction))
                {
                    command.Parameters.Add("@room",SqlDbType.Int).Value=room; await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                transaction.Commit(); return true;
            }
        }

        public async Task<List<EmployeeRecord>> GetEmployeesAsync()
        {
            using(var connection=await OpenAsync().ConfigureAwait(false))
            using(var command=new SqlCommand("SELECT eid,ename,mobile,gender,emailid,username FROM dbo.employee ORDER BY eid",connection))
            using(var reader=await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                var result=new List<EmployeeRecord>();
                while(await reader.ReadAsync().ConfigureAwait(false)) result.Add(new EmployeeRecord { eid=reader.GetInt32(0),ename=reader.GetString(1),mobile=reader.GetInt64(2),gender=reader.GetString(3),emailid=reader.GetString(4),username=reader.GetString(5) });
                return result;
            }
        }
        public async Task<int> AddEmployeeAsync(EmployeeRegistration data)
        {
            var name=Required(data.ename,250,"Họ tên"); var gender=Required(data.gender,50,"Giới tính");
            var email=Required(data.emailid,250,"Email"); var username=Required(data.username,50,"Tên tài khoản");
            Required(data.password,128,"Mật khẩu"); Positive(data.mobile);
            if(data.password.Length>128) throw new OperationException("VALIDATION_ERROR","Mật khẩu tối đa 128 ký tự.");
            string password = await Task.Run(() => {
                using(var hash=new Rfc2898DeriveBytes(data.password,16,210000,HashAlgorithmName.SHA256))
                    return "PBKDF2$210000$"+Convert.ToBase64String(hash.Salt)+"$"+Convert.ToBase64String(hash.GetBytes(32));
            }).ConfigureAwait(false);
            using(var connection=await OpenAsync().ConfigureAwait(false))
            using(var command=new SqlCommand("INSERT dbo.employee(ename,mobile,gender,emailid,username,pass) OUTPUT INSERTED.eid VALUES(@name,@mobile,@gender,@email,@username,@pass)",connection))
            {
                Text(command,"@name",name,250,true); Text(command,"@gender",gender,50,true); Text(command,"@email",email,250,true);
                Text(command,"@username",username,50,true); Text(command,"@pass",password,250,true); command.Parameters.Add("@mobile",SqlDbType.BigInt).Value=data.mobile;
                try { return Convert.ToInt32(await command.ExecuteScalarAsync().ConfigureAwait(false)); }
                catch(SqlException ex) when(ex.Number==2601 || ex.Number==2627) { throw new OperationException("EMPLOYEE_EXISTS","Tên tài khoản nhân viên đã tồn tại."); }
            }
        }
        public async Task<bool> DeleteEmployeeAsync(int eid)
        {
            Positive(eid);
            using(var connection=await OpenAsync().ConfigureAwait(false))
            using(var command=new SqlCommand("DELETE dbo.employee WHERE eid=@id",connection))
            {
                command.Parameters.Add("@id",SqlDbType.Int).Value=eid;
                if(await command.ExecuteNonQueryAsync().ConfigureAwait(false)!=1) throw new OperationException("NOT_FOUND","Nhân viên không tồn tại.");
                return true;
            }
        }
    }
}
