using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HotelData;
using HotelNetworking;

namespace HotelServer
{
    public sealed class StaffAccountRepository
    {
        private const int Iterations = 210000;
        private readonly string connectionString;
        public StaffAccountRepository(string connectionString = null)
        {
            this.connectionString = connectionString ?? HotelDatabase.GetConnectionString();
        }

        public async Task<List<StaffAccount>> GetAccountsAsync()
        {
            await HotelDatabase.InitializeAsync(connectionString).ConfigureAwait(false);
            var accounts = new List<StaffAccount>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                using (var command = new SqlCommand("SELECT accountId, username, displayName FROM dbo.staff_accounts WHERE isActive = 1 ORDER BY username", connection))
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    while (await reader.ReadAsync().ConfigureAwait(false)) accounts.Add(ReadAccount(reader));
            }
            return accounts;
        }

        public async Task CreateAsync(string username, string displayName, string password)
        {
            username = (username ?? "").Trim();
            displayName = (displayName ?? "").Trim();
            if (!Regex.IsMatch(username, @"\A[A-Za-z0-9_.-]{3,50}\z") || displayName.Length == 0 || displayName.Length > 100
                || string.IsNullOrEmpty(password) || password.Length > 128)
                throw new ArgumentException("Tên đăng nhập gồm 3–50 chữ, số hoặc dấu _ . -. Họ tên tối đa 100 ký tự. Mật khẩu từ 1–128 ký tự.");
            await HotelDatabase.InitializeAsync(connectionString).ConfigureAwait(false);
            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            // Run password derivation on the thread pool to keep the server window responsive.
            var hash = await Task.Run(() => Hash(password, salt, Iterations)).ConfigureAwait(false);
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                const string sql = @"INSERT INTO dbo.staff_accounts(username, displayName, passwordHash, passwordSalt, passwordIterations)
VALUES (@username, @displayName, @hash, @salt, @iterations)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username;
                    command.Parameters.Add("@displayName", SqlDbType.NVarChar, 100).Value = displayName;
                    command.Parameters.Add("@hash", SqlDbType.VarBinary, 32).Value = hash;
                    command.Parameters.Add("@salt", SqlDbType.VarBinary, 16).Value = salt;
                    command.Parameters.Add("@iterations", SqlDbType.Int).Value = Iterations;
                    try { await command.ExecuteNonQueryAsync().ConfigureAwait(false); }
                    catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                    {
                        throw new ArgumentException("Tên đăng nhập đã tồn tại.");
                    }
                }
            }
        }

        public async Task<StaffAccount> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 50 || string.IsNullOrEmpty(password) || password.Length > 128)
                return null;
            await HotelDatabase.InitializeAsync(connectionString).ConfigureAwait(false);
            StaffAccount account = null;
            byte[] expected = new byte[32];
            byte[] salt = new byte[16];
            int iterations = Iterations;
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                const string sql = "SELECT accountId, username, displayName, passwordHash, passwordSalt, passwordIterations FROM dbo.staff_accounts WHERE username=@username AND isActive=1";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username.Trim();
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            account = ReadAccount(reader);
                            expected = (byte[])reader[3];
                            salt = (byte[])reader[4];
                            iterations = reader.GetInt32(5);
                        }
                }
            }
            if (account == null) return await AuthenticateEmployeeAsync(username.Trim(), password).ConfigureAwait(false);
            if (expected.Length != 32 || salt.Length != 16 || iterations < 10000 || iterations > 1000000) return null;
            var actual = await Task.Run(() => Hash(password, salt, iterations)).ConfigureAwait(false);
            int difference = expected.Length ^ actual.Length;
            for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
            return difference == 0 ? account : null;
        }

        private async Task<StaffAccount> AuthenticateEmployeeAsync(string username, string password)
        {
            StaffAccount account = null;
            string encoded = "";
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                // A staff account, including a disabled one, owns its username. Never
                // fall back to an employee password with the same username.
                const string sql = @"SELECT eid,ename,pass FROM dbo.employee WHERE username=@username
AND NOT EXISTS(SELECT 1 FROM dbo.staff_accounts WHERE username=@username)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username;
                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                        if (await reader.ReadAsync().ConfigureAwait(false))
                        {
                            account = new StaffAccount { username=username, displayName=reader.GetString(1), accountType="employee", employeeId=reader.GetInt32(0) };
                            encoded=reader.GetString(2);
                        }
                }
            }
            byte[] salt=new byte[16], expected=new byte[32];
            int iterations=Iterations;
            var parts=encoded.Split('$');
            bool valid=false;
            if(parts.Length==4 && parts[0]=="PBKDF2" && int.TryParse(parts[1],out iterations) && iterations>=10000 && iterations<=1000000)
            {
                try { salt=Convert.FromBase64String(parts[2]); expected=Convert.FromBase64String(parts[3]); valid=salt.Length==16 && expected.Length==32; }
                catch(FormatException) { }
            }
            if(!valid) { iterations=Iterations; salt=new byte[16]; expected=new byte[32]; }
            var actual=await Task.Run(()=>Hash(password,salt,iterations)).ConfigureAwait(false);
            int difference=0;
            for(int i=0;i<actual.Length;i++) difference|=actual[i]^expected[i];
            return valid && difference==0 ? account : null;
        }

        private static byte[] Hash(string password, byte[] salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return derive.GetBytes(32);
        }

        private static StaffAccount ReadAccount(SqlDataReader reader)
        {
            return new StaffAccount { accountId = reader.GetInt32(0), username = reader.GetString(1), displayName = reader.GetString(2) };
        }
    }
}
