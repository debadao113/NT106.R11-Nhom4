using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using HotelData;
using ChuuongTrinhQuanLyKhachSan.UI;

namespace ChuuongTrinhQuanLyKhachSan
{
    public partial class DashboardForm : Form
    {
        private readonly Timer clock = new Timer
        {
            Interval = 60000
        };
        public DashboardForm()
        {
            InitializeComponent();
            DoubleBuffered = true;
            var workArea = Screen.FromControl(this).WorkingArea;
            ClientSize = new Size(Math.Min(1280, workArea.Width - 64), Math.Min(780, workArea.Height - 72));
            UiTheme.SetIcon(btnOverview, "home", UiTheme.Primary);
            UiTheme.SetIcon(btnAddRoom, "rooms", UiTheme.Primary);
            UiTheme.SetIcon(btnCustomerRes, "in", UiTheme.Primary);
            UiTheme.SetIcon(btnCheckOut, "out", UiTheme.Primary);
            UiTheme.SetIcon(btnCustomerDetail, "people", UiTheme.Primary);
            UiTheme.SetIcon(btnEmployee, "staff", UiTheme.Primary);
            UiTheme.SetIcon(btnAccounts, "staff", UiTheme.Primary);
            overviewPage.CheckInRequested += delegate
            {
                btnCustomerRes.PerformClick();
            };
            overviewPage.RoomsRequested += delegate
            {
                btnAddRoom.PerformClick();
            };
            HotelSession.Authenticated += UpdateSession;
            HotelSession.AuthenticationLost += UpdateSession;
            clock.Tick += delegate
            {
                UpdateDate();
            };
            clock.Start();
            UpdateDate();
            UpdateAccountTitle();
            Disposed += delegate
            {
                clock.Dispose();
                HotelSession.Authenticated -= UpdateSession;
                HotelSession.AuthenticationLost -= UpdateSession;
            };
        }

        private void UpdateDate()
        {
            dateLabel.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));
        }

        private void UpdateSession()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            try
            {
                BeginInvoke(new Action(UpdateAccountTitle));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void UpdateAccountTitle()
        {
            var account = HotelSession.CurrentAccount;
            sessionLabel.Text = account == null ? "Chưa có phiên làm việc" : "Đang làm việc · " + account.DisplayName;
            btnEmployee.Enabled = account != null && account.IsAdmin;
            btnAccounts.Visible = account?.IsAdmin == true;
            if (employeesControl.Visible && (account == null || !account.IsAdmin))
                btnOverview.PerformClick();
            Text = "HotelDesk | " + (account?.DisplayName ?? "Không gian lễ tân");
        }

        private void ShowPage(Control selected, Guna2Button active, string title, string description)
        {
            panel2.SuspendLayout();
            foreach (Control page in panel2.Controls)
                page.Visible = page == selected;
            panel2.AutoScrollPosition = Point.Empty;
            selected.Dock = DockStyle.Fill;
            selected.BringToFront();
            foreach (var button in new[]
            {
                btnOverview,
                btnAddRoom,
                btnCustomerRes,
                btnCheckOut,
                btnCustomerDetail,
                btnEmployee
            })
                button.Checked = button == active;
            pageTitle.Text = title;
            pageDescription.Text = description;
            panel2.ResumeLayout(true);
        }

        private void DashboardForm_Load(object sender, EventArgs e)
        {
            btnOverview.PerformClick();
        }

        private void btnOverview_Click(object sender, EventArgs e)
        {
            ShowPage(overviewPage, btnOverview, "Tổng quan", "Theo dõi phòng và quản lý công việc trong ngày.");
        }

        private void btnAddRoom_Click(object sender, EventArgs e)
        {
            ShowPage(roomsControl, btnAddRoom, "Quản lý phòng", "Danh sách phòng, giá và tình trạng lưu trú.");
        }

        private void btnCustomerRes_Click(object sender, EventArgs e)
        {
            ShowPage(checkInControl, btnCustomerRes, "Nhận phòng", "Bắt đầu một kỳ lưu trú mới cho khách hàng.");
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            ShowPage(checkOutControl, btnCheckOut, "Trả phòng", "Hoàn tất lưu trú và sẵn sàng đón khách mới.");
        }

        private void btnCustomerDetail_Click(object sender, EventArgs e)
        {
            ShowPage(customersControl, btnCustomerDetail, "Khách hàng", "Thông tin và lịch sử lưu trú trong một nơi.");
        }

        private void btnEmployee_Click(object sender, EventArgs e)
        {
            ShowPage(employeesControl, btnEmployee, "Nhân viên", "Quản lý hồ sơ nhân viên. Tài khoản đăng nhập được cấp tại Supabase Auth.");
        }

        private void btnAccounts_Click(object sender, EventArgs e)
        {
            if (HotelSession.CurrentAccount?.IsAdmin != true) return;
            using (var dialog = new AdminAccountsForm()) dialog.ShowDialog(this);
        }

        public bool LoggedOut { get; private set; }

        private async void btnLogout_Click(object sender, EventArgs e)
        {
            Enabled = false;
            try
            {
                await HotelSession.LogoutAsync();
            }
            catch (Exception)
            {
                // LogoutAsync clears the local session even if revoking it fails.
                if (!IsDisposed)
                    MessageBox.Show(this, "Đã đăng xuất trên máy. Không thể kết nối để kết thúc phiên trên Supabase.",
                        "Đăng xuất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                LoggedOut = true;
                if (!IsDisposed) Close();
            }
        }
    }
}
