using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HotelNetworking
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
        public CloudScreen(UserControl owner, string topic, Func<Task> refresh, Action clear)
        {
            this.owner=owner; this.topic=topic; this.refresh=refresh; this.clear=clear;
            status=new Label { Dock=DockStyle.Bottom,Height=38,ForeColor=Color.DarkBlue,Text="Dữ liệu được tải từ HotelServer." };
            owner.Controls.Add(status); status.BringToFront();
            owner.VisibleChanged+=VisibleChanged;
            HotelConnection.Authenticated+=OnAuthenticated;
            HotelConnection.Client.EventReceived+=OnEvent;
            HotelConnection.Client.Disconnected+=OnDisconnected;
            owner.Disposed+=delegate {
                HotelConnection.Authenticated-=OnAuthenticated;
                HotelConnection.Client.EventReceived-=OnEvent;
                HotelConnection.Client.Disconnected-=OnDisconnected;
            };
        }
        private void VisibleChanged(object sender,EventArgs e) { if(owner.Visible) Schedule(); }
        private void OnAuthenticated() { Schedule(); }
        private void OnEvent(NetworkMessage message) { if(message.action==topic) Schedule(); }
        private void OnDisconnected() { Post(()=> { clear(); status.Text="Mất kết nối. Vui lòng đăng nhập lại qua Kết nối server."; }); }
        private void Schedule() { Post(async()=>await RefreshAsync()); }
        private void Post(Action action)
        {
            if(owner.IsDisposed || !owner.IsHandleCreated) return;
            try { owner.BeginInvoke(action); } catch(InvalidOperationException) { }
        }
        public async Task RefreshAsync()
        {
            if(owner.IsDisposed || !owner.Visible || HotelConnection.CurrentAccount==null) return;
            pending=true;
            if(loading) return;
            loading=true;
            try
            {
                do
                {
                    pending=false; status.Text="Đang tải dữ liệu từ HotelServer...";
                    await refresh();
                } while(pending && !owner.IsDisposed && owner.Visible && HotelConnection.CurrentAccount!=null);
                if(!owner.IsDisposed) status.Text="Đã tải dữ liệu từ HotelServer.";
            }
            catch(Exception ex) { if(!owner.IsDisposed) { clear(); status.Text=ex.Message+" Chuyển sang mục khác rồi quay lại để tải lại."; } }
            finally { loading=false; }
        }
        public async Task WriteAsync(Control button,Func<Task> operation)
        {
            if(writing || owner.IsDisposed) return;
            writing=true; button.Enabled=false;
            try { await operation(); }
            catch(Exception ex) { if(!owner.IsDisposed) MessageBox.Show(owner,ex.Message,"HotelServer",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
            finally { writing=false; if(!owner.IsDisposed) button.Enabled=true; }
        }
    }
}
