using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    // ============================================================
    //  Raw Material Request - LIST FORM (with Search filter)
    // ============================================================
    public class RawMaterialRequestForm : Form
    {
        private DataGridView grid;
        private Button btnNew, btnEdit, btnView, btnApprove, btnReject, btnClose, btnRefresh;
        private TextBox txtSearch;
        private ComboBox cmbStatus;
        private CheckBox chkDateFilter;
        private DateTimePicker dtpFrom, dtpTo;

        public RawMaterialRequestForm()
        {
            Text = "Raw Material Requests";
            ClientSize = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // ── FILL body (must be added FIRST so dock order works) ──
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

            // ── TOP toolbar ──
            var top = UiTheme.BuildToolbar(72);

            var lblTitle = UiTheme.BuildHeading("Raw Material Requests");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);

            top.Controls.Add(new Label { Text = "Search:", Location = new Point(0, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(56, 39), Width = 230, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Status:", Location = new Point(305, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });
            cmbStatus = new ComboBox { Location = new Point(355, 39), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "(All)", "Pending", "Approved", "Rejected", "Procured" });
            cmbStatus.SelectedIndex = 0;
            UiTheme.StyleComboBox(cmbStatus);
            cmbStatus.SelectedIndexChanged += (s, e) => LoadGrid();
            top.Controls.Add(cmbStatus);

            chkDateFilter = new CheckBox { Text = "Date range:", Location = new Point(525, 41), AutoSize = true, ForeColor = UiTheme.TextMuted };
            chkDateFilter.CheckedChanged += (s, e) => { dtpFrom.Enabled = dtpTo.Enabled = chkDateFilter.Checked; LoadGrid(); };
            top.Controls.Add(chkDateFilter);

            dtpFrom = new DateTimePicker { Location = new Point(625, 38), Width = 110, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Enabled = false, Value = DateTime.Today.AddMonths(-1) };
            dtpFrom.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadGrid(); };
            top.Controls.Add(dtpFrom);

            top.Controls.Add(new Label { Text = "to", Location = new Point(740, 42), AutoSize = true, ForeColor = UiTheme.TextMuted });

            dtpTo = new DateTimePicker { Location = new Point(760, 38), Width = 110, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Enabled = false, Value = DateTime.Today };
            dtpTo.ValueChanged += (s, e) => { if (chkDateFilter.Checked) LoadGrid(); };
            top.Controls.Add(dtpTo);

            btnRefresh = new Button { Text = "Refresh", Location = new Point(880, 37), Width = 80, Height = 28 };
            UiTheme.StyleSecondary(btnRefresh);
            btnRefresh.Click += (s, e) => LoadGrid();
            top.Controls.Add(btnRefresh);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(top);

            // ── BOTTOM bar ──
            var bottom = UiTheme.BuildBottomBar(64);
            btnNew     = new Button { Text = "New Request",   Width = 120, Height = 34 };
            btnEdit    = new Button { Text = "Edit",          Width =  90, Height = 34 };
            btnView    = new Button { Text = "View",          Width =  90, Height = 34 };
            btnApprove = new Button { Text = "Approve",       Width = 100, Height = 34 };
            btnReject  = new Button { Text = "Reject",        Width =  90, Height = 34 };
            btnClose   = new Button { Text = "Close",         Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            btnNew.Click     += (s, e) => DoNew();
            btnEdit.Click    += (s, e) => DoEdit();
            btnView.Click    += (s, e) => DoView();
            btnApprove.Click += (s, e) => DoSetStatus("Approved");
            btnReject.Click  += (s, e) => DoSetStatus("Rejected");
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnView, btnApprove, btnReject, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleSecondary(btnView);
            UiTheme.StyleSuccess(btnApprove);
            UiTheme.StyleDangerOutlined(btnReject);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnView, btnApprove, btnReject, btnClose);

            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(bottom);
            CancelButton = btnClose;
        }

        private void LoadGrid()
        {
            grid.Columns.Clear();
            UiTheme.ApplyGrid(grid);
            UiTheme.EnableStatusColumn(grid);

            grid.Columns.Add("RmrId",       "RMR No.");
            grid.Columns.Add("RequestDate", "Request Date");
            grid.Columns.Add("Department",  "Department");
            grid.Columns.Add("RequestedBy", "Requested By");
            grid.Columns.Add("Status",      "Status");
            grid.Columns.Add("Lines",       "Lines");
            grid.Columns.Add("TotalQty",    "Total Qty");

            UiTheme.AlignNumericColumns(grid);

            string filter = txtSearch.Text.Trim().ToLower();
            string status = cmbStatus.SelectedItem as string;
            bool useDate = chkDateFilter != null && chkDateFilter.Checked;
            DateTime from = dtpFrom != null ? dtpFrom.Value.Date : DateTime.MinValue;
            DateTime to   = dtpTo   != null ? dtpTo.Value.Date   : DateTime.MaxValue;

            int shown = 0;
            foreach (var r in DataStore.RawMaterialRequests)
            {
                var st = DataStore.StaffList.FirstOrDefault(s => s.StaffId == r.RequestedBy);
                string byName = st != null ? st.FullName : (r.RequestedBy ?? "");
                if (status != null && status != "(All)" && r.Status != status) continue;
                if (useDate && (r.RequestDate.Date < from || r.RequestDate.Date > to)) continue;
                if (filter.Length > 0)
                {
                    string hay = (r.RmrId + " " + r.Department + " " + byName + " " + r.Status + " " + (r.Notes ?? "")).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(r.RmrId, r.RequestDate.ToString("yyyy-MM-dd"), r.Department,
                    byName, r.Status, r.Lines.Count, r.TotalQty);
                shown++;
            }
            Text = "Raw Material Requests - " + shown + " record(s)";
        }

        private RawMaterialRequest Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.RawMaterialRequests.FirstOrDefault(r => r.RmrId == id);
        }

        private void DoNew()
        {
            using (var f = new RmrEditForm(null))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoEdit()
        {
            var r = Selected();
            if (r == null) { UiTheme.ShowWarning(this, "Please select a request first."); return; }
            if (r.Status == "Approved" || r.Status == "Rejected" || r.Status == "Procured")
            { UiTheme.ShowWarning(this, "Approved / Rejected / Procured requests cannot be edited."); return; }
            using (var f = new RmrEditForm(r))
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
        }

        private void DoView()
        {
            var r = Selected();
            if (r == null) { UiTheme.ShowWarning(this, "Please select a request first."); return; }
            using (var f = new RmrEditForm(r) { ReadOnlyMode = true })
                f.ShowDialog(this);
        }

        private void DoSetStatus(string newStatus)
        {
            if (!SecurityService.IsManager)
            { UiTheme.ShowWarning(this, "Only a department manager can approve or reject material requests."); return; }
            var r = Selected();
            if (r == null) { UiTheme.ShowWarning(this, "Please select a request first."); return; }
            if (r.Status != "Pending")
            { UiTheme.ShowWarning(this, "Only Pending requests can be " + newStatus.ToLower() + "."); return; }
            if (!UiTheme.ShowConfirm(this, newStatus + " request " + r.RmrId + "?")) return;
            r.Status = newStatus;
            SecurityService.Audit(
                SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                newStatus + " RMR", r.RmrId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    // ============================================================
    //  RMR EDIT FORM (Create / Edit / View)
    // ============================================================
    public class RmrEditForm : Form
    {
        private readonly RawMaterialRequest original;
        private readonly RawMaterialRequest working;
        public bool ReadOnlyMode { get; set; }

        private TextBox txtRmrId, txtNotes;
        private DateTimePicker dtpDate;
        private ComboBox cmbDepartment, cmbStaff, cmbStatus;
        private DataGridView gridLines;
        private ComboBox cmbItem;
        private NumericUpDown numQty;
        private TextBox txtLineNote;
        private Button btnAddLine, btnRemoveLine, btnSave, btnCancel;
        private Label lblTotal;

        public RmrEditForm(RawMaterialRequest existing)
        {
            original = existing;
            working = new RawMaterialRequest();
            if (existing != null)
            {
                working.RmrId = existing.RmrId;
                working.RequestDate = existing.RequestDate;
                working.RequestedBy = existing.RequestedBy;
                working.Department = existing.Department;
                working.Status = existing.Status;
                working.Notes = existing.Notes;
                foreach (var ln in existing.Lines)
                    working.Lines.Add(new RmrLine
                    {
                        ItemId = ln.ItemId, ItemName = ln.ItemName,
                        QtyNeeded = ln.QtyNeeded, Notes = ln.Notes
                    });
            }
            else
            {
                working.RmrId = DataStore.NextId("RMR", DataStore.RawMaterialRequests.Select(r => r.RmrId));
                working.RequestDate = DateTime.Today;
                working.Status = "Pending";
                working.Department = "Production";
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
            Text = original == null ? "Create Raw Material Request" : "Edit RMR - " + working.RmrId;
            ClientSize = new Size(820, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            UiTheme.ApplyForm(this);

            var header = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text      = original == null ? "Create Raw Material Request" : "Edit Raw Material Request",
                ForeColor = Color.White,
                Font      = new Font(UiTheme.FontFamily, 10F, FontStyle.Bold),
                Location  = new Point(20, 12), AutoSize = true
            });
            Controls.Add(header);

            int y = 68;
            Controls.Add(new Label { Text = "RMR No.:", Location = new Point(20, y + 3), AutoSize = true });
            txtRmrId = new TextBox { Location = new Point(110, y), Width = 140, ReadOnly = true };
            Controls.Add(txtRmrId);

            Controls.Add(new Label { Text = "Request Date:", Location = new Point(280, y + 3), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(385, y), Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpDate);

            Controls.Add(new Label { Text = "Status:", Location = new Point(540, y + 3), AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(620, y), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "Pending", "Approved", "Rejected", "Procured" });
            Controls.Add(cmbStatus);

            y += 40;
            Controls.Add(new Label { Text = "Department:", Location = new Point(20, y + 3), AutoSize = true });
            cmbDepartment = new ComboBox { Location = new Point(110, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbDepartment.Items.AddRange(new object[] { "Production", "Design", "Warehouse", "Administration" });
            Controls.Add(cmbDepartment);

            Controls.Add(new Label { Text = "Requested By:", Location = new Point(305, y + 3), AutoSize = true });
            cmbStaff = new ComboBox { Location = new Point(405, y), Width = 355, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var s in DataStore.StaffList)
                cmbStaff.Items.Add(new ListItem(s.StaffId, s.StaffId + " - " + s.FullName + " (" + s.Department + ")"));
            cmbStaff.DisplayMember = "Display";
            Controls.Add(cmbStaff);

            y += 40;
            Controls.Add(new Label { Text = "Notes:", Location = new Point(20, y + 3), AutoSize = true });
            txtNotes = new TextBox { Location = new Point(110, y), Width = 650 };
            Controls.Add(txtNotes);

            y += 40;
            var grpLine = new GroupBox
            {
                Text = "Required Items", Location = new Point(15, y), Size = new Size(785, 380),
                ForeColor = UiTheme.TextPrimary,
                Font = new Font(UiTheme.FontFamily, 9.5F, FontStyle.Bold)
            };
            Controls.Add(grpLine);

            grpLine.Controls.Add(new Label { Text = "Item:", Location = new Point(10, 28), AutoSize = true });
            cmbItem = new ComboBox { Location = new Point(60, 25), Width = 290, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbItem.DropDownWidth = 480;   // wider dropdown so long item text is not clipped
            foreach (var it in DataStore.Items)
                cmbItem.Items.Add(new ListItem(it.ItemId, it.ItemId + " - " + it.ItemName + " (Stock: " + it.StockQty + ")"));
            cmbItem.DisplayMember = "Display";
            grpLine.Controls.Add(cmbItem);

            grpLine.Controls.Add(new Label { Text = "Qty:", Location = new Point(360, 28), AutoSize = true });
            numQty = new NumericUpDown { Location = new Point(395, 25), Width = 70, Minimum = 1, Maximum = 9999, Value = 1 };
            grpLine.Controls.Add(numQty);

            grpLine.Controls.Add(new Label { Text = "Note:", Location = new Point(475, 28), AutoSize = true });
            txtLineNote = new TextBox { Location = new Point(515, 25), Width = 110 };
            grpLine.Controls.Add(txtLineNote);

            btnAddLine = new Button { Text = "Add", Location = new Point(635, 22), Width = 60, Height = 28 };
            UiTheme.StylePrimary(btnAddLine);
            btnAddLine.Click += BtnAddLine_Click;
            grpLine.Controls.Add(btnAddLine);

            btnRemoveLine = new Button { Text = "Remove", Location = new Point(700, 22), Width = 75, Height = 28 };
            UiTheme.StyleSecondary(btnRemoveLine);
            btnRemoveLine.Click += BtnRemoveLine_Click;
            grpLine.Controls.Add(btnRemoveLine);

            gridLines = new DataGridView
            {
                Location = new Point(10, 60), Size = new Size(760, 250),
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridLines.Columns.Add("ItemId",   "Item ID");
            gridLines.Columns.Add("ItemName", "Item Name");
            gridLines.Columns.Add("Qty",      "Qty Needed");
            gridLines.Columns.Add("Note",     "Note");
            grpLine.Controls.Add(gridLines);

            lblTotal = new Label
            {
                Text      = "Total Qty: 0", Location = new Point(580, 325), AutoSize = true,
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
            txtRmrId.Text = working.RmrId;
            dtpDate.Value = working.RequestDate;
            cmbDepartment.SelectedItem = working.Department ?? "Production";
            for (int i = 0; i < cmbStaff.Items.Count; i++)
            {
                if (((ListItem)cmbStaff.Items[i]).Value == working.RequestedBy)
                { cmbStaff.SelectedIndex = i; break; }
            }
            cmbStatus.SelectedItem = working.Status ?? "Pending";
            txtNotes.Text = working.Notes ?? "";
            RefreshLines();
        }

        private void UpdateReadOnlyState()
        {
            if (!ReadOnlyMode) return;
            dtpDate.Enabled = cmbDepartment.Enabled = cmbStaff.Enabled = cmbStatus.Enabled = false;
            txtNotes.ReadOnly = true;
            cmbItem.Enabled = numQty.Enabled = txtLineNote.ReadOnly = true;
            txtLineNote.Enabled = false;
            btnAddLine.Enabled = btnRemoveLine.Enabled = false;
            btnSave.Visible = false;
            btnCancel.Text = "Close";
            Text = "View RMR - " + working.RmrId;
        }

        private void RefreshLines()
        {
            gridLines.Rows.Clear();
            foreach (var ln in working.Lines)
                gridLines.Rows.Add(ln.ItemId, ln.ItemName, ln.QtyNeeded, ln.Notes);
            lblTotal.Text = "Total Qty: " + working.TotalQty;
        }

        private void BtnAddLine_Click(object sender, EventArgs e)
        {
            if (cmbItem.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select an item."); return; }
            string itemId = ((ListItem)cmbItem.SelectedItem).Value;
            var item = DataStore.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) return;
            int qty = (int)numQty.Value;
            var existing = working.Lines.FirstOrDefault(l => l.ItemId == itemId);
            if (existing != null) existing.QtyNeeded += qty;
            else working.Lines.Add(new RmrLine
            {
                ItemId = item.ItemId, ItemName = item.ItemName,
                QtyNeeded = qty, Notes = txtLineNote.Text.Trim()
            });
            txtLineNote.Text = "";
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
            if (cmbStaff.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select the requesting staff."); return; }
            if (cmbDepartment.SelectedItem == null) { UiTheme.ShowWarning(this, "Please select a department."); return; }
            if (working.Lines.Count == 0) { UiTheme.ShowWarning(this, "Request must have at least one item."); return; }

            working.RequestedBy = ((ListItem)cmbStaff.SelectedItem).Value;
            working.Department  = cmbDepartment.SelectedItem as string;
            working.RequestDate = dtpDate.Value.Date;
            working.Status      = cmbStatus.SelectedItem as string ?? "Pending";
            working.Notes       = txtNotes.Text.Trim();

            bool added = false;
            if (original == null)
            {
                DataStore.RawMaterialRequests.Add(working);
                added = true;
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Create RMR", working.RmrId);
            }
            else
            {
                original.RequestDate = working.RequestDate;
                original.RequestedBy = working.RequestedBy;
                original.Department  = working.Department;
                original.Status      = working.Status;
                original.Notes       = working.Notes;
                original.Lines.Clear();
                foreach (var l in working.Lines)
                    original.Lines.Add(new RmrLine
                    {
                        ItemId = l.ItemId, ItemName = l.ItemName,
                        QtyNeeded = l.QtyNeeded, Notes = l.Notes
                    });
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Edit RMR", working.RmrId);
            }

            try { DataStore.SaveAll(); }
            catch (Exception ex)
            {
                if (added) DataStore.RawMaterialRequests.Remove(working);
                UiTheme.ShowWarning(this, "Database error while saving RMR:\n" + ex.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
