using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class SalesOrderListForm : Form
    {
        private DataGridView grid;
        private Button btnNew, btnEdit, btnView, btnDelete, btnClose, btnRefresh;
        private TextBox txtSearch;
        private ComboBox cmbStatus;

        public SalesOrderListForm()
        {
            Text = "Sales Orders";
            ClientSize = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // ── FILL grid (必須最先加入到 this.Controls，DockStyle.Fill 才會被 Top/Bottom 擠開) ──
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 8), BackColor = UiTheme.Background };
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
            // ApplyGrid registers the CellPainting handler and sets header height/style
            UiTheme.ApplyGrid(grid);
            UiTheme.EnableStatusColumn(grid);
            grid.DoubleClick += (s, e) => DoView();
            body.Controls.Add(grid);
            Controls.Add(body);

            // ── TOP toolbar ──────────────────────────────────────────────────
            var top = UiTheme.BuildToolbar(72);

            var lblTitle = UiTheme.BuildHeading("Sales Orders");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);

            top.Controls.Add(new Label { Text = "Search:", Location = new Point(0, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(56, 39), Width = 230, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Status:", Location = new Point(305, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbStatus = new ComboBox { Location = new Point(355, 39), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "(All)", "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" });
            cmbStatus.SelectedIndex = 0;
            UiTheme.StyleComboBox(cmbStatus);
            cmbStatus.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbStatus);

            btnRefresh = new Button { Text = "Refresh", Location = new Point(525, 37), Width = 90, Height = 28 };
            UiTheme.StyleSecondary(btnRefresh);
            btnRefresh.Click += (s, e) => LoadGrid();
            top.Controls.Add(btnRefresh);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);

            // ── BOTTOM bar ───────────────────────────────────────────────────
            var bottom = UiTheme.BuildBottomBar(64);
            btnNew    = new Button { Text = "New Order",   Width = 110, Height = 34 };
            btnEdit   = new Button { Text = "Edit",         Width =  90, Height = 34 };
            btnView   = new Button { Text = "View",         Width =  90, Height = 34 };
            btnDelete = new Button { Text = "Cancel Order", Width = 120, Height = 34 };
            btnClose  = new Button { Text = "Close",        Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            btnNew.Click    += (s, e) => DoNew();
            btnEdit.Click   += (s, e) => DoEdit();
            btnView.Click   += (s, e) => DoView();
            btnDelete.Click += (s, e) => DoCancel();
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnView, btnDelete, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnView);
            UiTheme.StyleDangerOutlined(btnDelete);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnView, btnDelete, btnClose);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
            CancelButton = btnClose;
        }

        private void LoadGrid()
        {
            // Columns.Clear() removes all columns AND their associated event handlers.
            // Re-call ApplyGrid so the CellPainting handler (custom header painter) is restored.
            grid.Columns.Clear();
            UiTheme.ApplyGrid(grid);          // <-- CRITICAL: restores CellPainting after Columns.Clear
            UiTheme.EnableStatusColumn(grid); // re-wire status badge painter

            grid.Columns.Add("OrderId",   "Order No.");
            grid.Columns.Add("OrderDate", "Order Date");
            grid.Columns.Add("Customer",  "Customer");
            grid.Columns.Add("Required",  "Required Date");
            grid.Columns.Add("Status",    "Status");
            grid.Columns.Add("Lines",     "Lines");
            grid.Columns.Add("Total",     "Total (HKD)");

            UiTheme.AlignNumericColumns(grid);

            string filter = txtSearch.Text.Trim().ToLower();
            string status = cmbStatus.SelectedItem as string;

            foreach (var o in DataStore.SalesOrders)
            {
                var cust = DataStore.Customers.FirstOrDefault(c => c.CustomerId == o.CustomerId);
                string custName = cust != null ? cust.CompanyName : o.CustomerId;
                if (status != null && status != "(All)" && o.Status != status) continue;
                if (filter.Length > 0)
                {
                    string hay = (o.OrderId + " " + custName + " " + o.Status).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(o.OrderId, o.OrderDate.ToString("yyyy-MM-dd"), custName,
                    o.RequiredDate.ToString("yyyy-MM-dd"), o.Status, o.Lines.Count, o.TotalAmount.ToString("N2"));
            }
        }

        private SalesOrder Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.SalesOrders.FirstOrDefault(o => o.OrderId == id);
        }

        private void DoNew()
        {
            using (var f = new SalesOrderEditForm(null))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoEdit()
        {
            var o = Selected();
            if (o == null) { UiTheme.ShowWarning(this, "Please select an order first."); return; }
            if (o.Status == "Completed" || o.Status == "Cancelled")
            { UiTheme.ShowWarning(this, "Completed or cancelled orders cannot be edited."); return; }
            using (var f = new SalesOrderEditForm(o))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoView()
        {
            var o = Selected();
            if (o == null) { UiTheme.ShowWarning(this, "Please select an order first."); return; }
            using (var f = new SalesOrderEditForm(o) { ReadOnlyMode = true })
                f.ShowDialog(this);
        }

        private void DoCancel()
        {
            var o = Selected();
            if (o == null) { UiTheme.ShowWarning(this, "Please select an order first."); return; }
            if (!UiTheme.ShowConfirm(this, "Cancel order " + o.OrderId + "?")) return;
            o.Status = "Cancelled";
            SecurityService.Audit(
                SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                "Cancel Order", o.OrderId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }
}
