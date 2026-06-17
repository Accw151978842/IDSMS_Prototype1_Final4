using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class SalesOrderEditForm : Form
    {
        private readonly SalesOrder original;
        private readonly SalesOrder working;
        public bool ReadOnlyMode { get; set; }

        private TextBox txtOrderId, txtRemarks;
        private DateTimePicker dtpOrder, dtpRequired;
        private ComboBox cmbCustomer, cmbStatus;
        private DataGridView gridLines;
        private ComboBox cmbItem;
        private NumericUpDown numQty;
        private Button btnAddLine, btnRemoveLine, btnSave, btnCancel;
        private Label lblTotal;

        public SalesOrderEditForm(SalesOrder existing)
        {
            original = existing;
            working = new SalesOrder();
            if (existing != null)
            {
                working.OrderId = existing.OrderId;
                working.OrderDate = existing.OrderDate;
                working.RequiredDate = existing.RequiredDate;
                working.CustomerId = existing.CustomerId;
                working.Status = existing.Status;
                working.Remarks = existing.Remarks;
                working.CreatedBy = existing.CreatedBy;
                foreach (var ln in existing.Lines)
                {
                    working.Lines.Add(new SalesOrderLine
                    {
                        ItemId = ln.ItemId,
                        ItemName = ln.ItemName,
                        Quantity = ln.Quantity,
                        UnitPrice = ln.UnitPrice
                    });
                }
            }
            else
            {
                working.OrderId = DataStore.NextId("SO", DataStore.SalesOrders.Select(o => o.OrderId));
                working.OrderDate = DateTime.Today;
                working.RequiredDate = DateTime.Today.AddDays(7);
                working.Status = "Pending";
                working.CreatedBy = SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "";
            }

            BuildUI();
            UiTheme.ApplyInputs(this);
            UiTheme.ApplyGrid(gridLines);
            UiTheme.AlignNumericColumns(gridLines);
            BindData();
            UpdateReadOnlyState();
        }

        private void BuildUI()
        {
            Text = original == null ? "Create Sales Order" : "Edit Sales Order - " + working.OrderId;
            ClientSize = new Size(820, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            UiTheme.ApplyForm(this);

            // Header banner — height 44, font 10F to avoid over-sized title at 200% DPI
            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text      = original == null ? "Create Sales Order" : "Edit Sales Order",
                ForeColor = Color.White,
                Font      = new Font(UiTheme.FontFamily, 10F, FontStyle.Bold),
                Location  = new Point(20, 12),
                AutoSize  = true
            });
            Controls.Add(header);

            int y = 68;
            Controls.Add(new Label { Text = "Order No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtOrderId = new TextBox { Location = new Point(110, y), Width = 140, ReadOnly = true };
            Controls.Add(txtOrderId);

            Controls.Add(new Label { Text = "Order Date:", Location = new Point(280, y + 3), AutoSize = true });
            dtpOrder = new DateTimePicker { Location = new Point(370, y), Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpOrder);

            Controls.Add(new Label { Text = "Required:", Location = new Point(540, y + 3), AutoSize = true });
            dtpRequired = new DateTimePicker { Location = new Point(620, y), Width = 140, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpRequired);

            y += 40;
            Controls.Add(new Label { Text = "Customer:", Location = new Point(20, y + 3), AutoSize = true });
            cmbCustomer = new ComboBox { Location = new Point(110, y), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in DataStore.Customers)
                cmbCustomer.Items.Add(new ListItem(c.CustomerId, c.CustomerId + " - " + c.CompanyName));
            cmbCustomer.DisplayMember = "Display";
            Controls.Add(cmbCustomer);

            Controls.Add(new Label { Text = "Status:", Location = new Point(540, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(620, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Pending", "Confirmed", "Shipped", "Completed", "Cancelled" });
            Controls.Add(cmbStatus);

            y += 40;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(110, y), Width = 650 };
            Controls.Add(txtRemarks);

            y += 40;
            var grpLine = new GroupBox
            {
                Text = "Order Lines",
                Location = new Point(15, y),
                Size = new Size(785, 380),
                ForeColor = UiTheme.TextPrimary,
                Font = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold)
            };
            Controls.Add(grpLine);

            grpLine.Controls.Add(new Label { Text = "Item:", Location = new Point(10, 28), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(60, 25), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbItem.DropDownWidth = 560;   // wider dropdown so "Stock: XX)" is not clipped
            foreach (var it in DataStore.Items)
                cmbItem.Items.Add(new ListItem(it.ItemId, it.ItemId + " - " + it.ItemName + " (HKD " + it.UnitPrice.ToString("N2") + ", Stock: " + it.StockQty + ")"));
            cmbItem.DisplayMember = "Display";
            grpLine.Controls.Add(cmbItem);

            grpLine.Controls.Add(new Label { Text = "Qty:", Location = new Point(420, 28), AutoSize = true });
            numQty = new NumericUpDown { Location = new Point(460, 25), Width = 70, Minimum = 1, Maximum = 9999, Value = 1 };
            grpLine.Controls.Add(numQty);

            btnAddLine = new Button { Text = "Add", Location = new Point(545, 22), Width = 80, Height = 28 };
            UiTheme.StylePrimary(btnAddLine);
            btnAddLine.Click += BtnAddLine_Click;
            grpLine.Controls.Add(btnAddLine);

            btnRemoveLine = new Button { Text = "Remove", Location = new Point(635, 22), Width = 90, Height = 28 };
            UiTheme.StyleSecondary(btnRemoveLine);
            btnRemoveLine.Click += BtnRemoveLine_Click;
            grpLine.Controls.Add(btnRemoveLine);

            gridLines = new DataGridView
            {
                Location = new Point(10, 60),
                Size = new Size(760, 250),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridLines.Columns.Add("ItemId",   "Item ID");
            gridLines.Columns.Add("ItemName",  "Item Name");
            gridLines.Columns.Add("Qty",       "Qty");
            gridLines.Columns.Add("Price",     "Unit Price");
            gridLines.Columns.Add("Total",     "Line Total");
            grpLine.Controls.Add(gridLines);

            lblTotal = new Label
            {
                Text      = "Total: HKD 0.00",
                Location  = new Point(560, 325),
                AutoSize  = true,
                Font      = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                ForeColor = UiTheme.Primary
            };
            grpLine.Controls.Add(lblTotal);

            btnSave = new Button { Text = "Save", Location = new Point(20, 575), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;

            btnCancel = new Button { Text = "Close", Location = new Point(128, 575), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void BindData()
        {
            txtOrderId.Text = working.OrderId;
            dtpOrder.Value  = working.OrderDate;
            dtpRequired.Value = working.RequiredDate;
            for (int i = 0; i < cmbCustomer.Items.Count; i++)
            {
                if (((ListItem)cmbCustomer.Items[i]).Value == working.CustomerId)
                { cmbCustomer.SelectedIndex = i; break; }
            }
            cmbStatus.SelectedItem = working.Status ?? "Pending";
            txtRemarks.Text = working.Remarks;
            RefreshLines();
        }

        private void UpdateReadOnlyState()
        {
            if (!ReadOnlyMode) return;
            dtpOrder.Enabled     = false;
            dtpRequired.Enabled  = false;
            cmbCustomer.Enabled  = false;
            cmbStatus.Enabled    = false;
            txtRemarks.ReadOnly  = true;
            cmbItem.Enabled      = false;
            numQty.Enabled       = false;
            btnAddLine.Enabled   = false;
            btnRemoveLine.Enabled = false;
            btnSave.Visible      = false;
            btnCancel.Text       = "Close";
            Text = "View Sales Order - " + working.OrderId;
        }

        private void RefreshLines()
        {
            gridLines.Rows.Clear();
            foreach (var ln in working.Lines)
                gridLines.Rows.Add(ln.ItemId, ln.ItemName, ln.Quantity,
                    ln.UnitPrice.ToString("N2"), ln.LineTotal.ToString("N2"));
            lblTotal.Text = "Total: HKD " + working.TotalAmount.ToString("N2");
        }

        private void BtnAddLine_Click(object sender, EventArgs e)
        {
            if (cmbItem.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select an item."); return; }
            string itemId = ((ListItem)cmbItem.SelectedItem).Value;
            var item = DataStore.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) return;
            int qty = (int)numQty.Value;
            var existing = working.Lines.FirstOrDefault(l => l.ItemId == itemId);
            if (existing != null) existing.Quantity += qty;
            else working.Lines.Add(new SalesOrderLine
            {
                ItemId    = item.ItemId,
                ItemName  = item.ItemName,
                Quantity  = qty,
                UnitPrice = item.UnitPrice
            });
            RefreshLines();
        }

        private void BtnRemoveLine_Click(object sender, EventArgs e)
        {
            if (gridLines.CurrentRow == null) return;
            string itemId = gridLines.CurrentRow.Cells[0].Value as string;
            var line = working.Lines.FirstOrDefault(l => l.ItemId == itemId);
            if (line != null) working.Lines.Remove(line);
            RefreshLines();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbCustomer.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select a customer."); return; }
            if (working.Lines.Count == 0) { UiTheme.ShowWarning(this, "Order must have at least one line item."); return; }
            if (dtpRequired.Value.Date < dtpOrder.Value.Date)
            { UiTheme.ShowWarning(this, "Required date cannot be earlier than the order date."); return; }

            string selectedCustomerId = ((ListItem)cmbCustomer.SelectedItem).Value;
            if (!DataStore.CustomerExists(selectedCustomerId))
            {
                UiTheme.ShowWarning(this,
                    "The selected customer no longer exists. Please reopen the form and pick a valid customer.");
                return;
            }

            working.CustomerId   = selectedCustomerId;
            working.OrderDate    = dtpOrder.Value.Date;
            working.RequiredDate = dtpRequired.Value.Date;
            working.Status       = cmbStatus.SelectedItem as string ?? "Pending";
            working.Remarks      = txtRemarks.Text.Trim();

            // The order object that will actually be persisted: the new
            // record for a create, or the existing record for an edit.
            SalesOrder target;

            bool added = false;
            if (original == null)
            {
                DataStore.SalesOrders.Add(working);
                added = true;
                target = working;
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Create Order", working.OrderId);
            }
            else
            {
                original.OrderDate    = working.OrderDate;
                original.RequiredDate = working.RequiredDate;
                original.CustomerId   = working.CustomerId;
                original.Status       = working.Status;
                original.Remarks      = working.Remarks;
                original.Lines.Clear();
                foreach (var l in working.Lines)
                    original.Lines.Add(new SalesOrderLine
                    {
                        ItemId    = l.ItemId,
                        ItemName  = l.ItemName,
                        Quantity  = l.Quantity,
                        UnitPrice = l.UnitPrice
                    });
                target = original;
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit Order", working.OrderId);
            }

            // ---- Stock movement on shipping --------------------------------
            // If this save moves the order into a shipped state for the first
            // time, make sure there is enough stock before deducting. Stock is
            // restored automatically if it is moved back out of a shipped state.
            bool firstTimeShipping = InventoryService.IsShippedStatus(target.Status)
                && !target.StockDeducted;
            if (firstTimeShipping)
            {
                string shortage;
                if (!InventoryService.CanFulfill(target, out shortage))
                {
                    if (added) DataStore.SalesOrders.Remove(working);
                    UiTheme.ShowWarning(this, shortage +
                        "\r\n\r\nRecord more inward goods or reduce the order quantity, then try again.");
                    return;
                }
            }
            InventoryService.ApplyStatusChange(target);

            try { DataStore.SaveAll(); }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                if (added) DataStore.SalesOrders.Remove(working);
                string msg = ex.Number == 1452
                    ? "Cannot save order: the selected customer was not found in the database. " +
                      "Please add the customer in Master Data first, then try again."
                    : "Database error while saving order:\n" + ex.Message;
                UiTheme.ShowWarning(this, msg);
                return;
            }
            catch (InvalidOperationException ex)
            {
                if (added) DataStore.SalesOrders.Remove(working);
                UiTheme.ShowWarning(this, ex.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
