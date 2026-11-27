using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HotelData;

namespace ChuuongTrinhQuanLyKhachSan.UI
{
    public sealed class OverviewPage : UserControl
    {
        private readonly Label totalValue, availableValue, occupiedValue, occupancyValue, availableHint;
        private readonly Label roomCount;
        private readonly Guna2ProgressBar occupancy;
        private readonly DataGridView roomsGrid;
        private readonly CloudScreen cloud;
        public event Action CheckInRequested;
        public event Action RoomsRequested;
        public OverviewPage()
        {
            BackColor = UiTheme.Canvas;
            MinimumSize = new Size(680, 560);
            var root = CreateLayout(1, 4);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            Controls.Add(root);
            var metrics = CreateLayout(4, 1);
            for (int i = 0; i < 4; i++)
                metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            root.Controls.Add(metrics, 0, 0);
            totalValue = Metric(metrics, 0, "Tổng số phòng", "Trong danh mục", UiTheme.Ink, "TotalRooms");
            availableValue = Metric(metrics, 1, "Phòng trống", "Sẵn sàng đón khách", Color.FromArgb(0, 120, 212), "AvailableRooms");
            occupiedValue = Metric(metrics, 2, "Phòng có khách", "Đang được sử dụng", Color.FromArgb(24, 24, 24), "OccupiedRooms");
            occupancyValue = Metric(metrics, 3, "Công suất phòng", "Theo tình trạng hiện tại", UiTheme.Primary, "OccupancyRate");
            var content = CreateLayout(2, 1);
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 73));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            root.Controls.Add(content, 0, 2);
            var roomCard = Card();
            content.Controls.Add(roomCard, 0, 0);
            var roomLayout = CreateLayout(1, 3);
            roomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            roomLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            roomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            roomCard.Controls.Add(roomLayout);
            var title = UiTheme.Label("Tình trạng phòng", 13, true);
            title.Dock = DockStyle.Fill;
            roomLayout.Controls.Add(title, 0, 0);
            roomCount = UiTheme.Label("Dữ liệu sẽ được cập nhật khi kết nối.", 9);
            roomCount.ForeColor = UiTheme.Muted;
            roomCount.Dock = DockStyle.Fill;
            roomLayout.Controls.Add(roomCount, 0, 1);
            roomsGrid = new Guna2DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "OverviewRooms"
            };
            UiTheme.StyleGrid(roomsGrid);
            roomLayout.Controls.Add(roomsGrid, 0, 2);
            var actionCard = Card();
            content.Controls.Add(actionCard, 1, 0);
            var side = CreateLayout(1, 9);
            foreach (int height in new[]
            {
                36,
                40,
                14,
                52,
                48,
                48,
                34,
                42
            })
                side.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            actionCard.Controls.Add(side);
            side.Controls.Add(UiTheme.Label("Thao tác nhanh", 12, true), 0, 0);
            var checkIn = UiTheme.Button("Nhận phòng", true);
            checkIn.Dock = DockStyle.Fill;
            checkIn.Margin = new Padding(0);
            side.Controls.Add(checkIn, 0, 1);
            checkIn.Click += delegate
            {
                CheckInRequested?.Invoke();
            };
            availableHint = UiTheme.Label("Đang chờ cập nhật phòng trống.", 9);
            availableHint.Dock = DockStyle.Fill;
            availableHint.ForeColor = UiTheme.Muted;
            side.Controls.Add(availableHint, 0, 3);
            var view = UiTheme.Button("Quản lý phòng");
            view.Dock = DockStyle.Fill;
            view.Margin = new Padding(0, 0, 0, 8);
            side.Controls.Add(view, 0, 4);
            view.Click += delegate
            {
                RoomsRequested?.Invoke();
            };
            var refresh = UiTheme.Button("Làm mới dữ liệu");
            refresh.Dock = DockStyle.Fill;
            refresh.Margin = new Padding(0, 0, 0, 8);
            side.Controls.Add(refresh, 0, 5);
            var occupancyTitle = UiTheme.Label("MỨC SỬ DỤNG PHÒNG", 8.5F, true);
            occupancyTitle.ForeColor = UiTheme.Muted;
            side.Controls.Add(occupancyTitle, 0, 6);
            occupancy = new Guna2ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 9,
                Margin = new Padding(0, 6, 0, 0),
                BorderRadius = 4,
                FillColor = UiTheme.PrimarySoft,
                ProgressColor = UiTheme.Primary,
                ProgressColor2 = UiTheme.Primary
            };
            side.Controls.Add(occupancy, 0, 7);
            var status = UiTheme.Label("Đang chờ dữ liệu.", 9);
            status.Name = "PageStatus";
            status.Dock = DockStyle.Fill;
            status.ForeColor = UiTheme.Muted;
            root.Controls.Add(status, 0, 3);
            cloud = new CloudScreen(this, "ROOMS_CHANGED", RefreshAsync, Clear);
            refresh.Click += async delegate
            {
                await cloud.RefreshAsync();
            };
            Load += async delegate
            {
                await cloud.RefreshAsync();
            };
        }

        private static TableLayoutPanel CreateLayout(int columns, int rows) => new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };
        private static Guna2Panel Card() => new Guna2Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            Padding = new Padding(18),
            BackColor = Color.Transparent,
            BorderRadius = 10,
            FillColor = Color.White,
            BorderThickness = 1,
            BorderColor = UiTheme.Line
        };
        private static Label Metric(TableLayoutPanel parent, int column, string title, string caption, Color accent, string name)
        {
            var card = Card();
            card.Padding = new Padding(16, 10, 12, 8);
            parent.Controls.Add(card, column, 0);
            var rows = CreateLayout(1, 3);
            rows.BackColor = Color.White;
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(rows);
            var label = UiTheme.Label(title, 9.5F);
            label.ForeColor = UiTheme.Muted;
            rows.Controls.Add(label, 0, 0);
            var value = UiTheme.Label("—", 26, true);
            value.Name = name;
            value.ForeColor = accent;
            rows.Controls.Add(value, 0, 1);
            var description = UiTheme.Label(caption, 8.3F);
            description.ForeColor = UiTheme.Muted;
            rows.Controls.Add(description, 0, 2);
            return value;
        }

        private async Task RefreshAsync()
        {
            var rooms = await RoomService.GetRoomsAsync();
            if (IsDisposed || HotelSession.CurrentAccount == null)
                return;
            int available = rooms.Count(room => !room.IsOccupied);
            int used = rooms.Count - available;
            totalValue.Text = rooms.Count.ToString();
            availableValue.Text = available.ToString();
            occupiedValue.Text = used.ToString();
            int percent = rooms.Count == 0 ? 0 : (int)Math.Round(100.0 * used / rooms.Count);
            occupancy.Value = percent;
            occupancyValue.Text = percent + "%";
            availableHint.Text = available + " phòng đang sẵn sàng đón khách.";
            roomCount.Text = rooms.Count <= 8 ? rooms.Count + " phòng trong danh mục" : "Hiển thị 8 / " + rooms.Count + " phòng. Mở Quản lý phòng để xem tất cả.";
            roomsGrid.DataSource = rooms.OrderBy(room => room.Number).Take(8).ToList();
            if (roomsGrid.Columns.Contains("Bed"))
                roomsGrid.Columns["Bed"].Visible = false;
        }

        private void Clear()
        {
            totalValue.Text = availableValue.Text = occupiedValue.Text = occupancyValue.Text = "—";
            occupancy.Value = 0;
            roomsGrid.DataSource = null;
            roomCount.Text = "Chưa có dữ liệu cập nhật.";
            availableHint.Text = "Kết nối lại để xem phòng trống.";
        }
    }
}
