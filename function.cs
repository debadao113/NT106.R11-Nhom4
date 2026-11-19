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
            return HotelData.HotelDatabase.GetConnectionString();
        }

        public static void InitializeDatabase()
        {
            try
            {
                HotelData.HotelDatabase.InitializeAsync(GetConnectionString()).GetAwaiter().GetResult();
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
