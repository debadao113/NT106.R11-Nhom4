using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace HotelData
{
    public static class HotelDatabase
    {
        public static string GetConnectionString()
        {
            string configured = ConfigurationManager.ConnectionStrings["HotelConnection"]?.ConnectionString;
            return string.IsNullOrEmpty(configured)
                ? "Data Source=.;Initial Catalog=dbMyHotel;Integrated Security=True;Connect Timeout=5"
                : configured;
        }

        public static async Task InitializeAsync(string connectionString)
        {
            var settings = new SqlConnectionStringBuilder(connectionString);
            string database = settings.InitialCatalog;
            if (string.IsNullOrWhiteSpace(database)) throw new System.InvalidOperationException("Cấu hình phải có Initial Catalog.");
            string mode = ConfigurationManager.AppSettings["DatabaseInitializationMode"];
            string host = settings.DataSource.Trim();
            if (host.StartsWith("tcp:", System.StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);
            host = host.Split(',')[0];
            bool azureSql = host.EndsWith(".database.windows.net", System.StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(mode)
                && !string.Equals(mode, "Existing", System.StringComparison.OrdinalIgnoreCase)
                && !string.Equals(mode, "CreateIfMissing", System.StringComparison.OrdinalIgnoreCase))
                throw new System.InvalidOperationException("DatabaseInitializationMode phải là Existing hoặc CreateIfMissing.");

            // Azure databases are provisioned separately. Validate the migrated schema
            // without connecting to master or changing tables during application startup.
            if (azureSql || string.Equals(mode, "Existing", System.StringComparison.OrdinalIgnoreCase))
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    const string validateSql = @"
SELECT TOP (0) roomid, roomNo, roomType, bed, price, booked FROM dbo.rooms;
SELECT TOP (0) cid, cname, mobile, nationality, gender, dob, idproof, address, checkin, checkout, chekout, roomid FROM dbo.customer;
SELECT TOP (0) accountId, username, displayName, passwordHash, passwordSalt, passwordIterations, isActive, createdAt FROM dbo.staff_accounts;
SELECT TOP (0) eid, ename, mobile, gender, emailid, username, pass FROM dbo.employee;";
                    using (var command = new SqlCommand(validateSql, connection))
                        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                return;
            }
            settings.InitialCatalog = "master";
            using (var connection = new SqlConnection(settings.ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                string sql = "IF DB_ID(@database) IS NULL CREATE DATABASE [" + database.Replace("]", "]]") + "]";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@database", database);
                    try { await command.ExecuteNonQueryAsync().ConfigureAwait(false); }
                    catch (SqlException ex) when (ex.Number == 1801) { /* Another process created the database. */ }
                }
            }
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                const string sql = @"
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult INT;
EXEC @lockResult = sys.sp_getapplock @Resource = 'NT106.InitializeSchema', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
IF @lockResult < 0 THROW 50001, 'Cannot acquire schema initialization lock.', 1;
IF OBJECT_ID('dbo.rooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.rooms (
        roomid INT IDENTITY(1,1) PRIMARY KEY,
        roomNo VARCHAR(50) NOT NULL UNIQUE,
        roomType VARCHAR(100) NOT NULL,
        bed VARCHAR(50) NOT NULL,
        price BIGINT NOT NULL,
        booked VARCHAR(10) DEFAULT 'NO'
    );
END;
IF OBJECT_ID('dbo.customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.customer (
        cid INT IDENTITY(1,1) PRIMARY KEY,
        cname VARCHAR(250) NOT NULL,
        mobile BIGINT NOT NULL,
        nationality VARCHAR(250) NOT NULL,
        gender VARCHAR(50) NOT NULL,
        dob VARCHAR(50) NOT NULL,
        idproof VARCHAR(250) NOT NULL,
        address VARCHAR(350) NOT NULL,
        checkin VARCHAR(250) NOT NULL,
        checkout VARCHAR(250) DEFAULT 'NO',
        chekout VARCHAR(250) DEFAULT 'NO',
        roomid INT NOT NULL REFERENCES dbo.rooms(roomid)
    );
END;
IF OBJECT_ID('dbo.staff_accounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.staff_accounts (
        accountId INT IDENTITY(1,1) PRIMARY KEY,
        username NVARCHAR(50) NOT NULL UNIQUE,
        displayName NVARCHAR(100) NOT NULL,
        passwordHash VARBINARY(32) NOT NULL,
        passwordSalt VARBINARY(16) NOT NULL,
        passwordIterations INT NOT NULL,
        isActive BIT NOT NULL DEFAULT 1,
        createdAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
IF OBJECT_ID('dbo.employee', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee (
        eid INT IDENTITY(1,1) PRIMARY KEY,
        ename NVARCHAR(250) NOT NULL,
        mobile BIGINT NOT NULL,
        gender NVARCHAR(50) NOT NULL,
        emailid NVARCHAR(250) NOT NULL,
        username NVARCHAR(50) NOT NULL UNIQUE,
        pass NVARCHAR(250) NOT NULL
    );
END;
COMMIT TRANSACTION;";
                using (var command = new SqlCommand(sql, connection))
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
