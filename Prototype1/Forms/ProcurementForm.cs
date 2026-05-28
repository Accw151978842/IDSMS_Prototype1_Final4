using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    // ============================================================
    //  Procurement (Purchase Order) - LIST FORM
    // ============================================================
    public class ProcurementForm : Form
    {
        private DataGridView grid;
        private Button btnNew, btnEdit, btnView, btnCancel_, btnClose, btnRefresh;
        private TextBox txtSearch;
        private ComboBox cmbStatus;
        private CheckBox chkDateFilter;
        private DateTimePicker dtpFrom, dtpTo;

        public ProcurementForm()
        {
            Text = "Purchase Orders (Procurement)";
            ClientSize = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
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
            UiTheme.ApplyGrid(grid);
            UiTheme.EnableStatusColumn(grid);
            grid.DoubleClick += (s, e) => DoView();
            body.Controls.Add(grid);
            Controls.Add(body);

            var top = UiTheme.BuildToolbar(72);
            var lblTitle = UiTheme.BuildHeading("Purchase Orders (Procurement)");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);

            top.Controls.Add(new Label { Text = "Search:", Location = new Point(0, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(56, 39), Width = 230, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Status:", Location = new Point(305, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbStatus = new ComboBox { Location = new Point(355, 39), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "(All)", "Draft", "Sent", "PartiallyReceived", "Completed", "Cancelled" });
            cmbStatus.SelectedIndex = 0;
            UiTheme.StyleComboBox(cmbStatus);
            cmbStatus.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbStatus);

            chkDateFilter = new CheckBox { Text = "Date range:", Location = new Point(525, 41), AutoSize = true, ForeColor = UiTheme.TextMuted };
            chkDateFilter.CheckedChanged += (s, e) => { dtpFrom.Enabled = dtpTo.Enabled = chkDateFilter.Checked; LoadGrid(); };
            top.Controls.Add(chkDateFilter);

            dtpFrom = new DateTimePicker { Location = new Point(625, 38), Width = 110, Format = DateTimePickerFormat.Short, Enabled = false, Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadGrid(); };
            top.Controls.Add(dtpFrom);
            top.Controls.Add(new Label { Text = "to", Location = new Point(740, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            dtpTo = new DateTimePicker { Location = new Point(760, 38), Width = 110, Format = DateTimePickerFormat.Short, Enabled = false, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadGrid(); };
            top.Controls.Add(dtpTo);

            btnRefresh = new Button { Text = "Refresh", Location = new Point(880, 37), Width = 80, Height = 28 };
            UiTheme.StyleSecondary(btnRefresh);
            btnRefresh.Click += (s, e) => LoadGrid();
            top.Controls.Add(btnRefresh);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);

            var bottom = UiTheme.BuildBottomBar(64);
            btnNew     = new Button { Text = "New PO",       Width = 100, Height = 34 };
            btnEdit    = new Button { Text = "Edit",         Width =  90, Height = 34 };
            btnView    = new Button { Text = "View",         Width =  90, Height = 34 };
            btnCancel_ = new Button { Text = "Cancel PO",    Width = 110, Height = 34 };
            btnClose   = new Button { Text = "Close",        Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            btnNew.Click     += (s, e) => DoNew();
            btnEdit.Click    += (s, e) => DoEdit();
            btnView.Click    += (s, e) => DoView();
            btnCancel_.Click += (s, e) => DoCancel();
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnView, btnCancel_, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnView);
            UiTheme.StyleDangerOutlined(btnCancel_);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnView, btnCancel_, btnClose);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
            CancelButton = btnClose;
        }

        private void LoadGrid()
        {
            grid.Columns.Clear();
            UiTheme.ApplyGrid(grid);
            UiTheme.EnableStatusColumn(grid);

            grid.Columns.Add("PoId",      "PO No.");
            grid.Columns.Add("OrderDate", "Order Date");
            grid.Columns.Add("Supplier",  "Supplier");
            grid.Columns.Add("Expected",  "Expected Delivery");
            grid.Columns.Add("Status",    "Status");
            grid.Columns.Add("LinkedRmr", "Linked RMR");
            grid.Columns.Add("Lines",     "Lines");
            grid.Columns.Add("Total",     "Total (HKD)");

            UiTheme.AlignNumericColumns(grid);

            string filter = txtSearch.Text.Trim().ToLower();
            string status = cmbStatus.SelectedItem as string;
            bool useDate = chkDateFilter != null && chkDateFilter.Checked;
            DateTime from = dtpFrom != null ? dtpFrom.Value.Date : DateTime.MinValue;
            DateTime to   = dtpTo   != null ? dtpTo.Value.Date   : DateTime.MaxValue;

            int shown = 0;
            foreach (var p in DataStore.Procurements)
            {
                var sup = DataStore.Suppliers.FirstOrDefault(s => s.SupplierId == p.SupplierId);
                string supName = sup != null ? sup.CompanyName : (p.SupplierId ?? "");
                if (status != null && status != "(All)" && p.Status != status) continue;
                if (useDate && (p.OrderDate.Date < from || p.OrderDate.Date > to)) continue;
                if (filter.Length > 0)
                {
                    string hay = (p.PoId + " " + supName + " " + p.Status + " " + (p.LinkedRmrId ?? "") + " " + (p.Remarks ?? "")).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(p.PoId, p.OrderDate.ToString("yyyy-MM-dd"), supName,
                    p.ExpectedDelivery.ToString("yyyy-MM-dd"), p.Status,
                    string.IsNullOrEmpty(p.LinkedRmrId) ? "-" : p.LinkedRmrId,
                    p.Lines.Count, p.TotalAmount.ToString("N2"));
                shown++;
            }
            Text = "Purchase Orders - " + shown + " record(s)";
        }

        private Procurement Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Procurements.FirstOrDefault(p => p.PoId == id);
        }

        private void DoNew()
        {
            using (var f = new ProcurementEditForm(null))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoEdit()
        {
            var p = Selected();
            if (p == null) { UiTheme.ShowWarning(this, "Please select a PO first."); return; }
            if (p.Status == "Completed" || p.Status == "Cancelled")
            { UiTheme.ShowWarning(this, "Completed or cancelled POs cannot be edited."); return; }
            using (var f = new ProcurementEditForm(p))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoView()
        {
            var p = Selected();
            if (p == null) { UiTheme.ShowWarning(this, "Please select a PO first."); return; }
            using (var f = new ProcurementEditForm(p) { ReadOnlyMode = true })
                f.ShowDialog(this);
        }

        private void DoCancel()
        {
            var p = Selected();
            if (p == null) { UiTheme.ShowWarning(this, "Please select a PO first."); return; }
            if (!UiTheme.ShowConfirm(this, "Cancel PO " + p.PoId + "?")) return;
            p.Status = "Cancelled";
            SecurityService.Audit(
                SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                "Cancel PO", p.PoId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    // ============================================================
    //  Procurement EDIT FORM
    // ============================================================
    public class ProcurementEditForm : Form
    {
        private readonly Procurement original;
        private readonly Procurement working;
        public bool ReadOnlyMode { get; set; }

        private TextBox txtPoId, txtRemarks;
        private DateTimePicker dtpOrder, dtpExpected;
        private ComboBox cmbSupplier, cmbStatus, cmbLinkedRmr;
        private DataGridView gridLines;
        private ComboBox cmbItem;
        private NumericUpDown numQty;
        private TextBox txtUnitPrice;
        private Button btnAddLine, btnRemoveLine, btnSave, btnCancel, btnImportRmr;
        private Label lblTotal;

        public ProcurementEditForm(Procurement existing)
        {
            original = existing;
            working = new Procurement();
            if (existing != null)
            {
                working.PoId = existing.PoId;
                working.SupplierId = existing.SupplierId;
                working.OrderDate = existing.OrderDate;
                working.ExpectedDelivery = existing.ExpectedDelivery;
                working.Status = existing.Status;
                working.LinkedRmrId = existing.LinkedRmrId;
                working.CreatedBy = existing.CreatedBy;
                working.Remarks = existing.Remarks;
                foreach (var ln in existing.Lines)
                    working.Lines.Add(new ProcurementLine
                    {
                        ItemId = ln.ItemId, ItemName = ln.ItemName,
                        Quantity = ln.Quantity, UnitPrice = ln.UnitPrice
                    });
            }
            else
            {
                working.PoId = DataStore.NextId("PO", DataStore.Procurements.Select(p => p.PoId));
                working.OrderDate = DateTime.Today;
                working.ExpectedDelivery = DateTime.Today.AddDays(14);
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
            Text = original == null ? "Create Purchase Order" : "Edit PO - " + working.PoId;
            ClientSize = new Size(860, 660);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            UiTheme.ApplyForm(this);

            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text      = original == null ? "Create Purchase Order" : "Edit Purchase Order",
                ForeColor = Color.White,
                Font      = new Font(UiTheme.FontFamily, 10F, FontStyle.Bold),
                Location  = new Point(20, 12), AutoSize = true
            });
            Controls.Add(header);

            int y = 68;
            Controls.Add(new Label { Text = "PO No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtPoId = new TextBox { Location = new Point(110, y), Width = 140, ReadOnly = true };
            Controls.Add(txtPoId);

            Controls.Add(new Label { Text = "Order Date:", Location = new Point(280, y + 3), AutoSize = true });
            dtpOrder = new DateTimePicker { Location = new Point(370, y), Width = 140, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpOrder);

            Controls.Add(new Label { Text = "Expected:", Location = new Point(540, y + 3), AutoSize = true });
            dtpExpected = new DateTimePicker { Location = new Point(620, y), Width = 140, Format = DateTimePickerFormat.Short };
            Controls.Add(dtpExpected);

            y += 40;
            Controls.Add(new Label { Text = "Supplier:", Location = new Point(20, y + 3), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(110, y), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var s in DataStore.Suppliers)
                cmbSupplier.Items.Add(new ListItem(s.SupplierId, s.SupplierId + " - " + s.CompanyName));
            cmbSupplier.DisplayMember = "Display";
            Controls.Add(cmbSupplier);

            Controls.Add(new Label { Text = "Status:", Location = new Point(540, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(620, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Draft", "Sent", "PartiallyReceived", "Completed", "Cancelled" });
            Controls.Add(cmbStatus);

            y += 40;
            Controls.Add(new Label { Text = "Linked RMR:", Location = new Point(20, y + 3), AutoSize = true });
            cmbLinkedRmr = new ComboBox { Location = new Point(110, y), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbLinkedRmr.Items.Add(new ListItem("", "(None)"));
            foreach (var r in DataStore.RawMaterialRequests)
            {
                if (r.Status == "Approved" || r.Status == "Procured")
                    cmbLinkedRmr.Items.Add(new ListItem(r.RmrId, r.RmrId + " - " + r.Department + " (" + r.Lines.Count + " items)"));
            }
            cmbLinkedRmr.DisplayMember = "Display";
            cmbLinkedRmr.SelectedIndex = 0;
            Controls.Add(cmbLinkedRmr);

            btnImportRmr = new Button { Text = "Import Items from RMR", Location = new Point(370, y - 2), Width = 170, Height = 28 };
            UiTheme.StyleAccent(btnImportRmr);
            btnImportRmr.Click += BtnImportRmr_Click;
            Controls.Add(btnImportRmr);

            y += 40;
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(20, y + 3), AutoSize = true });
            txtRemarks = new TextBox { Location = new Point(110, y), Width = 690 };
            Controls.Add(txtRemarks);

            y += 40;
            var grpLine = new GroupBox
            {
                Text = "Order Lines", Location = new Point(15, y), Size = new Size(825, 380),
                ForeColor = UiTheme.TextPrimary,
                Font = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold)
            };
            Controls.Add(grpLine);

            grpLine.Controls.Add(new Label { Text = "Item:", Location = new Point(10, 28), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(60, 25), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var it in DataStore.Items)
                cmbItem.Items.Add(new ListItem(it.ItemId, it.ItemId + " - " + it.ItemName + " (HKD " + it.UnitPrice.ToString("N2") + ")"));
            cmbItem.DisplayMember = "Display";
            cmbItem.SelectedIndexChanged += (s, e) =>
            {
                if (cmbItem.SelectedItem == null) return;
                string id = ((ListItem)cmbItem.SelectedItem).Value;
                var it = DataStore.Items.FirstOrDefault(i => i.ItemId == id);
                if (it != null) txtUnitPrice.Text = it.UnitPrice.ToString("N2");
            };
            grpLine.Controls.Add(cmbItem);

            grpLine.Controls.Add(new Label { Text = "Qty:", Location = new Point(390, 28), AutoSize = true });
            numQty = new NumericUpDown { Location = new Point(425, 25), Width = 65, Minimum = 1, Maximum = 9999, Value = 1 };
            grpLine.Controls.Add(numQty);

            grpLine.Controls.Add(new Label { Text = "Unit Price:", Location = new Point(500, 28), AutoSize = true });
            txtUnitPrice = new TextBox { Location = new Point(572, 25), Width = 80 };
            grpLine.Controls.Add(txtUnitPrice);

            btnAddLine = new Button { Text = "Add", Location = new Point(660, 22), Width = 65, Height = 28 };
            UiTheme.StylePrimary(btnAddLine);
            btnAddLine.Click += BtnAddLine_Click;
            grpLine.Controls.Add(btnAddLine);

            btnRemoveLine = new Button { Text = "Remove", Location = new Point(735, 22), Width = 75, Height = 28 };
            UiTheme.StyleSecondary(btnRemoveLine);
            btnRemoveLine.Click += BtnRemoveLine_Click;
            grpLine.Controls.Add(btnRemoveLine);

            gridLines = new DataGridView
            {
                Location = new Point(10, 60), Size = new Size(800, 250),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridLines.Columns.Add("ItemId",   "Item ID");
            gridLines.Columns.Add("ItemName", "Item Name");
            gridLines.Columns.Add("Qty",      "Qty");
            gridLines.Columns.Add("Price",    "Unit Price");
            gridLines.Columns.Add("Total",    "Subtotal");
            grpLine.Controls.Add(gridLines);

            lblTotal = new Label
            {
                Text      = "Total: HKD 0.00", Location = new Point(600, 325), AutoSize = true,
                Font      = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                ForeColor = UiTheme.Primary
            };
            grpLine.Controls.Add(lblTotal);

            btnSave = new Button { Text = "Save", Location = new Point(20, 615), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnSave);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            AcceptButton = btnSave;

            btnCancel = new Button { Text = "Close", Location = new Point(128, 615), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void BindData()
        {
            txtPoId.Text = working.PoId;
            dtpOrder.Value = working.OrderDate;
            dtpExpected.Value = working.ExpectedDelivery;
            for (int i = 0; i < cmbSupplier.Items.Count; i++)
                if (((ListItem)cmbSupplier.Items[i]).Value == working.SupplierId)
                { cmbSupplier.SelectedIndex = i; break; }
            cmbStatus.SelectedItem = working.Status ?? "Draft";
            for (int i = 0; i < cmbLinkedRmr.Items.Count; i++)
                if (((ListItem)cmbLinkedRmr.Items[i]).Value == (working.LinkedRmrId ?? ""))
                { cmbLinkedRmr.SelectedIndex = i; break; }
            txtRemarks.Text = working.Remarks ?? "";
            RefreshLines();
        }

        private void UpdateReadOnlyState()
        {
            if (!ReadOnlyMode) return;
            dtpOrder.Enabled = dtpExpected.Enabled = cmbSupplier.Enabled = cmbStatus.Enabled = cmbLinkedRmr.Enabled = false;
            txtRemarks.ReadOnly = true;
            cmbItem.Enabled = numQty.Enabled = false;
            txtUnitPrice.ReadOnly = true;
            btnAddLine.Enabled = btnRemoveLine.Enabled = btnImportRmr.Enabled = false;
            btnSave.Visible = false;
            btnCancel.Text = "Close";
            Text = "View PO - " + working.PoId;
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
            decimal up;
            if (!decimal.TryParse(txtUnitPrice.Text, out up) || up < 0)
            { UiTheme.ShowWarning(this, "Please enter a valid unit price."); return; }
            int qty = (int)numQty.Value;
            var existing = working.Lines.FirstOrDefault(l => l.ItemId == itemId);
            if (existing != null) { existing.Quantity += qty; existing.UnitPrice = up; }
            else working.Lines.Add(new ProcurementLine
            {
                ItemId = item.ItemId, ItemName = item.ItemName,
                Quantity = qty, UnitPrice = up
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

        private void BtnImportRmr_Click(object sender, EventArgs e)
        {
            if (cmbLinkedRmr.SelectedItem == null) return;
            string rmrId = ((ListItem)cmbLinkedRmr.SelectedItem).Value;
            if (string.IsNullOrEmpty(rmrId)) { UiTheme.ShowWarning(this, "Please select an RMR to import from."); return; }
            var rmr = DataStore.RawMaterialRequests.FirstOrDefault(r => r.RmrId == rmrId);
            if (rmr == null) return;
            int imported = 0;
            foreach (var rl in rmr.Lines)
            {
                if (working.Lines.Any(l => l.ItemId == rl.ItemId)) continue;
                var item = DataStore.Items.FirstOrDefault(i => i.ItemId == rl.ItemId);
                working.Lines.Add(new ProcurementLine
                {
                    ItemId = rl.ItemId, ItemName = rl.ItemName,
                    Quantity = rl.QtyNeeded,
                    UnitPrice = item != null ? item.UnitPrice : 0m
                });
                imported++;
            }
            RefreshLines();
            UiTheme.ShowInfo(this, "Imported " + imported + " item(s) from " + rmrId + ".");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbSupplier.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select a supplier."); return; }
            if (working.Lines.Count == 0) { UiTheme.ShowWarning(this, "PO must have at least one line."); return; }
            if (dtpExpected.Value.Date < dtpOrder.Value.Date)
            { UiTheme.ShowWarning(this, "Expected delivery cannot be earlier than the order date."); return; }

            working.SupplierId       = ((ListItem)cmbSupplier.SelectedItem).Value;
            working.OrderDate        = dtpOrder.Value.Date;
            working.ExpectedDelivery = dtpExpected.Value.Date;
            working.Status           = cmbStatus.SelectedItem as string ?? "Draft";
            working.LinkedRmrId      = cmbLinkedRmr.SelectedItem != null ? ((ListItem)cmbLinkedRmr.SelectedItem).Value : "";
            working.Remarks          = txtRemarks.Text.Trim();

            bool added = false;
            if (original == null)
            {
                DataStore.Procurements.Add(working);
                added = true;
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Create PO", working.PoId);
            }
            else
            {
                original.SupplierId       = working.SupplierId;
                original.OrderDate        = working.OrderDate;
                original.ExpectedDelivery = working.ExpectedDelivery;
                original.Status           = working.Status;
                original.LinkedRmrId      = working.LinkedRmrId;
                original.Remarks          = working.Remarks;
                original.Lines.Clear();
                foreach (var l in working.Lines)
                    original.Lines.Add(new ProcurementLine
                    {
                        ItemId = l.ItemId, ItemName = l.ItemName,
                        Quantity = l.Quantity, UnitPrice = l.UnitPrice
                    });
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit PO", working.PoId);
            }

            // If a RMR is linked and status is Sent/Completed, mark the RMR as Procured
            if (!string.IsNullOrEmpty(working.LinkedRmrId) &&
                (working.Status == "Sent" || working.Status == "Completed" || working.Status == "PartiallyReceived"))
            {
                var rmr = DataStore.RawMaterialRequests.FirstOrDefault(r => r.RmrId == working.LinkedRmrId);
                if (rmr != null && rmr.Status != "Procured")
                {
                    rmr.Status = "Procured";
                    SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                        "Auto-update RMR", rmr.RmrId + " -> Procured (via " + working.PoId + ")");
                }
            }

            try { DataStore.SaveAll(); }
            catch (Exception ex)
            {
                if (added) DataStore.Procurements.Remove(working);
                UiTheme.ShowWarning(this, "Database error while saving PO:\n" + ex.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
