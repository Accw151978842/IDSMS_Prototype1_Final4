using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class DeliveryNoteForm : Form
    {
        private DataGridView grid;
        private Button btnGenerate, btnEdit, btnReplySlip, btnPreview, btnClose;
        private TextBox txtSearch;
        private ComboBox cmbStatusFilter;

        public DeliveryNoteForm()
        {
            Text = "Delivery Notes & Reply Slip";
            ClientSize = new Size(960, 580);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // 1. TOP
            var top = UiTheme.BuildToolbar(116);
            var lblTitle = UiTheme.BuildHeading("Delivery Schedule and Reply Slip");
            lblTitle.Location = new Point(16, 10);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Track delivery progress, capture reply slip and customer acknowledgement.", Location = new Point(16, 44), AutoSize = true, ForeColor = UiTheme.TextMuted });

            top.Controls.Add(new Label { Text = "Search:", Location = new Point(16, 84), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(72, 81), Width = 240, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Status:", Location = new Point(330, 84), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbStatusFilter = new ComboBox { Location = new Point(382, 81), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "Scheduled", "In Transit", "Delivered", "Failed" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbStatusFilter);

            // 2. BOTTOM
            var bottom = UiTheme.BuildBottomBar(64);
            btnGenerate = new Button { Text = "Generate from Order", Width = 160, Height = 34 };
            btnGenerate.Click += (s, e) => Generate();
            btnEdit = new Button { Text = "Edit", Width = 90, Height = 34 };
            btnEdit.Click += (s, e) => Edit();
            btnReplySlip = new Button { Text = "Record Reply Slip", Width = 150, Height = 34 };
            btnReplySlip.Click += (s, e) => ReplySlip();
            btnPreview = new Button { Text = "Preview Delivery Note", Width = 170, Height = 34 };
            btnPreview.Click += (s, e) => Preview();
            btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnGenerate, btnEdit, btnReplySlip, btnPreview, btnClose });
            UiTheme.StylePrimary(btnGenerate);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleAccent(btnReplySlip);
            UiTheme.StyleSecondary(btnPreview);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 14, btnGenerate, btnEdit, btnReplySlip, btnPreview, btnClose);
            CancelButton = btnClose;

            // 3. FILL (must be added FIRST so Top/Bottom dock above/below it correctly)
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 8), BackColor = UiTheme.Background };
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
            body.Controls.Add(grid);
            Controls.Add(body);

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
            grid.Columns.Add("Id", "Delivery No.");
            grid.Columns.Add("OrderId", "Order No.");
            grid.Columns.Add("Customer", "Customer");
            grid.Columns.Add("Date", "Delivery Date");
            grid.Columns.Add("Driver", "Driver");
            grid.Columns.Add("Vehicle", "Vehicle");
            grid.Columns.Add("Status", "Status");
            grid.Columns.Add("Reply", "Reply Slip");
            UiTheme.AlignNumericColumns(grid);

            string keyword = txtSearch != null ? txtSearch.Text.Trim().ToLowerInvariant() : "";
            string statusFilter = cmbStatusFilter != null ? cmbStatusFilter.SelectedItem as string : "All Status";

            foreach (var d in DataStore.Deliveries)
            {
                var order = DataStore.SalesOrders.FirstOrDefault(o => o.OrderId == d.OrderId);
                string cust = "(unknown)";
                if (order != null)
                {
                    var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == order.CustomerId);
                    if (c != null) cust = c.CompanyName;
                }

                // Status filter
                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All Status"
                    && !string.Equals(d.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Keyword search across delivery no., order no., customer, driver, vehicle
                if (keyword.Length > 0)
                {
                    string haystack = ((d.DeliveryId ?? "") + " " + (d.OrderId ?? "") + " " + cust + " "
                        + (d.DriverName ?? "") + " " + (d.VehicleNo ?? "")).ToLowerInvariant();
                    if (!haystack.Contains(keyword)) continue;
                }

                grid.Rows.Add(d.DeliveryId, d.OrderId, cust, d.DeliveryDate.ToString("yyyy-MM-dd"),
                    d.DriverName, d.VehicleNo, d.Status, d.ReplySlipStatus);
            }
        }

        private DeliveryNote Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Deliveries.FirstOrDefault(d => d.DeliveryId == id);
        }

        private void Generate()
        {
            using (var f = new DeliveryNoteEditForm(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void Edit()
        {
            var d = Selected();
            if (d == null) { UiTheme.ShowWarning(this, "Please select a delivery."); return; }
            using (var f = new DeliveryNoteEditForm(d))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void ReplySlip()
        {
            var d = Selected();
            if (d == null) { UiTheme.ShowWarning(this, "Please select a delivery."); return; }
            using (var f = new ReplySlipForm(d))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void Preview()
        {
            var d = Selected();
            if (d == null) { UiTheme.ShowWarning(this, "Please select a delivery."); return; }
            var order = DataStore.SalesOrders.FirstOrDefault(o => o.OrderId == d.OrderId);
            string text = "PREMIUM LIVING FURNITURE CO. LTD." + Environment.NewLine +
                          "DELIVERY NOTE" + Environment.NewLine +
                          "-----------------------------------" + Environment.NewLine +
                          "Delivery No: " + d.DeliveryId + Environment.NewLine +
                          "Order No   : " + d.OrderId + Environment.NewLine +
                          "Date       : " + d.DeliveryDate.ToString("yyyy-MM-dd") + Environment.NewLine +
                          "Driver     : " + d.DriverName + " / " + d.VehicleNo + Environment.NewLine;
            if (order != null)
            {
                var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == order.CustomerId);
                if (c != null)
                {
                    text += "Deliver To : " + c.CompanyName + Environment.NewLine +
                            "Address    : " + c.Address + Environment.NewLine +
                            "Contact    : " + c.ContactPerson + " (" + c.Phone + ")" + Environment.NewLine;
                }
                text += "-----------------------------------" + Environment.NewLine +
                        "Items:" + Environment.NewLine;
                foreach (var ln in order.Lines)
                {
                    text += "  " + ln.ItemId + " " + ln.ItemName + "  x" + ln.Quantity + Environment.NewLine;
                }
            }
            text += "-----------------------------------" + Environment.NewLine +
                    "Remarks: " + d.Remarks + Environment.NewLine + Environment.NewLine +
                    "Reply Slip Status: " + d.ReplySlipStatus + Environment.NewLine +
                    "Customer Signature: ______________________  Date: __________";

            using (var prev = new Form())
            {
                prev.Text = "Delivery Note Preview - " + d.DeliveryId;
                prev.ClientSize = new Size(600, 500);
                prev.StartPosition = FormStartPosition.CenterParent;
                var tb = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10F),
                    ScrollBars = ScrollBars.Vertical,
                    Text = text
                };
                prev.Controls.Add(tb);
                prev.ShowDialog(this);
            }
        }
    }

    public class DeliveryNoteEditForm : Form
    {
        private readonly DeliveryNote original;
        private TextBox txtId, txtDriver, txtVehicle, txtRemarks;
        private ComboBox cmbOrder, cmbStatus;
        private DateTimePicker dtpDate;

        public DeliveryNoteEditForm(DeliveryNote existing)
        {
            original = existing;
            Text = existing == null ? "Generate Delivery Note" : "Edit Delivery Note";
            ClientSize = new Size(530, 470);
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
            header.Controls.Add(new Label { Text = original == null ? "Generate Delivery Note" : "Edit Delivery Note", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Delivery No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            Controls.Add(txtId);

            y += 35;
            Controls.Add(new Label { Text = "Sales Order:", Location = new Point(20, y + 3), AutoSize = true });
            cmbOrder = new ComboBox { Location = new Point(140, y), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var o in DataStore.SalesOrders)
            {
                if (o.Status == "Cancelled" || o.Status == "Completed") continue;
                var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == o.CustomerId);
                string cname = c != null ? c.CompanyName : o.CustomerId;
                cmbOrder.Items.Add(new ListItem(o.OrderId, o.OrderId + " - " + cname));
            }
            cmbOrder.DisplayMember = "Display";
            Controls.Add(cmbOrder);

            y += 35;
            Controls.Add(new Label { Text = "Delivery Date:", Location = new Point(20, y + 3), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(140, y), Width = 200, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpDate);

            y += 35;
            Controls.Add(new Label { Text = "Driver Name:", Location = new Point(20, y + 3), AutoSize = true });
            txtDriver = new TextBox { Location = new Point(140, y), Width = 300 };
            Controls.Add(txtDriver);

            y += 35;
            Controls.Add(new Label { Text = "Vehicle No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtVehicle = new TextBox { Location = new Point(140, y), Width = 200 };
            txtVehicle.KeyPress += Validation.OnlyAlphanumeric;
            Controls.Add(txtVehicle);

            y += 35;
            Controls.Add(new Label { Text = "Status:", Location = new Point(20, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(140, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Scheduled", "In Transit", "Delivered", "Failed" });
            Controls.Add(cmbStatus);

            y += 35;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(140, y), Width = 300 };
            Controls.Add(txtRemarks);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 50), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;

            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 50), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("DN", DataStore.Deliveries.Select(d => d.DeliveryId));
                dtpDate.Value = DateTime.Today.AddDays(1);
                cmbStatus.SelectedItem = "Scheduled";
            }
            else
            {
                txtId.Text = original.DeliveryId;
                for (int i = 0; i < cmbOrder.Items.Count; i++)
                {
                    if (((ListItem)cmbOrder.Items[i]).Value == original.OrderId)
                    {
                        cmbOrder.SelectedIndex = i;
                        break;
                    }
                }
                if (cmbOrder.SelectedIndex < 0 && cmbOrder.Items.Count > 0)
                {
                    cmbOrder.Items.Insert(0, new ListItem(original.OrderId, original.OrderId));
                    cmbOrder.SelectedIndex = 0;
                }
                dtpDate.Value = original.DeliveryDate;
                txtDriver.Text = original.DriverName;
                txtVehicle.Text = original.VehicleNo;
                cmbStatus.SelectedItem = original.Status;
                txtRemarks.Text = original.Remarks;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var v = new Validation(this);
            v.Selected(cmbOrder, "Please select a sales order.")
             .Required(txtDriver, "Driver name is required.")
             .Regex(txtDriver, Validation.RxLettersOnly, "Driver name: 2-50 letters/Chinese only.")
             .Required(txtVehicle, "Vehicle number is required.")
             .Regex(txtVehicle, Validation.RxHkVehicle, "Vehicle format: e.g. LV1234 or AB 1234.")
             .Custom(dtpDate, () => dtpDate.Value.Date >= DateTime.Today.AddDays(-30), "Delivery date cannot be in distant past.");
            if (!v.ValidateAll()) return;

            if (original == null)
            {
                var d = new DeliveryNote
                {
                    DeliveryId = txtId.Text,
                    OrderId = ((ListItem)cmbOrder.SelectedItem).Value,
                    DeliveryDate = dtpDate.Value.Date,
                    DriverName = txtDriver.Text.Trim(),
                    VehicleNo = txtVehicle.Text.Trim(),
                    Status = cmbStatus.SelectedItem as string ?? "Scheduled",
                    ReplySlipStatus = "Pending",
                    Remarks = txtRemarks.Text.Trim()
                };
                DataStore.Deliveries.Add(d);
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Generate Delivery Note", d.DeliveryId);
            }
            else
            {
                original.OrderId = ((ListItem)cmbOrder.SelectedItem).Value;
                original.DeliveryDate = dtpDate.Value.Date;
                original.DriverName = txtDriver.Text.Trim();
                original.VehicleNo = txtVehicle.Text.Trim();
                original.Status = cmbStatus.SelectedItem as string ?? "Scheduled";
                original.Remarks = txtRemarks.Text.Trim();
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit Delivery Note", original.DeliveryId);
            }
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public class ReplySlipForm : Form
    {
        private readonly DeliveryNote dn;
        private ComboBox cmbStatus;
        private TextBox txtSignature, txtRemarks;
        private DateTimePicker dtpDate;

        public ReplySlipForm(DeliveryNote target)
        {
            dn = target;
            Text = "Reply Slip - " + dn.DeliveryId;
            ClientSize = new Size(490, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            UiTheme.ApplyInputs(this);
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label { Text = "Reply Slip", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Reply Slip Status:", Location = new Point(20, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(150, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Pending", "Acknowledged", "Discrepancy Reported" });
            cmbStatus.SelectedItem = dn.ReplySlipStatus ?? "Pending";
            Controls.Add(cmbStatus);

            y += 40;
            Controls.Add(new Label { Text = "Received Date:", Location = new Point(20, y + 3), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(150, y), Width = 200, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpDate);

            y += 40;
            Controls.Add(new Label { Text = "Customer Signature:", Location = new Point(20, y + 3), AutoSize = true });
            txtSignature = new TextBox { Location = new Point(150, y), Width = 270, Text = dn.CustomerSignature };
            Controls.Add(txtSignature);

            y += 40;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(150, y), Width = 270, Text = dn.Remarks };
            Controls.Add(txtRemarks);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 50), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;

            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 50), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSignature.Text) && cmbStatus.SelectedItem as string == "Acknowledged")
            {
                UiTheme.ShowWarning(this, "Customer signature is required when acknowledging delivery.");
                return;
            }
            dn.ReplySlipStatus = cmbStatus.SelectedItem as string;
            dn.CustomerSignature = txtSignature.Text.Trim();
            dn.Remarks = txtRemarks.Text.Trim();
            if (dn.ReplySlipStatus == "Acknowledged")
            {
                dn.Status = "Delivered";
                var order = DataStore.SalesOrders.FirstOrDefault(o => o.OrderId == dn.OrderId);
                if (order != null && order.Status == "Confirmed") order.Status = "Shipped";
            }
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Record Reply Slip", dn.DeliveryId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
