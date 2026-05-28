using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class AfterServiceForm : Form
    {
        private DataGridView grid;
        private ComboBox cmbFilter;
        private Button btnNew, btnEdit, btnClose;

        public AfterServiceForm()
        {
            Text = "After-Service Management";
            ClientSize = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            var top = UiTheme.BuildToolbar(72);
            var lblTitle = UiTheme.BuildHeading("After-Service Requests");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Return / Replacement / Refund processing.", Location = new Point(0, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });

            var lblFilter = new Label { Text = "Filter:", Location = new Point(560, 42), AutoSize = true, ForeColor = UiTheme.TextMuted };
            top.Controls.Add(lblFilter);
            cmbFilter = new ComboBox { Location = new Point(610, 39), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilter.Items.AddRange(new object[] { "(All)", "Return", "Replacement", "Refund", "Open", "Processing", "Closed" });
            cmbFilter.SelectedIndex = 0;
            UiTheme.StyleComboBox(cmbFilter);
            cmbFilter.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbFilter);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 12), BackColor = UiTheme.Background };
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiTheme.ApplyGrid(grid);
            UiTheme.EnableStatusColumn(grid);
            grid.DoubleClick += (s, e) => DoEdit();
            body.Controls.Add(grid);
            Controls.Add(body);

            var bottom = UiTheme.BuildBottomBar(64);
            btnNew = new Button { Text = "New Request", Width = 130, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            btnEdit = new Button { Text = "Edit / Resolve", Width = 130, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnClose);
            CancelButton = btnClose;

            // Add Top/Bottom AFTER Fill so they correctly clip the Fill area
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
        }

        private void LoadGrid()
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("Id", "Request No.");
            grid.Columns.Add("Date", "Date");
            grid.Columns.Add("Order", "Order No.");
            grid.Columns.Add("Customer", "Customer");
            grid.Columns.Add("Type", "Type");
            grid.Columns.Add("Item", "Item");
            grid.Columns.Add("Qty", "Qty");
            grid.Columns.Add("Status", "Status");
            grid.Columns.Add("By", "Handled By");
            UiTheme.AlignNumericColumns(grid);

            string filter = cmbFilter.SelectedItem as string;
            foreach (var r in DataStore.ServiceRequests.OrderByDescending(x => x.RequestDate))
            {
                if (filter != null && filter != "(All)" && r.RequestType != filter && r.Status != filter) continue;
                var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == r.CustomerId);
                var it = DataStore.Items.FirstOrDefault(x => x.ItemId == r.ItemId);
                grid.Rows.Add(r.RequestId, r.RequestDate.ToString("yyyy-MM-dd"), r.OrderId,
                    c != null ? c.CompanyName : r.CustomerId, r.RequestType,
                    it != null ? it.ItemName : r.ItemId, r.Quantity, r.Status, r.HandledBy);
            }
        }

        private AfterServiceRequest Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.ServiceRequests.FirstOrDefault(r => r.RequestId == id);
        }

        private void DoNew()
        {
            using (var f = new AfterServiceEditForm(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoEdit()
        {
            var r = Selected();
            if (r == null) { UiTheme.ShowWarning(this, "Please select a request."); return; }
            using (var f = new AfterServiceEditForm(r))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }
    }

    public class AfterServiceEditForm : Form
    {
        private readonly AfterServiceRequest original;
        private TextBox txtId, txtReason, txtResolution;
        private DateTimePicker dtpDate;
        private ComboBox cmbType, cmbStatus, cmbCustomer, cmbOrder, cmbItem;
        private NumericUpDown numQty;

        public AfterServiceEditForm(AfterServiceRequest existing)
        {
            original = existing;
            Text = existing == null ? "New After-Service Request" : "Edit Request - " + existing.RequestId;
            ClientSize = new Size(560, 590);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            UiTheme.ApplyInputs(this);
            Load_();
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label { Text = original == null ? "New After-Service Request" : "Edit Request", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Request No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            Controls.Add(txtId);

            y += 35;
            Controls.Add(new Label { Text = "Request Date:", Location = new Point(20, y + 3), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(140, y), Width = 200, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpDate);

            y += 35;
            Controls.Add(new Label { Text = "Request Type:", Location = new Point(20, y + 3), AutoSize = true });
            cmbType = new ComboBox { Location = new Point(140, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Return", "Replacement", "Refund", "Repair" });
            Controls.Add(cmbType);

            y += 35;
            Controls.Add(new Label { Text = "Customer:", Location = new Point(20, y + 3), AutoSize = true });
            cmbCustomer = new ComboBox { Location = new Point(140, y), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in DataStore.Customers) cmbCustomer.Items.Add(new ListItem(c.CustomerId, c.CustomerId + " - " + c.CompanyName));
            cmbCustomer.DisplayMember = "Display";
            Controls.Add(cmbCustomer);

            y += 35;
            Controls.Add(new Label { Text = "Order No.:", Location = new Point(20, y + 3), AutoSize = true });
            cmbOrder = new ComboBox { Location = new Point(140, y), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var o in DataStore.SalesOrders) cmbOrder.Items.Add(new ListItem(o.OrderId, o.OrderId));
            cmbOrder.DisplayMember = "Display";
            Controls.Add(cmbOrder);

            y += 35;
            Controls.Add(new Label { Text = "Item:", Location = new Point(20, y + 3), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(140, y), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var it in DataStore.Items) cmbItem.Items.Add(new ListItem(it.ItemId, it.ItemId + " - " + it.ItemName));
            cmbItem.DisplayMember = "Display";
            Controls.Add(cmbItem);

            y += 35;
            Controls.Add(new Label { Text = "Quantity:", Location = new Point(20, y + 3), AutoSize = true });
            numQty = new NumericUpDown { Location = new Point(140, y), Width = 100, Minimum = 1, Maximum = 999, Value = 1 };
            Controls.Add(numQty);

            y += 35;
            Controls.Add(new Label { Text = "Reason:", Location = new Point(20, y + 3), AutoSize = true });
            txtReason = new TextBox { Location = new Point(140, y), Width = 350, Multiline = true, Height = 50 };
            Controls.Add(txtReason);

            y += 60;
            Controls.Add(new Label { Text = "Status:", Location = new Point(20, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(140, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Open", "Processing", "Closed" });
            Controls.Add(cmbStatus);

            y += 35;
            Controls.Add(new Label { Text = "Resolution:", Location = new Point(20, y + 3), AutoSize = true });
            txtResolution = new TextBox { Location = new Point(140, y), Width = 350, Multiline = true, Height = 50 };
            Controls.Add(txtResolution);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 70), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;

            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 70), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("AS", DataStore.ServiceRequests.Select(s => s.RequestId));
                dtpDate.Value = DateTime.Today;
                cmbType.SelectedIndex = 0;
                cmbStatus.SelectedIndex = 0;
            }
            else
            {
                txtId.Text = original.RequestId;
                dtpDate.Value = original.RequestDate;
                cmbType.SelectedItem = original.RequestType;
                cmbStatus.SelectedItem = original.Status;
                SelectByValue(cmbCustomer, original.CustomerId);
                SelectByValue(cmbOrder, original.OrderId);
                SelectByValue(cmbItem, original.ItemId);
                numQty.Value = Math.Max(1, original.Quantity);
                txtReason.Text = original.Reason;
                txtResolution.Text = original.Resolution;
            }
        }

        private void SelectByValue(ComboBox cb, string value)
        {
            if (value == null) return;
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (((ListItem)cb.Items[i]).Value == value) { cb.SelectedIndex = i; return; }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbCustomer.SelectedItem == null) { UiTheme.ShowWarning(this, "Select a customer."); return; }
            if (cmbItem.SelectedItem == null) { UiTheme.ShowWarning(this, "Select an item."); return; }
            if (string.IsNullOrWhiteSpace(txtReason.Text)) { UiTheme.ShowWarning(this, "Reason is required."); return; }

            string status = cmbStatus.SelectedItem as string ?? "Open";
            string type = cmbType.SelectedItem as string ?? "Return";
            string customerId = ((ListItem)cmbCustomer.SelectedItem).Value;
            string orderId = cmbOrder.SelectedItem != null ? ((ListItem)cmbOrder.SelectedItem).Value : "";
            string itemId = ((ListItem)cmbItem.SelectedItem).Value;
            int qty = (int)numQty.Value;

            AfterServiceRequest target;
            bool isNew = original == null;
            if (isNew)
            {
                target = new AfterServiceRequest { RequestId = txtId.Text };
                DataStore.ServiceRequests.Add(target);
            }
            else
            {
                target = original;
            }

            target.RequestDate = dtpDate.Value.Date;
            target.RequestType = type;
            target.CustomerId = customerId;
            target.OrderId = orderId;
            target.ItemId = itemId;
            target.Quantity = qty;
            target.Reason = txtReason.Text.Trim();
            target.Status = status;
            target.Resolution = txtResolution.Text.Trim();
            target.HandledBy = SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "";

            if (status == "Closed" && (type == "Return" || type == "Replacement"))
            {
                var item = DataStore.Items.FirstOrDefault(x => x.ItemId == itemId);
                if (item != null) item.StockQty += qty;
            }

            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                isNew ? "New Service Request" : "Update Service Request", target.RequestId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
