using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class GoodsReceivedForm : Form
    {
        private DataGridView grid;
        private Button btnNew, btnEdit, btnClose;

        public GoodsReceivedForm()
        {
            Text = "Goods Received Notes";
            ClientSize = new Size(960, 520);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // 1. TOP
            var top = UiTheme.BuildToolbar(60);
            var lblTitle = UiTheme.BuildHeading("Goods Received Notes");
            lblTitle.Location = new Point(0, 14);
            top.Controls.Add(lblTitle);

            // 2. BOTTOM

            var bottom = UiTheme.BuildBottomBar(64);
            btnNew = new Button { Text = "New GRN", Width = 100, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            btnEdit = new Button { Text = "Edit", Width = 90, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnClose);

            CancelButton = btnClose;

            // 3. FILL - must be last
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
            grid.DoubleClick += (s, e) => DoEdit();
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
            grid.Columns.Add("Id", "Receipt ID");
            grid.Columns.Add("Supplier", "Supplier");
            grid.Columns.Add("Date", "Receive Date");
            grid.Columns.Add("Item", "Item");
            grid.Columns.Add("Qty", "Quantity");
            grid.Columns.Add("PONo", "PO No.");
            grid.Columns.Add("ReceivedBy", "Received By");
            grid.Columns.Add("Condition", "Condition");
            grid.Columns.Add("Remarks", "Remarks");
            UiTheme.AlignNumericColumns(grid);

            foreach (var g in DataStore.Receipts)
            {
                var sup = DataStore.Suppliers.FirstOrDefault(s => s.SupplierId == g.SupplierId);
                string supName = sup != null ? sup.CompanyName : g.SupplierId;
                var item = DataStore.Items.FirstOrDefault(i => i.ItemId == g.ItemId);
                string itemName = item != null ? item.ItemName : g.ItemId;
                grid.Rows.Add(g.ReceiptId, supName, g.ReceiveDate.ToString("yyyy-MM-dd"),
                    itemName, g.Quantity, g.PurchaseOrderNo, g.ReceivedBy, g.Condition, g.Remarks);
            }
        }

        private GoodsReceived Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Receipts.FirstOrDefault(g => g.ReceiptId == id);
        }

        private void DoNew()
        {
            using (var f = new GoodsReceivedEditForm(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoEdit()
        {
            var g = Selected();
            if (g == null) { UiTheme.ShowWarning(this, "Please select a record."); return; }
            using (var f = new GoodsReceivedEditForm(g))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }
    }

    public class GoodsReceivedEditForm : Form
    {
        private readonly GoodsReceived original;
        private TextBox txtId, txtPONo, txtReceivedBy, txtRemarks;
        private ComboBox cmbSupplier, cmbItem, cmbCondition;
        private DateTimePicker dtpDate;
        private NumericUpDown numQty;

        public GoodsReceivedEditForm(GoodsReceived existing)
        {
            original = existing;
            Text = existing == null ? "New Goods Received" : "Edit GRN - " + existing.ReceiptId;
            ClientSize = new Size(530, 460);
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
            header.Controls.Add(new Label
            {
                Text = original == null ? "New Goods Received" : "Edit Goods Received",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                Location = new Point(20, 14),
                AutoSize = true
            });
            Controls.Add(header);

            int y = 70;
            Controls.Add(new Label { Text = "Receipt ID:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(150, y), Width = 180, ReadOnly = true };
            Controls.Add(txtId);

            y += 35;
            Controls.Add(new Label { Text = "Supplier:", Location = new Point(20, y + 3), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(150, y), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var s in DataStore.Suppliers)
                cmbSupplier.Items.Add(new ListItem(s.SupplierId, s.CompanyName));
            cmbSupplier.DisplayMember = "Display";
            Controls.Add(cmbSupplier);

            y += 35;
            Controls.Add(new Label { Text = "Receive Date:", Location = new Point(20, y + 3), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(150, y), Width = 200, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpDate);

            y += 35;
            Controls.Add(new Label { Text = "Item:", Location = new Point(20, y + 3), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(150, y), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var i in DataStore.Items)
                cmbItem.Items.Add(new ListItem(i.ItemId, i.ItemId + " - " + i.ItemName));
            cmbItem.DisplayMember = "Display";
            Controls.Add(cmbItem);

            y += 35;
            Controls.Add(new Label { Text = "Quantity:", Location = new Point(20, y + 3), AutoSize = true });
            numQty = new NumericUpDown { Location = new Point(150, y), Width = 100, Minimum = 1, Maximum = 99999, Value = 1 };
            Controls.Add(numQty);

            y += 35;
            Controls.Add(new Label { Text = "PO No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtPONo = new TextBox { Location = new Point(150, y), Width = 220 };
            Controls.Add(txtPONo);

            y += 35;
            Controls.Add(new Label { Text = "Received By:", Location = new Point(20, y + 3), AutoSize = true });
            txtReceivedBy = new TextBox { Location = new Point(150, y), Width = 220 };
            Controls.Add(txtReceivedBy);

            y += 35;
            Controls.Add(new Label { Text = "Condition:", Location = new Point(20, y + 3), AutoSize = true });
            cmbCondition = new ComboBox { Location = new Point(150, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCondition.Items.AddRange(new object[] { "Good", "Damaged", "Partial" });
            Controls.Add(cmbCondition);

            y += 35;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(150, y), Width = 300 };
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
                var ids = DataStore.Receipts != null
                    ? DataStore.Receipts.Select(r => r.ReceiptId)
                    : System.Linq.Enumerable.Empty<string>();
                txtId.Text = DataStore.NextId("GRN", ids);
                dtpDate.Value = DateTime.Today;
                cmbCondition.SelectedIndex = 0;
            }
            else
            {
                txtId.Text = original.ReceiptId;
                for (int i = 0; i < cmbSupplier.Items.Count; i++)
                    if (((ListItem)cmbSupplier.Items[i]).Value == original.SupplierId)
                    { cmbSupplier.SelectedIndex = i; break; }
                dtpDate.Value = original.ReceiveDate == DateTime.MinValue ? DateTime.Today : original.ReceiveDate;
                for (int i = 0; i < cmbItem.Items.Count; i++)
                    if (((ListItem)cmbItem.Items[i]).Value == original.ItemId)
                    { cmbItem.SelectedIndex = i; break; }
                numQty.Value = original.Quantity > 0 ? original.Quantity : 1;
                txtPONo.Text = original.PurchaseOrderNo;
                txtReceivedBy.Text = original.ReceivedBy;
                cmbCondition.SelectedItem = original.Condition ?? "Good";
                if (cmbCondition.SelectedIndex < 0) cmbCondition.SelectedIndex = 0;
                txtRemarks.Text = original.Remarks;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbSupplier.SelectedItem == null) { UiTheme.ShowWarning(this, "Select a supplier."); return; }
            if (cmbItem.SelectedItem == null) { UiTheme.ShowWarning(this, "Select an item."); return; }
            if (string.IsNullOrWhiteSpace(txtReceivedBy.Text)) { UiTheme.ShowWarning(this, "Received By is required."); return; }

            string supplierId = ((ListItem)cmbSupplier.SelectedItem).Value;
            string itemId = ((ListItem)cmbItem.SelectedItem).Value;
            int qty = (int)numQty.Value;

            if (original == null)
            {
                var g = new GoodsReceived
                {
                    ReceiptId = txtId.Text,
                    SupplierId = supplierId,
                    ReceiveDate = dtpDate.Value.Date,
                    ItemId = itemId,
                    Quantity = qty,
                    PurchaseOrderNo = txtPONo.Text.Trim(),
                    ReceivedBy = txtReceivedBy.Text.Trim(),
                    Condition = cmbCondition.SelectedItem as string ?? "Good",
                    Remarks = txtRemarks.Text.Trim()
                };
                var item = DataStore.Items.FirstOrDefault(i => i.ItemId == itemId);
                if (item != null) item.StockQty += qty;
                DataStore.Receipts.Add(g);
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "New GRN", g.ReceiptId);
            }
            else
            {
                original.SupplierId = supplierId;
                original.ReceiveDate = dtpDate.Value.Date;
                original.ItemId = itemId;
                original.Quantity = qty;
                original.PurchaseOrderNo = txtPONo.Text.Trim();
                original.ReceivedBy = txtReceivedBy.Text.Trim();
                original.Condition = cmbCondition.SelectedItem as string ?? "Good";
                original.Remarks = txtRemarks.Text.Trim();
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit GRN", original.ReceiptId);
            }
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
