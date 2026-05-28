using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Prototype1.Forms
{
    public static class UiTheme
    {
        public const string AppTitle = "IDSMS";
        public const string AppTitleLong = "IDSMS Prototype";

        public static readonly Color Primary        = Color.FromArgb( 13,  94, 118);
        public static readonly Color PrimaryHover   = Color.FromArgb( 10,  75,  96);
        public static readonly Color PrimaryLight   = Color.FromArgb(210, 234, 242);
        public static readonly Color Accent         = Color.FromArgb( 22, 160, 133);
        public static readonly Color Danger         = Color.FromArgb(192,  57,  43);
        public static readonly Color DangerLight    = Color.FromArgb(253, 228, 225);
        public static readonly Color Warning        = Color.FromArgb(230, 126,  34);
        public static readonly Color WarningLight   = Color.FromArgb(254, 236, 211);
        public static readonly Color Success        = Color.FromArgb( 39, 174,  96);
        public static readonly Color SuccessLight   = Color.FromArgb(212, 245, 225);
        public static readonly Color Background     = Color.FromArgb(245, 247, 250);
        public static readonly Color Surface        = Color.White;
        public static readonly Color BorderLight    = Color.FromArgb(220, 225, 230);
        public static readonly Color TextPrimary    = Color.FromArgb( 30,  40,  50);
        public static readonly Color TextMuted      = Color.FromArgb(120, 130, 145);
        public static readonly Color GridSelection  = Color.FromArgb(180, 220, 235);
        public static readonly Color GridAltRow     = Color.FromArgb(240, 245, 248);
        public static readonly Color GridHeaderBg   = Color.FromArgb( 13,  94, 118);
        public static readonly Color GridHeaderFore = Color.White;

        public static readonly string FontFamily = "Segoe UI";
        public static readonly Font   FontNormal  = new Font(FontFamily,  9.5f);
        public static readonly Font   FontBold    = new Font(FontFamily,  9.5f, FontStyle.Bold);
        public static readonly Font   FontGrid    = new Font(FontFamily,  9.0f);
        public static readonly Font   FontSmall   = new Font(FontFamily,  8.5f);
        public static readonly Font   FontHeading = new Font(FontFamily, 13.0f, FontStyle.Bold);
        public static readonly Font   FontLarge   = new Font(FontFamily, 28.0f, FontStyle.Bold);

        public static Font FontBase   => FontNormal;
        public static Font FontButton => FontBold;

        public static void ApplyForm(Form f)
        {
            f.Font      = FontNormal;
            f.BackColor = Background;
            f.ForeColor = TextPrimary;
        }

        public static Panel BuildPageHeader(string section, string page, string subtitle = "")
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = subtitle.Length > 0 ? 76 : 58, BackColor = Surface, Padding = new Padding(20, 0, 20, 0) };
            var border = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = BorderLight };
            pnl.Controls.Add(border);
            pnl.Controls.Add(new Label { Text = section + "  ›  " + page, Font = new Font(FontFamily, 8.5f), ForeColor = TextMuted, AutoSize = true, Location = new Point(20, 10) });
            pnl.Controls.Add(new Label { Text = page, Font = FontHeading, ForeColor = TextPrimary, AutoSize = true, Location = new Point(20, 26) });
            if (subtitle.Length > 0)
                pnl.Controls.Add(new Label { Text = subtitle, Font = FontSmall, ForeColor = TextMuted, AutoSize = true, Location = new Point(20, 50) });
            return pnl;
        }

        // Paint header row manually to bypass DPI header-height clipping
        private static void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only handle header row (RowIndex == -1)
            if (e.RowIndex != -1) return;

            var grid = sender as DataGridView;
            if (grid == null || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count) return;

            // Fill background
            using (var bgBrush = new SolidBrush(GridHeaderBg))
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);

            // Column separator line
            using (var sepPen = new Pen(Color.FromArgb(255, 30, 110, 135)))
                e.Graphics.DrawLine(sepPen,
                    e.CellBounds.Right - 1, e.CellBounds.Top,
                    e.CellBounds.Right - 1, e.CellBounds.Bottom);

            // Get header text from the column directly (e.Value is always null for header rows)
            var col = grid.Columns[e.ColumnIndex];
            string text = col.HeaderText ?? "";

            var textRect = new Rectangle(
                e.CellBounds.X + 8,
                e.CellBounds.Y,
                e.CellBounds.Width - 12,
                e.CellBounds.Height);

            // Right-align numeric columns
            var colStyle = col.HeaderCell.Style;
            bool rightAlign = colStyle.Alignment == DataGridViewContentAlignment.MiddleRight ||
                              colStyle.Alignment == DataGridViewContentAlignment.TopRight ||
                              colStyle.Alignment == DataGridViewContentAlignment.BottomRight;

            using (var sf = new StringFormat
            {
                Alignment     = rightAlign ? StringAlignment.Far : StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            })
            using (var fbrush = new SolidBrush(GridHeaderFore))
            using (var headerFont = new Font(FontFamily, 9.5f, FontStyle.Bold))
                e.Graphics.DrawString(text, headerFont, fbrush, textRect, sf);

            e.Handled = true;
        }

        public static void ApplyGrid(DataGridView grid)
        {
            if (grid == null) return;

            grid.BorderStyle           = BorderStyle.None;
            grid.BackgroundColor       = Surface;
            grid.GridColor             = BorderLight;
            grid.RowHeadersVisible     = false;
            grid.AllowUserToResizeRows = false;
            grid.CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal;

            grid.EnableHeadersVisualStyles = false;

            grid.ColumnHeadersVisible        = true;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight         = 42;

            var hs = grid.ColumnHeadersDefaultCellStyle;
            hs.BackColor          = GridHeaderBg;
            hs.ForeColor          = GridHeaderFore;
            hs.Font               = new Font(FontFamily, 9.5f, FontStyle.Bold);
            hs.Alignment          = DataGridViewContentAlignment.MiddleLeft;
            hs.Padding            = new Padding(6, 0, 4, 0);
            hs.SelectionBackColor = GridHeaderBg;
            hs.SelectionForeColor = GridHeaderFore;

            var ds = grid.DefaultCellStyle;
            ds.Font               = FontGrid;
            ds.BackColor          = Surface;
            ds.ForeColor          = TextPrimary;
            ds.SelectionBackColor = GridSelection;
            ds.SelectionForeColor = TextPrimary;
            ds.Padding            = new Padding(8, 4, 8, 4);

            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltRow;
            grid.RowTemplate.Height = 36;

            grid.CellPainting -= Grid_CellPainting;
            grid.CellPainting += Grid_CellPainting;
        }

        public static Color StatusBadgeBack(string status)
        {
            if (status == null) return BorderLight;
            switch (status.ToLower())
            {
                case "pending":              return WarningLight;
                case "confirmed":            return PrimaryLight;
                case "shipped":              return Color.FromArgb(220, 235, 252);
                case "completed":
                case "delivered":
                case "acknowledged":         return SuccessLight;
                case "cancelled":
                case "failed":               return DangerLight;
                case "in transit":
                case "scheduled":            return Color.FromArgb(225, 215, 252);
                case "open":
                case "processing":           return WarningLight;
                case "closed":
                case "resolved":             return SuccessLight;
                default:                     return Color.FromArgb(235, 238, 242);
            }
        }

        public static Color StatusBadgeFore(string status)
        {
            if (status == null) return TextMuted;
            switch (status.ToLower())
            {
                case "pending":              return Color.FromArgb(150, 80, 0);
                case "confirmed":            return Primary;
                case "shipped":              return Color.FromArgb(30, 80, 170);
                case "completed":
                case "delivered":
                case "acknowledged":         return Color.FromArgb(20, 120, 60);
                case "cancelled":
                case "failed":               return Danger;
                case "in transit":
                case "scheduled":            return Color.FromArgb(80, 40, 160);
                case "open":
                case "processing":           return Color.FromArgb(150, 80, 0);
                case "closed":
                case "resolved":             return Color.FromArgb(20, 120, 60);
                default:                     return TextMuted;
            }
        }

        public static void PaintStatusCell(DataGridViewCellPaintingEventArgs e, string status)
        {
            e.PaintBackground(e.CellBounds, true);
            var back = StatusBadgeBack(status);
            var fore = StatusBadgeFore(status);
            var r = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 5,
                                  e.CellBounds.Width - 12, e.CellBounds.Height - 10);
            using (var brush = new SolidBrush(back))
                e.Graphics.FillRectangle(brush, r);
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var pen   = new Pen(Color.FromArgb(40, fore)))
            using (var fbrush = new SolidBrush(fore))
            {
                e.Graphics.DrawRectangle(pen, r);
                e.Graphics.DrawString(status ?? "", new Font(FontFamily, 8.5f, FontStyle.Bold), fbrush, r, sf);
            }
            e.Handled = true;
        }

        public static Label BuildHeading(string text)
            => new Label { Text = text, Font = FontHeading, ForeColor = TextPrimary, AutoSize = true };

        public static Panel BuildToolbar(int height)
            => new Panel { Dock = DockStyle.Top, Height = height, BackColor = Surface, Padding = new Padding(16, 0, 16, 0) };

        public static Panel BuildBottomBar(int height)
            => new Panel { Dock = DockStyle.Bottom, Height = height, BackColor = Surface, Padding = new Padding(16, 0, 16, 0) };

        public static Panel BuildSeparator(DockStyle dock)
            => new Panel { Dock = dock, Height = 1, BackColor = BorderLight };

        public static Panel BuildCard()
        {
            var card = new Panel { BackColor = Surface, Margin = new Padding(8) };
            card.Paint += (s, e) =>
            {
                var r = card.ClientRectangle;
                r.Width--; r.Height--;
                using (var pen = new Pen(BorderLight))
                    e.Graphics.DrawRectangle(pen, r);
            };
            return card;
        }

        public static void StyleTextBox(TextBox tb)
        {
            tb.BorderStyle = BorderStyle.FixedSingle;
            // Uniform white background for both editable and read-only TextBoxes.
            tb.BackColor   = Surface;
            tb.ForeColor   = TextPrimary;
        }

        public static void ApplyInputs(Control parent)
        {
            // Recursive so TextBoxes nested inside Panels also get styled.
            foreach (Control c in parent.Controls)
            {
                if (c is TextBox tb) StyleTextBox(tb);
                if (c.HasChildren)  ApplyInputs(c);
            }
        }

        public static void StyleComboBox(ComboBox cb)
        {
            // Use Standard (not Flat) so Windows draws the same thin border as
            // TextBox.BorderStyle = FixedSingle. FlatStyle.Flat removes the
            // border entirely on most themes which made ComboBox look weaker
            // than its TextBox neighbours.
            cb.FlatStyle = FlatStyle.Standard;
            cb.BackColor = Surface;
            cb.ForeColor = TextPrimary;
            cb.Font      = FontNormal;
        }

        public static void StylePrimary(Button b)
        {
            b.BackColor = Primary; b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.Font = FontBold; b.Cursor = Cursors.Hand;
        }
        public static void StyleSecondary(Button b)
        {
            b.BackColor = Surface; b.ForeColor = TextPrimary;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = BorderLight; b.FlatAppearance.BorderSize = 1;
            b.Font = FontNormal; b.Cursor = Cursors.Hand;
        }
        public static void StyleDanger(Button b)
        {
            b.BackColor = Danger; b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.Font = FontBold; b.Cursor = Cursors.Hand;
        }
        public static void StyleDangerOutlined(Button b)
        {
            b.BackColor = Surface; b.ForeColor = Danger;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Danger; b.FlatAppearance.BorderSize = 1;
            b.Font = FontNormal; b.Cursor = Cursors.Hand;
        }
        public static void StyleAccent(Button b)
        {
            b.BackColor = Accent; b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.Font = FontBold; b.Cursor = Cursors.Hand;
        }
        public static void StyleSuccess(Button b)
        {
            b.BackColor = Success; b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.Font = FontBold; b.Cursor = Cursors.Hand;
        }

        public static void LayoutLeft(Panel bar, int gap, params Control[] controls)
        {
            int x  = bar.Padding.Left;
            int cy = (bar.Height - controls[0].Height) / 2;
            foreach (var c in controls) { c.Location = new Point(x, cy); x += c.Width + gap; }
        }

        public static void LayoutRight(Panel bar, int gap, params Control[] controls)
        {
            int x  = bar.Width - bar.Padding.Right;
            int cy = (bar.Height - controls[0].Height) / 2;
            for (int i = controls.Length - 1; i >= 0; i--)
            {
                x -= controls[i].Width;
                controls[i].Location = new Point(x, cy);
                controls[i].Anchor   = AnchorStyles.Right | AnchorStyles.Top;
                x -= gap;
            }
        }

        public static void ShowInfo(string message)
            => MessageBox.Show(message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static void ShowInfo(IWin32Window owner, string message)
            => MessageBox.Show(owner, message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static void ShowWarning(string message)
            => MessageBox.Show(message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static void ShowWarning(IWin32Window owner, string message)
            => MessageBox.Show(owner, message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static void ShowError(string message)
            => MessageBox.Show(message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        public static void ShowError(IWin32Window owner, string message)
            => MessageBox.Show(owner, message, AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        public static bool ShowConfirm(string message)
            => MessageBox.Show(message, AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        public static bool ShowConfirm(IWin32Window owner, string message)
            => MessageBox.Show(owner, message, AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        public static void EnableStatusColumn(DataGridView grid, string statusColumnName = "Status")
        {
            if (grid == null) return;
            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var col = grid.Columns[e.ColumnIndex];
                if (col == null) return;
                if (!string.Equals(col.Name, statusColumnName, StringComparison.OrdinalIgnoreCase)) return;
                string status = e.Value == null ? "" : e.Value.ToString();
                PaintStatusCell(e, status);
            };
        }

        private static readonly string[] NumericColumnNames =
        {
            "Qty", "Quantity", "Stock", "StockQty", "Reorder", "ReorderLvl",
            "Price", "UnitPrice", "Total", "LineTotal", "Amount", "Lines"
        };

        public static void AlignNumericColumns(DataGridView grid, params string[] extraColumnNames)
        {
            if (grid == null) return;
            foreach (DataGridViewColumn col in grid.Columns)
            {
                bool isNumeric = false;
                foreach (var n in NumericColumnNames)
                    if (string.Equals(col.Name, n, StringComparison.OrdinalIgnoreCase)) { isNumeric = true; break; }
                if (!isNumeric && extraColumnNames != null)
                    foreach (var n in extraColumnNames)
                        if (string.Equals(col.Name, n, StringComparison.OrdinalIgnoreCase)) { isNumeric = true; break; }
                if (isNumeric)
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }
    }

    public class ListItem
    {
        public string Value   { get; }
        public string Display { get; }
        public ListItem(string value, string display) { Value = value; Display = display; }
        public override string ToString() => Display;
    }
}
