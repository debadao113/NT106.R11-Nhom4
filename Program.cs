using System;
using System.Windows.Forms;
using HotelData;

namespace ChuuongTrinhQuanLyKhachSan
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new LoginForm()); }
            finally { HotelSession.Dispose(); }
        }
    }
}
