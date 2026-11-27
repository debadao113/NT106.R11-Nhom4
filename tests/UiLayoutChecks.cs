using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ChuuongTrinhQuanLyKhachSan;
using HotelData;

// Exercises only the client layout. No login, network request or database write.
internal static class UiLayoutChecks
{
    private static string output;
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            output = args.Length == 0 ? Path.Combine(Path.GetTempPath(), "NT106-ui-review") : args[0];
            Directory.CreateDirectory(output);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var login = new LoginForm())
            {
                foreach (var size in new[]
                {
                    new Size(820, 460),
                    new Size(900, 500),
                    new Size(980, 680),
                    new Size(1080, 720),
                    new Size(1440, 900)
                })
                {
                    Prepare(login, size);
                    foreach (string name in new[]
                    {
                        "txtUsername",
                        "txtPassword",
                        "btnLogin"
                    })
                    {
                        var control = Find(login, name);
                        Assert(control.Width >= 280 && control.Height >= 38, "Collapsed login control: " + name + " at " + size);
                        Assert(InsideAncestors(control, login), "Clipped login control: " + name + " at " + size);
                    }

                    Capture(login, "login-" + size.Width);
                    var hotelImage = Find(login, "guna2PictureBox1");
                    Assert(hotelImage.Width >= 200 && hotelImage.Height >= 200, "Hotel illustration is clipped.");
                    Assert(hotelImage.GetType().GetProperty("Image").GetValue(hotelImage) != null, "Hotel illustration asset is missing.");
                }

                Find(login, "showPassword").GetType().GetProperty("Checked").SetValue(Find(login, "showPassword"), true);
                bool passwordHidden = (bool)Find(login, "txtPassword").GetType().GetProperty("UseSystemPasswordChar").GetValue(Find(login, "txtPassword"));
                Assert(!passwordHidden, "Show-password toggle is not wired.");
            }

            using (var accountForm = new AdminAccountsForm())
            {
                foreach (var size in new[] { new Size(780, 640), new Size(850, 650) })
                {
                    Prepare(accountForm, size);
                    foreach (string name in new[] { "NewAdminUsername", "NewAdminDisplayName", "NewAdminPassword", "NewAdminPasswordConfirmation", "CreateAdminButton" })
                        Assert(InsideAncestors(Find(accountForm, name), accountForm), "Clipped admin field: " + name);
                    Capture(accountForm, "create-admin-" + size.Width);
                }
            }

            using (var shell = new DashboardForm())
            {
                foreach (var size in new[]
                {
                    new Size(980, 700),
                    new Size(1320, 820)
                })
                {
                    Prepare(shell, size);
                    foreach (var route in new[]
                    {
                        new[]
                        {
                            "btnOverview_Click",
                            "overviewPage",
                            "OverviewRooms"
                        },
                        new[]
                        {
                            "btnAddRoom_Click",
                            "roomsControl",
                            "btnAddRoom"
                        },
                        new[]
                        {
                            "btnCustomerRes_Click",
                            "checkInControl",
                            "btnAlloteRoom"
                        },
                        new[]
                        {
                            "btnCheckOut_Click",
                            "checkOutControl",
                            "btnCheckOut"
                        },
                        new[]
                        {
                            "btnCustomerDetail_Click",
                            "customersControl",
                            "txtSearchBy"
                        },
                        new[]
                        {
                            "btnEmployee_Click",
                            "employeesControl",
                            "btnRegistation"
                        }
                    })
                    {
                        typeof(DashboardForm).GetMethod(route[0], BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shell, new object[] { shell, EventArgs.Empty });
                        LayoutTree(shell);
                        var page = Find(shell, route[1]);
                        var action = Find(page, route[2]);
                        Assert(action.Width >= 95 && action.Height >= 30, "Collapsed action: " + route[2] + " at " + size);
                        Assert(InsideAncestors(action, page), "Clipped page action: " + route[2] + " at " + size);
                        Capture(shell, route[1] + "-" + size.Width);
                    }
                }

                var roomGrid = (DataGridView)Find(shell, "DataGridView1");
                roomGrid.BindingContext = new BindingContext();
                roomGrid.DataSource = new[] { new Room { Id = 1, Number = "101", IsOccupied = true, Price = 500000 } };
                Assert(roomGrid.Columns["IsOccupied"] is DataGridViewTextBoxColumn, "Room status must use text cells.");
                Assert(Convert.ToString(roomGrid.Rows[0].Cells["IsOccupied"].FormattedValue) == "Có khách", "Room status label failed.");
                var customerGrid = (DataGridView)Find(Find(shell, "customersControl"), "guna2DataGridView1");
                customerGrid.BindingContext = new BindingContext();
                customerGrid.DataSource = new[] { new CustomerRecord { Id = 1, Name = "Test", CheckOut = "2026-01-02" } };
                Assert(customerGrid.Columns["HasCheckedOut"] is DataGridViewTextBoxColumn, "Stay status must use text cells.");
                Assert(Convert.ToString(customerGrid.Rows[0].Cells["HasCheckedOut"].FormattedValue) == "Đã trả phòng", "Stay status label failed.");
                var employee = Find(shell, "employeesControl");
                var tabs = (TabControl)Find(employee, "tabControl1");
                tabs.SelectedIndex = 2;
                LayoutTree(shell);
                Assert(InsideAncestors(Find(employee, "btnDelete"), employee), "Delete action is clipped.");
                Capture(shell, "employee-delete");
            }

            Console.WriteLine("PASS: Login controls remain visible at 820/900/980/1080/1440 widths; password toggle works.");
            Console.WriteLine("PASS: All six routes and their primary actions fit at compact and desktop sizes.");
            Console.WriteLine("UI review images: " + output);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    private static void Prepare(Form form, Size size)
    {
        form.ClientSize = size;
        CreateHandles(form);
        LayoutTree(form);
    }

    private static void CreateHandles(Control control)
    {
        var handle = control.Handle;
        foreach (Control child in control.Controls)
            CreateHandles(child);
    }

    private static void LayoutTree(Control control)
    {
        control.PerformLayout();
        foreach (Control child in control.Controls)
            LayoutTree(child);
        control.PerformLayout();
    }

    private static Control Find(Control root, string name)
    {
        var result = root.Controls.Find(name, true).FirstOrDefault();
        if (result == null)
            throw new Exception("Missing control: " + name);
        return result;
    }

    private static bool InsideAncestors(Control control, Control boundary)
    {
        while (control != boundary)
        {
            var parent = control.Parent;
            if (parent == null)
                return false;
            var bounds = control.Bounds;
            if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > parent.ClientSize.Width + 1 || bounds.Bottom > parent.ClientSize.Height + 1)
                return false;
            control = parent;
        }

        return true;
    }

    private static void Capture(Control control, string name)
    {
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
