using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelData;

namespace ChuuongTrinhQuanLyKhachSan.UI
{
    // Own subscriptions for one screen, serialize refreshes and marshal events to the UI.
    internal sealed class CloudScreen
    {
        private readonly UserControl owner;
        private readonly Func<Task> refresh;
        private readonly Action clear;
        private readonly string topic;
        private readonly Label status;
        private bool loading, pending, writing;
        private readonly Timer refreshTimer = new Timer
        {
            Interval = 15000
        };
        public CloudScreen(UserControl owner, string topic, Func<Task> refresh, Action clear)
        {
            this.owner = owner;
            this.topic = topic;
            this.refresh = refresh;
            this.clear = clear;
            status = System.Linq.Enumerable.FirstOrDefault(owner.Controls.Find("PageStatus", true)) as Label;
            if (status == null)
            {
                status = new Label
                {
                    Dock = DockStyle.Bottom,
                    Height = 38,
                    ForeColor = Color.FromArgb(100, 105, 110),
                    Text = "Sẵn sàng cập nhật dữ liệu."
                };
                owner.Controls.Add(status);
                status.BringToFront();
            }

            refreshTimer.Tick += delegate
            {
                if (owner.Visible)
                    Schedule();
            };
            refreshTimer.Start();
            owner.VisibleChanged += VisibleChanged;
            HotelSession.Authenticated += OnAuthenticated;
            HotelSession.AuthenticationLost += OnDisconnected;
            HotelSession.DataChanged += OnEvent;
            owner.Disposed += delegate
            {
                HotelSession.Authenticated -= OnAuthenticated;
                HotelSession.AuthenticationLost -= OnDisconnected;
                HotelSession.DataChanged -= OnEvent;
                refreshTimer.Dispose();
            };
        }

        private void VisibleChanged(object sender, EventArgs e)
        {
            if (owner.Visible)
                Schedule();
        }

        private void OnAuthenticated()
        {
            Schedule();
        }

        private void OnEvent(string changedTopic)
        {
            if (changedTopic == topic)
                Schedule();
        }

        private void OnDisconnected()
        {
            Post(() =>
            {
                clear();
                status.Text = "Mất kết nối. Vui lòng đăng nhập lại qua Phiên đăng nhập.";
            });
        }

        private void Schedule()
        {
            Post(async () => await RefreshAsync());
        }

        private void Post(Action action)
        {
            if (owner.IsDisposed || !owner.IsHandleCreated)
                return;
            try
            {
                owner.BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        public async Task RefreshAsync()
        {
            if (owner.IsDisposed || !owner.Visible || HotelSession.CurrentAccount == null)
                return;
            pending = true;
            if (loading)
                return;
            loading = true;
            try
            {
                do
                {
                    pending = false;
                    status.Text = "Đang cập nhật dữ liệu...";
                    await refresh();
                }
                while (pending && !owner.IsDisposed && owner.Visible && HotelSession.CurrentAccount != null);
                if (!owner.IsDisposed)
                    status.Text = "Đã cập nhật dữ liệu · " + DateTime.Now.ToString("HH:mm");
            }
            catch (Exception ex)
            {
                if (!owner.IsDisposed)
                {
                    clear();
                    status.Text = ex.Message + " Chuyển sang mục khác rồi quay lại để tải lại.";
                }
            }
            finally
            {
                loading = false;
            }
        }

        public async Task WriteAsync(Control button, Func<Task> operation)
        {
            if (writing || owner.IsDisposed)
                return;
            writing = true;
            button.Enabled = false;
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                if (!owner.IsDisposed)
                    MessageBox.Show(owner, ex.Message, "Supabase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                writing = false;
                if (!owner.IsDisposed)
                    button.Enabled = true;
            }
        }
    }
}
