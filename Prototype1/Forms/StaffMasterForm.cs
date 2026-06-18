using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class StaffMasterForm : Form
    {
        private DataGridView grid;
        private TextBox txtSearch;

        public StaffMasterForm()
        {
            Text = "Master Data - Staff";
            ClientSize = new Size(940, 540);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // 1. TOP
            var top = UiTheme.BuildToolbar(64);
            var lblTitle = UiTheme.BuildHeading("Staff");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Search:", Location = new Point(0, 38), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(56, 35), Width = 280, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            // 2. BOTTOM

            var bottom = UiTheme.BuildBottomBar(64);
            var btnNew = new Button { Text = "New", Width = 90, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            var btnEdit = new Button { Text = "Edit", Width = 90, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            var btnDel = new Button { Text = "Delete", Width = 90, Height = 34 };
            btnDel.Click += (s, e) => DoDelete();
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDel, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleDanger(btnDel);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnDel, btnClose);

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
            grid.Columns.Add("Id", "Staff ID");
            grid.Columns.Add("Name", "Full Name");
            grid.Columns.Add("Pos", "Position");
            grid.Columns.Add("Dept", "Department");
            grid.Columns.Add("Phone", "Phone");
            grid.Columns.Add("Email", "Email");
            grid.Columns.Add("Hire", "Hire Date");
            string filter = txtSearch.Text.Trim().ToLower();
            foreach (var s in DataStore.StaffList)
            {
                if (filter.Length > 0)
                {
                    string hay = (s.StaffId + " " + s.FullName + " " + s.Department + " " + s.Position).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(s.StaffId, s.FullName, s.Position, s.Department, s.Phone, s.Email, s.HireDate.ToString("yyyy-MM-dd"));
            }
        }

        private Staff Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.StaffList.FirstOrDefault(s => s.StaffId == id);
        }

        private void DoNew()
        {
            using (var f = new StaffEditForm(null)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoEdit()
        {
            var s = Selected();
            if (s == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            using (var f = new StaffEditForm(s)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoDelete()
        {
            if (!SecurityService.IsManager)
            { UiTheme.ShowWarning(this, "Only a department manager can delete records."); return; }
            var s = Selected();
            if (s == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            if (!UiTheme.ShowConfirm(this, "Delete " + s.FullName + "?")) return;
            DataStore.StaffList.Remove(s);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Delete Staff", s.StaffId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    public class StaffEditForm : Form
    {
        private readonly Staff original;
        private TextBox txtId, txtName, txtPos, txtPhone, txtEmail;
        private ComboBox cmbDept;
        private DateTimePicker dtpHire;

        // Default suggestions - aligned with Case Study (Section 7) official departments
        // plus Administration. Users can still type a new department.
        private static readonly string[] DefaultDepartments =
            { "Sales and Marketing", "Furniture Design", "Production",
              "Inventory Control", "Logistics", "Finance", "IT", "Administration" };

        public StaffEditForm(Staff s)
        {
            original = s;
            Text = s == null ? "New Staff" : "Edit Staff - " + s.StaffId;
            ClientSize = new Size(490, 430);
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
            header.Controls.Add(new Label { Text = original == null ? "New Staff" : "Edit Staff", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Staff ID:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            Controls.Add(txtId);
            y += 35;
            Controls.Add(new Label { Text = "Full Name:", Location = new Point(20, y + 3), AutoSize = true });
            txtName = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtName);
            y += 35;
            Controls.Add(new Label { Text = "Position:", Location = new Point(20, y + 3), AutoSize = true });
            txtPos = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtPos);
            y += 35;
            Controls.Add(new Label { Text = "Department:", Location = new Point(20, y + 3), AutoSize = true });
            cmbDept = new ComboBox
            {
                Location = new Point(140, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var d in BuildDepartmentOptions()) cmbDept.Items.Add(d);
            UiTheme.StyleComboBox(cmbDept);
            Controls.Add(cmbDept);
            y += 35;
            Controls.Add(new Label { Text = "Phone:", Location = new Point(20, y + 3), AutoSize = true });
            txtPhone = new TextBox { Location = new Point(140, y), Width = 200 };
            txtPhone.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && "+- ".IndexOf(e.KeyChar) < 0) e.Handled = true; };
            Controls.Add(txtPhone);
            y += 35;
            Controls.Add(new Label { Text = "Email:", Location = new Point(20, y + 3), AutoSize = true });
            txtEmail = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtEmail);
            y += 35;
            Controls.Add(new Label { Text = "Hire Date:", Location = new Point(20, y + 3), AutoSize = true });
            dtpHire = new DateTimePicker { Location = new Point(140, y), Width = 200, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            Controls.Add(dtpHire);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 55), Width = 100, Height = 34 };
            btnSave.Click += BtnSave_Click;
            UiTheme.StylePrimary(btnSave);
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 55), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("S", DataStore.StaffList.Select(s => s.StaffId));
                dtpHire.Value = DateTime.Today;
            }
            else
            {
                txtId.Text = original.StaffId;
                txtName.Text = original.FullName;
                txtPos.Text = original.Position;
                cmbDept.Text = original.Department;
                txtPhone.Text = original.Phone;
                txtEmail.Text = original.Email;
                dtpHire.Value = original.HireDate == DateTime.MinValue ? DateTime.Today : original.HireDate;
            }
        }

        // Combine defaults with existing data so nothing is lost.
        private static IEnumerable<string> BuildDepartmentOptions()
        {
            var existing = DataStore.StaffList
                .Select(s => s.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d.Trim());
            return DefaultDepartments.Concat(existing)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var v = new Validation(this);
            v.Required(txtName, "Full name is required.")
             .Regex(txtName, Validation.RxLettersOnly, "Name: 2-50 letters/Chinese only.")
             .Required(txtPhone, "Phone is required.")
             .Phone(txtPhone)
             .Required(txtEmail, "Email is required.")
             .Email(txtEmail)
             .Required(cmbDept, "Department required.");
            if (!v.ValidateAll()) return;
            var t = original;
            if (t == null)
            {
                t = new Staff { StaffId = txtId.Text };
                DataStore.StaffList.Add(t);
            }
            t.FullName = txtName.Text.Trim();
            t.Position = txtPos.Text.Trim();
            t.Department = cmbDept.Text.Trim();
            t.Phone = txtPhone.Text.Trim();
            t.Email = txtEmail.Text.Trim();
            t.HireDate = dtpHire.Value.Date;
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                original == null ? "New Staff" : "Update Staff", t.StaffId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
