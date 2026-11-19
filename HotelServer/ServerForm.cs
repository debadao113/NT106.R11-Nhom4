using System;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace HotelServer
{
    public sealed class ServerForm : Form
    {
        public ServerForm()
        {
            Text = "HotelServer - Quản lý kết nối khách sạn";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(600, 240);
            MinimumSize = new Size(520, 270);
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 2,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var title = CreateLabel("HotelServer", true);
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 2);

            layout.Controls.Add(CreateLabel("Địa chỉ lắng nghe"), 0, 1);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0"), 1, 1);
            layout.Controls.Add(CreateLabel("Cổng TCP"), 0, 2);
            layout.Controls.Add(CreateLabel(ConfigurationManager.AppSettings["ListenPort"] ?? "8888"), 1, 2);

            var status = CreateLabel("Đã tải cấu hình. Chức năng bật TCP sẽ được bổ sung ở bước tiếp theo.");
            IPAddress address;
            int port;
            if (!IPAddress.TryParse(ConfigurationManager.AppSettings["ListenAddress"] ?? "0.0.0.0", out address)
                || !int.TryParse(ConfigurationManager.AppSettings["ListenPort"] ?? "8888", out port)
                || port < 1 || port > 65535)
            {
                status.Text = "Cấu hình chưa hợp lệ. Kiểm tra ListenAddress và ListenPort trong HotelServer.exe.config.";
                status.ForeColor = Color.Firebrick;
            }

            layout.Controls.Add(status, 0, 3);
            layout.SetColumnSpan(status, 2);
            Controls.Add(layout);
        }

        private static Label CreateLabel(string text, bool title = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 12),
                Font = new Font("Segoe UI", title ? 18F : 10F, title ? FontStyle.Bold : FontStyle.Regular)
            };
        }
    }
}
