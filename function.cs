using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Data;
using System.Configuration;

namespace ChuuongTrinhQuanLyKhachSan
{
    internal class function
    {
        public static string GetConnectionString()
        {
            string connString = ConfigurationManager.ConnectionStrings["HotelConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(connString))
            {
                connString = "Data Source=.;Initial Catalog=dbMyHotel;Integrated Security=True";
            }
            return connString;
        }

        public static void InitializeDatabase()
        {
            try
            {
                string connString = GetConnectionString();
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connString);
                string dbName = string.IsNullOrEmpty(builder.InitialCatalog) ? "dbMyHotel" : builder.InitialCatalog;

                // 1. Ket noi master de kiem tra va tao database neu chua co
                SqlConnectionStringBuilder masterBuilder = new SqlConnectionStringBuilder(connString)
                {
                    InitialCatalog = "master"
                };

                using (SqlConnection masterConn = new SqlConnection(masterBuilder.ConnectionString))
                {
                    masterConn.Open();
                    string checkDbSql = $@"
                        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{dbName.Replace("'", "''")}')
                        BEGIN
                            CREATE DATABASE [{dbName.Replace("]", "]]")}];
                        END";
                    using (SqlCommand cmd = new SqlCommand(checkDbSql, masterConn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // 2. Ket noi vao database dbMyHotel de tao cac bang neu chua co
                using (SqlConnection dbConn = new SqlConnection(connString))
                {
                    dbConn.Open();
                    string createTablesSql = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'rooms')
                        BEGIN
                            CREATE TABLE rooms (
                                roomid INT IDENTITY(1,1) PRIMARY KEY,
                                roomNo VARCHAR(50) NOT NULL UNIQUE,
                                roomType VARCHAR(100) NOT NULL,
                                bed VARCHAR(50) NOT NULL,
                                price BIGINT NOT NULL,
                                booked VARCHAR(10) DEFAULT 'NO'
                            );
                        END";
                    using (SqlCommand cmd = new SqlCommand(createTablesSql, dbConn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Khởi tạo kết nối CSDL thất bại:\n" + ex.Message + 
                    "\n\nVui lòng kiểm tra dịch vụ SQL Server và cấu hình trong file App.config.", 
                    "Cảnh báo CSDL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected SqlConnection getConnection()
        {
            return new SqlConnection(GetConnectionString());
        }

        public DataSet GetData(string query)
        {
            using (SqlConnection con = getConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataSet ds = new DataSet();
                        da.Fill(ds);
                        return ds;
                    }
                }
            }
        }

        public void setData(string query, string message)
        {
            using (SqlConnection con = getConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }
            }

            MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public SqlDataReader getForCombo(string query)
        {
            SqlConnection con = getConnection();
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return sdr;
        }
    }
}
