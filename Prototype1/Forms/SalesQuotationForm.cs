using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    // =================================================================
    //  SALES QUOTATION LIST  —  quotes issued to customers
    //  Flow: Draft -> Sent -> Accepted -> Convert to Sales Order
    // =================================================================
    public class SalesQuotationForm : Form
    {
        private DataGridView grid;
        private Button btnNew, btnEdit, btnView, btnConvert, btnPreview, btnDelete, btnClose;
        private TextBox txtSearch;
        private ComboBox cmbStatusFilter;

        public SalesQuotationForm()
        {
            Text = "Sales Quotations";
            ClientSize = new Size(980, 600);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // 1. TOP
            var top = UiTheme.BuildToolbar(116);
            var lblTitle = UiTheme.BuildHeading("Sales Quotations");
            lblTitle.Location = new Point(16, 10);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Prepare price quotations for customers and convert accepted quotes to sales orders.", Location = new Point(16, 44), AutoSize = true, ForeColor = UiTheme.TextMuted });

            top.Controls.Add(new Label { Text = "Search:", Location = new Point(16, 84), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(72, 81), Width = 240, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Status:", Location = new Point(330, 84), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbStatusFilter = new ComboBox { Location = new Point(382, 81), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "Draft", "Sent", "Accepted", "Rejected", "Expired", "Converted" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbStatusFilter);

            // 2. BOTTOM
            var bottom = UiTheme.BuildBottomBar(64);
            btnNew = new Button { Text = "New Quotation", Width = 130, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            btnEdit = new Button { Text = "Edit", Width = 80, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            btnView = new Button { Text = "View", Width = 80, Height = 34 };
            btnView.Click += (s, e) => DoView();
            btnConvert = new Button { Text = "Convert to Order", Width = 150, Height = 34 };
            btnConvert.Click += (s, e) => DoConvert();
            btnPreview = new Button { Text = "Preview", Width = 100, Height = 34 };
            btnPreview.Click += (s, e) => DoPreview();
            btnDelete = new Button { Text = "Delete", Width = 90, Height = 34 };
            btnDelete.Click += (s, e) => DoDelete();
            btnClose = new Button { Text = "Close", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnView, btnConvert, btnPreview, btnDelete, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnView);
            UiTheme.StyleAccent(btnConvert);
            UiTheme.StyleSecondary(btnPreview);
            UiTheme.StyleDangerOutlined(btnDelete);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnView, btnConvert, btnPreview, btnDelete, btnClose);
            CancelButton = btnClose;

            // 3. FILL
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
            grid.DoubleClick += (s, e) => DoView();
            body.Controls.Add(grid);
            Controls.Add(body);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
        }

        private void LoadGrid()
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("Id", "Quotation No.");
            grid.Columns.Add("Customer", "Customer");
            grid.Columns.Add("QuoteDate", "Quote Date");
            grid.Columns.Add("ValidUntil", "Valid Until");
            grid.Columns.Add("Total", "Total (HKD)");
            grid.Columns.Add("Status", "Status");
            grid.Columns.Add("ConvertedTo", "Converted Order");
            UiTheme.AlignNumericColumns(grid, "Total (HKD)");

            string keyword = txtSearch != null ? txtSearch.Text.Trim().ToLowerInvariant() : "";
            string statusFilter = cmbStatusFilter != null ? cmbStatusFilter.SelectedItem as string : "All Status";

            foreach (var q in DataStore.SalesQuotations)
            {
                var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == q.CustomerId);
                string cust = c != null ? c.CompanyName : (q.CustomerId ?? "(unknown)");

                if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All Status"
                    && !string.Equals(q.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (keyword.Length > 0)
                {
                    string haystack = ((q.QuotationId ?? "") + " " + cust + " " + (q.Status ?? "")).ToLowerInvariant();
                    if (!haystack.Contains(keyword)) continue;
                }

                grid.Rows.Add(q.QuotationId, cust,
                    q.QuoteDate.ToString("yyyy-MM-dd"),
                    q.ValidUntil.ToString("yyyy-MM-dd"),
                    q.TotalAmount.ToString("N2"),
                    q.Status,
                    string.IsNullOrEmpty(q.ConvertedOrderId) ? "" : q.ConvertedOrderId);
            }
        }

        private SalesQuotation Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.SalesQuotations.FirstOrDefault(q => q.QuotationId == id);
        }

        private void DoNew()
        {
            using (var f = new SalesQuotationEditForm(null))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoEdit()
        {
            var q = Selected();
            if (q == null) { UiTheme.ShowWarning(this, "Please select a quotation first."); return; }
            if (q.Status == "Converted")
            {
                UiTheme.ShowWarning(this, "This quotation has been converted to a sales order and can no longer be edited.");
                return;
            }
            using (var f = new SalesQuotationEditForm(q))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoView()
        {
            var q = Selected();
            if (q == null) { UiTheme.ShowWarning(this, "Please select a quotation first."); return; }
            using (var f = new SalesQuotationEditForm(q) { ReadOnlyMode = true })
                f.ShowDialog(this);
        }

        private void DoConvert()
        {
            var q = Selected();
            if (q == null) { UiTheme.ShowWarning(this, "Please select a quotation first."); return; }
            if (q.Status == "Converted" || !string.IsNullOrEmpty(q.ConvertedOrderId))
            {
                UiTheme.ShowWarning(this, "This quotation has already been converted to order " + q.ConvertedOrderId + ".");
                return;
            }
            if (q.Lines == null || q.Lines.Count == 0)
            {
                UiTheme.ShowWarning(this, "Cannot convert: the quotation has no line items.");
                return;
            }
            if (!DataStore.CustomerExists(q.CustomerId))
            {
                UiTheme.ShowWarning(this, "Cannot convert: the customer on this quotation no longer exists.");
                return;
            }
            if (!UiTheme.ShowConfirm(this, "Convert quotation " + q.QuotationId + " into a new sales order?")) return;

            var order = new SalesOrder
            {
                OrderId = DataStore.NextId("SO", DataStore.SalesOrders.Select(o => o.OrderId)),
                OrderDate = DateTime.Today,
                RequiredDate = DateTime.Today.AddDays(7),
                CustomerId = q.CustomerId,
                Status = "Pending",
                Remarks = "Converted from quotation " + q.QuotationId,
                CreatedBy = SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : ""
            };
            foreach (var ln in q.Lines)
                order.Lines.Add(new SalesOrderLine
                {
                    ItemId = ln.ItemId,
                    ItemName = ln.ItemName,
                    Quantity = ln.Quantity,
                    UnitPrice = ln.UnitPrice
                });

            DataStore.SalesOrders.Add(order);
            q.Status = "Converted";
            q.ConvertedOrderId = order.OrderId;

            try { DataStore.SaveAll(); }
            catch (Exception ex)
            {
                // roll back the in-memory change so state stays consistent
                DataStore.SalesOrders.Remove(order);
                q.Status = "Accepted";
                q.ConvertedOrderId = "";
                UiTheme.ShowWarning(this, "Could not convert quotation:\r\n" + ex.Message);
                return;
            }

            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                "Convert Quotation", q.QuotationId + " -> " + order.OrderId);
            LoadGrid();
            UiTheme.ShowInfo(this, "Quotation converted to sales order " + order.OrderId +
                ". You can now process it under Sales Orders.");
        }

        private void DoDelete()
        {
            var q = Selected();
            if (q == null) { UiTheme.ShowWarning(this, "Please select a quotation first."); return; }
            if (q.Status == "Converted")
            {
                UiTheme.ShowWarning(this, "A converted quotation cannot be deleted (its sales order already exists).");
                return;
            }
            if (!UiTheme.ShowConfirm(this, "Delete quotation " + q.QuotationId + "?")) return;
            DataStore.SalesQuotations.Remove(q);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                "Delete Quotation", q.QuotationId);
            DataStore.SaveAll();
            LoadGrid();
        }

        private void DoPreview()
        {
            var q = Selected();
            if (q == null) { UiTheme.ShowWarning(this, "Please select a quotation first."); return; }

            var c = DataStore.Customers.FirstOrDefault(x => x.CustomerId == q.CustomerId);
            string text = "PREMIUM LIVING FURNITURE CO. LTD." + Environment.NewLine +
                          "SALES QUOTATION" + Environment.NewLine +
                          "-----------------------------------" + Environment.NewLine +
                          "Quotation No: " + q.QuotationId + Environment.NewLine +
                          "Quote Date  : " + q.QuoteDate.ToString("yyyy-MM-dd") + Environment.NewLine +
                          "Valid Until : " + q.ValidUntil.ToString("yyyy-MM-dd") + Environment.NewLine +
                          "Status      : " + q.Status + Environment.NewLine;
            if (c != null)
            {
                text += "-----------------------------------" + Environment.NewLine +
                        "Quote To    : " + c.CompanyName + Environment.NewLine +
                        "Address     : " + c.Address + Environment.NewLine +
                        "Contact     : " + c.ContactPerson + " (" + c.Phone + ")" + Environment.NewLine;
            }
            text += "-----------------------------------" + Environment.NewLine +
                    "Items:" + Environment.NewLine +
                    "  Item                         Qty     Unit Price      Line Total" + Environment.NewLine;
            foreach (var ln in q.Lines)
            {
                string nm = ((ln.ItemId ?? "") + " " + (ln.ItemName ?? "")).Trim();
                if (nm.Length > 28) nm = nm.Substring(0, 28);
                text += "  " + nm.PadRight(28) +
                        " " + ln.Quantity.ToString().PadLeft(5) +
                        "   " + ("HKD " + ln.UnitPrice.ToString("N2")).PadLeft(12) +
                        "   " + ("HKD " + ln.Subtotal.ToString("N2")).PadLeft(13) + Environment.NewLine;
            }
            text += "-----------------------------------" + Environment.NewLine +
                    "TOTAL: HKD " + q.TotalAmount.ToString("N2") + Environment.NewLine + Environment.NewLine +
                    "Remarks: " + (q.Remarks ?? "") + Environment.NewLine + Environment.NewLine +
                    "This quotation is valid until the date shown above." + Environment.NewLine +
                    "Customer Acceptance: ______________________  Date: __________";

            using (var prev = new Form())
            {
                prev.Text = "Quotation Preview - " + q.QuotationId;
                prev.ClientSize = new Size(640, 520);
                prev.StartPosition = FormStartPosition.CenterParent;
                var tb = new TextBox
                {
                    Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10F), ScrollBars = ScrollBars.Both, WordWrap = false, Text = text
                };
                prev.Controls.Add(tb);
                prev.ShowDialog(this);
            }
        }
    }

    // =================================================================
    //  SALES QUOTATION EDIT  —  create / edit a customer quotation
    // =================================================================
    public class SalesQuotationEditForm : Form
    {
        private readonly SalesQuotation original;
        private readonly SalesQuotation working;
        public bool ReadOnlyMode { get; set; }

        private TextBox txtId, txtRemarks;
        private DateTimePicker dtpQuote, dtpValid;
        private ComboBox cmbCustomer, cmbStatus, cmbItem;
        private NumericUpDown numQty;
        private DataGridView gridLines;
        private Button btnAddLine, btnRemoveLine, btnSave, btnCancel;
        private Label lblTotal;

        public SalesQuotationEditForm(SalesQuotation existing)
        {
            original = existing;
            working = new SalesQuotation();
            if (existing != null)
            {
                working.QuotationId = existing.QuotationId;
                working.CustomerId = existing.CustomerId;
                working.QuoteDate = existing.QuoteDate;
                working.ValidUntil = existing.ValidUntil;
                working.Status = existing.Status;
                working.ConvertedOrderId = existing.ConvertedOrderId;
                working.CreatedBy = existing.CreatedBy;
                working.Remarks = existing.Remarks;
                foreach (var ln in existing.Lines)
                    working.Lines.Add(new SalesQuotationLine
                    {
                        ItemId = ln.ItemId, ItemName = ln.ItemName,
                        Quantity = ln.Quantity, UnitPrice = ln.UnitPrice
                    });
            }
            else
            {
                working.QuotationId = DataStore.NextId("SQ", DataStore.SalesQuotations.Select(q => q.QuotationId));
                working.QuoteDate = DateTime.Today;
                working.ValidUntil = DateTime.Today.AddDays(14);
                working.Status = "Draft";
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
            Text = original == null ? "Create Sales Quotation" : "Edit Sales Quotation - " + working.QuotationId;
            ClientSize = new Size(820, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            UiTheme.ApplyForm(this);

            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text = original == null ? "Create Sales Quotation" : "Edit Sales Quotation",
                ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 10F, FontStyle.Bold),
                Location = new Point(20, 12), AutoSize = true
            });
            Controls.Add(header);

            int y = 68;
            Controls.Add(new Label { Text = "Quotation No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(130, y), Width = 140, ReadOnly = true };
            Controls.Add(txtId);

            Controls.Add(new Label { Text = "Quote Date:", Location = new Point(300, y + 3), AutoSize = true });
            dtpQuote = new DateTimePicker { Location = new Point(390, y), Width = 130, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpQuote);

            Controls.Add(new Label { Text = "Valid Until:", Location = new Point(540, y + 3), AutoSize = true });
            dtpValid = new DateTimePicker { Location = new Point(630, y), Width = 130, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpValid);

            y += 40;
            Controls.Add(new Label { Text = "Customer:", Location = new Point(20, y + 3), AutoSize = true });
            cmbCustomer = new ComboBox { Location = new Point(130, y), Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var c in DataStore.Customers)
                cmbCustomer.Items.Add(new ListItem(c.CustomerId, c.CustomerId + " - " + c.CompanyName));
            cmbCustomer.DisplayMember = "Display";
            Controls.Add(cmbCustomer);

            Controls.Add(new Label { Text = "Status:", Location = new Point(540, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(630, y), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Draft", "Sent", "Accepted", "Rejected", "Expired" });
            Controls.Add(cmbStatus);

            y += 40;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(130, y), Width = 630 };
            Controls.Add(txtRemarks);

            y += 40;
            var grpLine = new GroupBox
            {
                Text = "Quotation Lines",
                Location = new Point(15, y),
                Size = new Size(785, 380),
                ForeColor = UiTheme.TextPrimary,
                Font = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold)
            };
            Controls.Add(grpLine);

            grpLine.Controls.Add(new Label { Text = "Item:", Location = new Point(10, 28), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(60, 25), Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var it in DataStore.Items)
                cmbItem.Items.Add(new ListItem(it.ItemId, it.ItemId + " - " + it.ItemName + " (HKD " + it.UnitPrice.ToString("N2") + ")"));
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
            gridLines.Columns.Add("ItemId", "Item ID");
            gridLines.Columns.Add("ItemName", "Item Name");
            gridLines.Columns.Add("Qty", "Qty");
            gridLines.Columns.Add("Price", "Unit Price");
            gridLines.Columns.Add("Total", "Line Total");
            grpLine.Controls.Add(gridLines);

            lblTotal = new Label
            {
                Text = "Total: HKD 0.00",
                Location = new Point(560, 325),
                AutoSize = true,
                Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
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
            txtId.Text = working.QuotationId;
            dtpQuote.Value = working.QuoteDate;
            dtpValid.Value = working.ValidUntil;
            for (int i = 0; i < cmbCustomer.Items.Count; i++)
                if (((ListItem)cmbCustomer.Items[i]).Value == working.CustomerId)
                { cmbCustomer.SelectedIndex = i; break; }
            // "Converted" is not in the editable list; fall back to Accepted display.
            string st = working.Status == "Converted" ? "Accepted" : (working.Status ?? "Draft");
            cmbStatus.SelectedItem = st;
            if (cmbStatus.SelectedIndex < 0) cmbStatus.SelectedIndex = 0;
            txtRemarks.Text = working.Remarks;
            RefreshLines();
        }

        private void UpdateReadOnlyState()
        {
            if (!ReadOnlyMode) return;
            dtpQuote.Enabled = false;
            dtpValid.Enabled = false;
            cmbCustomer.Enabled = false;
            cmbStatus.Enabled = false;
            txtRemarks.ReadOnly = true;
            cmbItem.Enabled = false;
            numQty.Enabled = false;
            btnAddLine.Enabled = false;
            btnRemoveLine.Enabled = false;
            btnSave.Visible = false;
            btnCancel.Text = "Close";
            Text = "View Sales Quotation - " + working.QuotationId;
        }

        private void RefreshLines()
        {
            gridLines.Rows.Clear();
            foreach (var ln in working.Lines)
                gridLines.Rows.Add(ln.ItemId, ln.ItemName, ln.Quantity,
                    ln.UnitPrice.ToString("N2"), ln.Subtotal.ToString("N2"));
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
            else working.Lines.Add(new SalesQuotationLine
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = qty,
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
            if (working.Lines.Count == 0) { UiTheme.ShowWarning(this, "Quotation must have at least one line item."); return; }
            if (dtpValid.Value.Date < dtpQuote.Value.Date)
            { UiTheme.ShowWarning(this, "Valid-until date cannot be earlier than the quote date."); return; }

            string selectedCustomerId = ((ListItem)cmbCustomer.SelectedItem).Value;
            if (!DataStore.CustomerExists(selectedCustomerId))
            {
                UiTheme.ShowWarning(this, "The selected customer no longer exists. Please reopen the form and pick a valid customer.");
                return;
            }

            working.CustomerId = selectedCustomerId;
            working.QuoteDate = dtpQuote.Value.Date;
            working.ValidUntil = dtpValid.Value.Date;
            working.Status = cmbStatus.SelectedItem as string ?? "Draft";
            working.Remarks = txtRemarks.Text.Trim();

            bool added = false;
            if (original == null)
            {
                DataStore.SalesQuotations.Add(working);
                added = true;
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Create Quotation", working.QuotationId);
            }
            else
            {
                original.CustomerId = working.CustomerId;
                original.QuoteDate = working.QuoteDate;
                original.ValidUntil = working.ValidUntil;
                original.Status = working.Status;
                original.Remarks = working.Remarks;
                original.Lines.Clear();
                foreach (var ln in working.Lines)
                    original.Lines.Add(new SalesQuotationLine
                    {
                        ItemId = ln.ItemId, ItemName = ln.ItemName,
                        Quantity = ln.Quantity, UnitPrice = ln.UnitPrice
                    });
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit Quotation", working.QuotationId);
            }

            try { DataStore.SaveAll(); }
            catch (Exception ex)
            {
                if (added) DataStore.SalesQuotations.Remove(working);
                UiTheme.ShowWarning(this, "Database error while saving quotation:\r\n" + ex.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
