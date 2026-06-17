using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class AuditLogForm : Form
    {
        private DataGridView    grid;
        private TextBox         txtSearch;
        private ComboBox        cmbAction;
        private DateTimePicker  dtpFrom, dtpTo;
        private CheckBox        chkUseDate;
        private Label           lblCount;

        public AuditLogForm()
        {
            Text          = "Security - Audit Log";
            ClientSize    = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // ---- TOP TOOLBAR ----
            // Height raised to 130 to fit heading + 2 filter rows with breathing room.
            // All controls offset by 16px from the left for a proper margin.
            var top = UiTheme.BuildToolbar(130);

            var lblTitle = UiTheme.BuildHeading("Audit Log");
            lblTitle.Location = new Point(16, 10);
            top.Controls.Add(lblTitle);

            // Row 1: keyword + action filter
            top.Controls.Add(new Label { Text = "Keyword:", Location = new Point(16, 58), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(80, 55), Width = 220 };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Action:", Location = new Point(316, 58), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbAction = new ComboBox { Location = new Point(368, 55), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAction.Items.AddRange(new object[] { "(All Actions)", "Login", "Logout", "Create", "Edit", "Cancel Order", "Delete", "Change Password" });
            cmbAction.SelectedIndex = 0;
            UiTheme.StyleComboBox(cmbAction);
            cmbAction.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbAction);

            // Row 2: date range + record count
            chkUseDate = new CheckBox { Text = "Date range:", Location = new Point(16, 94), AutoSize = true, ForeColor = UiTheme.TextMuted };
            chkUseDate.CheckedChanged += (s, e) => { dtpFrom.Enabled = dtpTo.Enabled = chkUseDate.Checked; LoadGrid(); };
            top.Controls.Add(chkUseDate);

            dtpFrom = new DateTimePicker { Location = new Point(108, 91), Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Enabled = false, Value = DateTime.Today.AddDays(-30) };
            dtpFrom.ValueChanged += (s, e) => LoadGrid();
            top.Controls.Add(dtpFrom);

            top.Controls.Add(new Label { Text = "to", Location = new Point(246, 94), AutoSize = true, ForeColor = UiTheme.TextMuted });

            dtpTo = new DateTimePicker { Location = new Point(266, 91), Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Enabled = false, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => LoadGrid();
            top.Controls.Add(dtpTo);

            lblCount = new Label { Location = new Point(416, 94), AutoSize = true, ForeColor = UiTheme.TextMuted };
            top.Controls.Add(lblCount);

            // ---- GRID (build but DO NOT add to Controls yet) ----
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8), BackColor = UiTheme.Background };
            grid = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiTheme.ApplyGrid(grid);
            body.Controls.Add(grid);

            // ---- BOTTOM BAR ----
            var bottom = UiTheme.BuildBottomBar(64);

            var btnExport  = new Button { Text = "Export CSV", Width = 110, Height = 34 };
            UiTheme.StyleSecondary(btnExport);
            btnExport.Click += DoExport;

            var btnRefresh = new Button { Text = "Refresh", Width = 90, Height = 34 };
            UiTheme.StyleSecondary(btnRefresh);
            btnRefresh.Click += (s, e) => { DataStore.LoadAll(); LoadGrid(); };

            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnClose);

            bottom.Controls.AddRange(new Control[] { btnExport, btnRefresh, btnClose });
            UiTheme.LayoutLeft(bottom, 8, btnExport, btnRefresh, btnClose);

            // ---- Add to form in correct Z-order: Fill FIRST, then Top, then Bottom ----
            Controls.Add(body);                                       // Fill
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);                                        // Top
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);                                     // Bottom
            CancelButton = btnClose;
        }

        private void LoadGrid()
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("Time",   "Timestamp");
            grid.Columns.Add("User",   "User");
            grid.Columns.Add("Action", "Action");
            grid.Columns.Add("Detail", "Detail");
            grid.Columns["Time"].Width   = 160;
            grid.Columns["User"].Width   = 120;
            grid.Columns["Action"].Width = 160;

            string keyword = txtSearch.Text.Trim().ToLower();
            string action  = cmbAction.SelectedItem as string;

            var query = DataStore.AuditLogs.OrderByDescending(x => x.Timestamp).AsEnumerable();

            // Date range filter
            if (chkUseDate.Checked)
            {
                var from = dtpFrom.Value.Date;
                var to   = dtpTo.Value.Date.AddDays(1); // inclusive end date
                query = query.Where(l => l.Timestamp >= from && l.Timestamp < to);
            }

            // Action filter
            if (!string.IsNullOrEmpty(action) && action != "(All Actions)")
                query = query.Where(l => (l.Action ?? "").IndexOf(action, StringComparison.OrdinalIgnoreCase) >= 0);

            // Keyword filter
            if (keyword.Length > 0)
                query = query.Where(l =>
                    ((l.Username ?? "") + " " + (l.Action ?? "") + " " + (l.Detail ?? ""))
                    .ToLower().Contains(keyword));

            int count = 0;
            foreach (var l in query)
            {
                grid.Rows.Add(
                    l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    l.Username,
                    l.Action,
                    l.Detail);
                count++;
            }
            lblCount.Text = $"Showing {count} record(s)";
        }

        private void DoExport(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
            {
                Title    = "Export Audit Log",
                Filter   = "CSV files (*.csv)|*.csv",
                FileName = "AuditLog_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    using (var sw = new StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine("Timestamp,User,Action,Detail");
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            sw.WriteLine(
                                $"\"{row.Cells[0].Value}\"," +
                                $"\"{row.Cells[1].Value}\"," +
                                $"\"{row.Cells[2].Value}\"," +
                                $"\"{row.Cells[3].Value}\"");
                        }
                    }
                    UiTheme.ShowInfo(this, "Exported successfully to:\n" + dlg.FileName);
                }
                catch (Exception ex)
                {
                    UiTheme.ShowError(this, "Export failed: " + ex.Message);
                }
            }
        }
    }
}
