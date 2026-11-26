using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace ChuuongTrinhQuanLyKhachSan.UI
{
    internal static class UiTheme
    {
        public static readonly Color Ink = Color.FromArgb(24, 24, 24);
        public static readonly Color Muted = Color.FromArgb(100, 105, 110);
        public static readonly Color Canvas = Color.FromArgb(245, 249, 253);
        public static readonly Color Navy = Color.FromArgb(32, 32, 32);
        public static readonly Color Primary = Color.FromArgb(0, 120, 212);
        public static readonly Color PrimaryHover = Color.FromArgb(0, 103, 184);
        public static readonly Color PrimarySoft = Color.FromArgb(232, 244, 255);
        public static readonly Color Line = Color.FromArgb(221, 231, 239);
        public static Label Label(string text, float size = 10, bool bold = false)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Ink,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        public static Guna2Button Button(string text, bool primary = false)
        {
            var button = new Guna2Button
            {
                Text = text,
                Height = 42,
                Width = 150,
                BorderRadius = 7,
                Cursor = Cursors.Hand,
                FillColor = primary ? Primary : Color.White,
                ForeColor = primary ? Color.White : Ink,
                BorderColor = Line,
                BorderThickness = primary ? 0 : 1,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Margin = new Padding(0, 0, 10, 0)
            };
            button.HoverState.FillColor = primary ? PrimaryHover : PrimarySoft;
            button.HoverState.ForeColor = primary ? Color.White : Ink;
            button.PressedColor = PrimaryHover;
            button.DisabledState.FillColor = Color.FromArgb(221, 231, 239);
            button.DisabledState.ForeColor = Color.FromArgb(100, 105, 110);
            return button;
        }

        public static string Money(long value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
        public static void StyleGrid(DataGridView grid)
        {
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoGenerateColumns = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.GridColor = Line;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 44;
            grid.RowTemplate.Height = 46;
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Ink,
                BackColor = Color.White,
                SelectionBackColor = PrimarySoft,
                SelectionForeColor = Ink,
                Padding = new Padding(8, 0, 5, 0)
            };
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Canvas,
                SelectionBackColor = Canvas,
                Padding = new Padding(8, 0, 5, 0)
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 252, 253)
            };
            var guna = grid as Guna2DataGridView;
            if (guna != null)
            {
                guna.ThemeStyle.HeaderStyle.BackColor = Canvas;
                guna.ThemeStyle.HeaderStyle.ForeColor = Muted;
                guna.ThemeStyle.HeaderStyle.Font = grid.ColumnHeadersDefaultCellStyle.Font;
                guna.ThemeStyle.HeaderStyle.Height = 44;
                guna.ThemeStyle.RowsStyle.BackColor = Color.White;
                guna.ThemeStyle.RowsStyle.ForeColor = Ink;
                guna.ThemeStyle.RowsStyle.SelectionBackColor = PrimarySoft;
                guna.ThemeStyle.RowsStyle.SelectionForeColor = Ink;
                guna.ThemeStyle.RowsStyle.Font = grid.DefaultCellStyle.Font;
                guna.ThemeStyle.RowsStyle.Height = 46;
                guna.ThemeStyle.AlternatingRowsStyle.BackColor = Color.FromArgb(250, 252, 253);
            }

            grid.DataBindingComplete += delegate
            {
                LocalizeColumns(grid);
            };
            grid.CellFormatting += delegate (object sender, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value == null)
                    return;
                string name = grid.Columns[e.ColumnIndex].DataPropertyName;
                if (name == "IsOccupied" || name == "HasCheckedOut")
                {
                    bool yes = Convert.ToBoolean(e.Value);
                    e.Value = name == "IsOccupied" ? (yes ? "Có khách" : "Trống") : (yes ? "Đã trả phòng" : "Đang lưu trú");
                    e.CellStyle.ForeColor = yes ? Color.FromArgb(24, 24, 24) : Primary;
                    e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                    e.FormattingApplied = true;
                }

                if (name == "Price")
                {
                    long amount;
                    if (long.TryParse(Convert.ToString(e.Value), out amount))
                    {
                        e.Value = Money(amount);
                        e.FormattingApplied = true;
                    }
                }
            };
        }

        private static void LocalizeColumns(DataGridView grid)
        {
            var headers = new Dictionary<string, string>
            {
                { "Id", "Mã" },
                { "Number", "Phòng" },
                { "Type", "Loại phòng" },
                { "Bed", "Giường" },
                { "RoomNumber", "Phòng" },
                { "RoomType", "Loại phòng" },
                { "Price", "Giá / đêm" },
                { "IsOccupied", "Trạng thái" },
                { "Name", "Họ tên" },
                { "Phone", "Điện thoại" },
                { "Nationality", "Quốc tịch" },
                { "Gender", "Giới tính" },
                { "BirthDate", "Ngày sinh" },
                { "IdentityDocument", "Giấy tờ" },
                { "Address", "Địa chỉ" },
                { "CheckIn", "Nhận phòng" },
                { "CheckOut", "Trả phòng" },
                { "HasCheckedOut", "Lưu trú" },
                { "Email", "Email" }
            };

            foreach (DataGridViewColumn original in grid.Columns.Cast<DataGridViewColumn>().ToArray())
            {
                var column = original;
                // Auto-generated bool columns are checkboxes. Replace them with text
                // cells so CellFormatting can show Vietnamese status labels safely.
                if (column is DataGridViewCheckBoxColumn &&
                    (column.DataPropertyName == "IsOccupied" || column.DataPropertyName == "HasCheckedOut"))
                {
                    int index = column.Index;
                    var textColumn = new DataGridViewTextBoxColumn
                    {
                        Name = column.Name,
                        DataPropertyName = column.DataPropertyName,
                        HeaderText = column.HeaderText,
                        ReadOnly = true
                    };
                    grid.Columns.Remove(column);
                    grid.Columns.Insert(index, textColumn);
                    column = textColumn;
                }
                string title;
                if (headers.TryGetValue(column.DataPropertyName, out title)) column.HeaderText = title;
                if (column.DataPropertyName == "RoomId") column.Visible = false;
                column.MinimumWidth = column.DataPropertyName == "Name" ? 145 : 95;
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.Padding = new Point(12, 8);
            tabs.DrawItem += delegate (object sender, DrawItemEventArgs e)
            {
                bool active = e.Index == tabs.SelectedIndex;
                using (var background = new SolidBrush(Canvas))
                    e.Graphics.FillRectangle(background, e.Bounds);
                TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, active ? Primary : Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                if (active)
                    using (var line = new Pen(Primary, 3))
                        e.Graphics.DrawLine(line, e.Bounds.Left + 16, e.Bounds.Bottom - 2, e.Bounds.Right - 16, e.Bounds.Bottom - 2);
            };
        }

        public static void SetIcon(Guna2Button button, string kind, Color color)
        {
            var bitmap = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bitmap))
            using (var pen = new Pen(color, 1.7F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            })
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (kind == "home")
                {
                    g.DrawLines(pen, new[] { new Point(3, 11), new Point(12, 3), new Point(21, 11) });
                    g.DrawRectangle(pen, 6, 10, 12, 11);
                    g.DrawRectangle(pen, 10, 15, 4, 6);
                }
                else if (kind == "rooms")
                {
                    g.DrawRectangle(pen, 4, 4, 16, 17);
                    g.DrawLine(pen, 9, 4, 9, 21);
                    g.DrawEllipse(pen, 14, 12, 2, 2);
                }
                else if (kind == "people" || kind == "staff")
                {
                    g.DrawEllipse(pen, 8, 3, 8, 8);
                    g.DrawArc(pen, 4, 12, 16, 14, 180, 180);
                    if (kind == "staff")
                        g.DrawLine(pen, 12, 13, 12, 20);
                }
                else if (kind == "refresh")
                {
                    g.DrawArc(pen, 4, 4, 16, 16, 40, 280);
                    g.DrawLines(pen, new[] { new Point(20, 4), new Point(20, 10), new Point(14, 10) });
                }
                else if (kind == "link")
                {
                    g.DrawArc(pen, 3, 7, 12, 8, 90, 270);
                    g.DrawArc(pen, 9, 7, 12, 8, 270, 270);
                    g.DrawLine(pen, 8, 11, 16, 11);
                }
                else
                {
                    g.DrawRectangle(pen, 3, 4, 10, 16);
                    int direction = kind == "out" ? -1 : 1;
                    int end = direction > 0 ? 22 : 15;
                    int start = direction > 0 ? 14 : 23;
                    g.DrawLine(pen, start, 12, end, 12);
                    g.DrawLine(pen, end, 12, end - direction * 4, 8);
                    g.DrawLine(pen, end, 12, end - direction * 4, 16);
                }
            }

            button.Image = bitmap;
            button.ImageSize = new Size(21, 21);
            button.Disposed += delegate
            {
                bitmap.Dispose();
            };
        }
    }
}
