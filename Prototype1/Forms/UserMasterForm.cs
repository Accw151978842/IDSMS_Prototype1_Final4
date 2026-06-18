using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class UserMasterForm : Form
    {
        private DataGridView grid;

        public UserMasterForm()
        {
            Text = "Security - User Accounts";
            ClientSize = new Size(900, 540);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            var top = UiTheme.BuildToolbar(64);
            var lblTitle = UiTheme.BuildHeading("User Accounts");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Manage system users and access roles.", Location = new Point(0, 38), AutoSize = true, ForeColor = UiTheme.TextMuted });

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
            grid.DoubleClick += (s, e) => DoEdit();
            body.Controls.Add(grid);
            Controls.Add(body);

            var bottom = UiTheme.BuildBottomBar(64);
            var btnNew = new Button { Text = "New", Width = 90, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            var btnEdit = new Button { Text = "Edit", Width = 90, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            var btnReset = new Button { Text = "Reset Password", Width = 140, Height = 34 };
            btnReset.Click += (s, e) => DoReset();
            var btnToggle = new Button { Text = "Activate / Disable", Width = 150, Height = 34 };
            btnToggle.Click += (s, e) => DoToggle();
            var btnAudit = new Button { Text = "View Audit Log", Width = 130, Height = 34 };
            btnAudit.Click += (s, e) => { using (var f = new AuditLogForm()) f.ShowDialog(this); };
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnReset, btnToggle, btnAudit, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleAccent(btnReset);
            UiTheme.StyleSecondary(btnToggle);
            UiTheme.StyleSecondary(btnAudit);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnReset, btnToggle, btnAudit, btnClose);
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
            grid.Columns.Add("Id", "User ID");
            grid.Columns.Add("User", "Username");
            grid.Columns.Add("Name", "Full Name");
            grid.Columns.Add("Role", "Role");
            grid.Columns.Add("Manager", "Manager");
            grid.Columns.Add("Staff", "Linked Staff");
            grid.Columns.Add("Active", "Active");
            foreach (var u in DataStore.Users)
            {
                string staffDisplay = "";
                if (!string.IsNullOrEmpty(u.StaffId))
                {
                    var st = DataStore.StaffList.FirstOrDefault(x => x.StaffId == u.StaffId);
                    staffDisplay = st != null ? (st.StaffId + " - " + st.FullName) : u.StaffId;
                }
                grid.Rows.Add(u.UserId, u.Username, u.FullName, u.Role, u.IsManager ? "Yes" : "", staffDisplay, u.Active ? "Yes" : "No");
            }
        }

        private User Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Users.FirstOrDefault(u => u.UserId == id);
        }

        private void DoNew()
        {
            using (var f = new UserEditForm(null)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoEdit()
        {
            var u = Selected();
            if (u == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            using (var f = new UserEditForm(u)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoReset()
        {
            var u = Selected();
            if (u == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            string newPwd = InputPrompt.Show(this, "Enter a new password for " + u.Username + ":", "Reset Password", "password");
            if (string.IsNullOrEmpty(newPwd) || newPwd.Length < 5) { UiTheme.ShowWarning(this, "Password must be at least 5 characters."); return; }
            u.PasswordHash = SecurityService.Hash(newPwd);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Reset Password", u.Username);
            DataStore.SaveAll();
            UiTheme.ShowInfo(this, "Password has been reset.");
        }

        private void DoToggle()
        {
            var u = Selected();
            if (u == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            if (SecurityService.CurrentUser != null && u.UserId == SecurityService.CurrentUser.UserId)
            {
                UiTheme.ShowWarning(this, "You cannot disable your own account.");
                return;
            }
            u.Active = !u.Active;
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Toggle Active", u.Username + " -> " + u.Active);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    public class UserEditForm : Form
    {
        private readonly User original;
        private TextBox txtId, txtUser, txtName, txtPwd;
        private ComboBox cmbRole, cmbStaff;
        private CheckBox chkActive, chkManager;
        private Label lblPwd;

        public UserEditForm(User u)
        {
            original = u;
            Text = u == null ? "New User" : "Edit User - " + u.Username;
            ClientSize = new Size(560, 500);
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
            // ---- HEADER ----
            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text      = original == null ? "New User" : "Edit User",
                ForeColor = Color.White,
                Font      = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                Location  = new Point(20, 14),
                AutoSize  = true
            });

            // ---- BODY (build but DO NOT add yet) ----
            var body = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Background, Padding = new Padding(0) };

            int y = 20;
            body.Controls.Add(new Label { Text = "User ID:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            body.Controls.Add(txtId);
            y += 35;
            body.Controls.Add(new Label { Text = "Username:", Location = new Point(20, y + 3), AutoSize = true });
            // Pre-mark username as ReadOnly in EDIT mode (original != null).
            // Username is the primary key referenced by audit_logs - must not change.
            txtUser = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = (original != null) };
            body.Controls.Add(txtUser);
            y += 35;
            body.Controls.Add(new Label { Text = "Full Name:", Location = new Point(20, y + 3), AutoSize = true });
            txtName = new TextBox { Location = new Point(140, y), Width = 260 };
            body.Controls.Add(txtName);
            y += 35;
            body.Controls.Add(new Label { Text = "Role:", Location = new Point(20, y + 3), AutoSize = true });
            cmbRole = new ComboBox { Location = new Point(140, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new object[] { "Administrator", "Sales", "Logistics", "Warehouse", "Service" });
            body.Controls.Add(cmbRole);
            y += 35;
            // ---- Staff link: which employee this login belongs to ----
            body.Controls.Add(new Label { Text = "Staff:", Location = new Point(20, y + 3), AutoSize = true });
            cmbStaff = new ComboBox { Location = new Point(140, y), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStaff.DropDownWidth = 360;
            cmbStaff.Items.Add(new ListItem("", "(none)"));
            foreach (var s in DataStore.StaffList)
                cmbStaff.Items.Add(new ListItem(s.StaffId,
                    s.StaffId + " - " + s.FullName + " (" + s.Position + ", " + s.Department + ")"));
            cmbStaff.DisplayMember = "Display";
            // Picking a staff auto-fills Full Name from the master record.
            cmbStaff.SelectedIndexChanged += (s, e) =>
            {
                var sel = cmbStaff.SelectedItem as ListItem;
                if (sel != null && !string.IsNullOrEmpty(sel.Value))
                {
                    var st = DataStore.StaffList.FirstOrDefault(x => x.StaffId == sel.Value);
                    if (st != null) txtName.Text = st.FullName;
                }
            };
            body.Controls.Add(cmbStaff);
            y += 35;
            // Password row: label spans full row, TextBox on next line if label is long
            lblPwd = new Label { Text = "Password:", Location = new Point(20, y + 3), AutoSize = true };
            body.Controls.Add(lblPwd);
            txtPwd = new TextBox { Location = new Point(140, y), Width = 200, UseSystemPasswordChar = true };
            body.Controls.Add(txtPwd);
            // (lblPwd may grow when text is set to 'New Password (leave blank to keep):'
            //  - we re-layout in Load_ to push the textbox to the next line in that case.)
            y += 35;
            chkManager = new CheckBox { Text = "Department Manager (approval / reports / pricing rights)", Location = new Point(140, y), AutoSize = true };
            body.Controls.Add(chkManager);

            y += 30;
            chkActive = new CheckBox { Text = "Active", Location = new Point(140, y), AutoSize = true };
            body.Controls.Add(chkActive);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 50), Width = 100, Height = 34 };
            btnSave.Click += BtnSave_Click;
            UiTheme.StylePrimary(btnSave);
            body.Controls.Add(btnSave);
            AcceptButton = btnSave;
            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 50), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            body.Controls.Add(btnCancel);
            CancelButton = btnCancel;

            // ---- Add to form in correct Z-order: Fill FIRST, then Top ----
            Controls.Add(body);                                          // Fill
            Controls.Add(header);                                        // Top
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("U", DataStore.Users.Select(u => u.UserId));
                cmbRole.SelectedIndex = 1;
                if (cmbStaff.Items.Count > 0) cmbStaff.SelectedIndex = 0;   // (none)
                chkActive.Checked = true;
                chkManager.Checked = false;
                lblPwd.Text = "Password:";
            }
            else
            {
                txtId.Text = original.UserId;
                txtUser.Text = original.Username;
                // ReadOnly + gray bg already applied in BuildUI/ApplyInputs.
                txtName.Text = original.FullName;
                cmbRole.SelectedItem = original.Role;
                chkActive.Checked = original.Active;
                chkManager.Checked = original.IsManager;
                // Re-select the linked staff (or '(none)')
                cmbStaff.SelectedIndex = 0;
                for (int i = 0; i < cmbStaff.Items.Count; i++)
                {
                    if (((ListItem)cmbStaff.Items[i]).Value == (original.StaffId ?? ""))
                    {
                        cmbStaff.SelectedIndex = i;
                        break;
                    }
                }
                lblPwd.Text = "New Password:";
                lblPwd.AutoSize = true;
                // Hint text BELOW the input field; push Active and buttons down
                // so they don't overlap the hint.
                var lblHint = new Label
                {
                    Text      = "Leave blank to keep current password.",
                    Location  = new Point(txtPwd.Left, txtPwd.Bottom + 4),
                    AutoSize  = true,
                    ForeColor = UiTheme.TextMuted,
                    Font      = new Font(UiTheme.FontFamily, 8.5F, FontStyle.Italic)
                };
                txtPwd.Parent.Controls.Add(lblHint);

                // Shift Manager checkbox + Active checkbox + Save/Cancel buttons
                // down to make room for the hint line under the password box.
                int shift = 22;
                chkManager.Top += shift;
                chkActive.Top += shift;
                foreach (Control c in chkActive.Parent.Controls)
                {
                    if (c is Button) c.Top += shift;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var v = new Validation(this);
            v.Required(txtUser, "Username required.")
             .Regex(txtUser, Validation.RxUsername, "Username: 3-20 letters/digits/underscore.")
             .Required(txtName, "Full name required.")
             .Selected(cmbRole, "Please select a role.");
            if (original == null)
            {
                v.Required(txtPwd, "Password required.")
                 .MinLength(txtPwd, 5, "Password must be at least 5 characters.");
            }
            if (!v.ValidateAll()) return;

            // Resolve the chosen staff link (may be empty = not linked).
            string staffId = (cmbStaff.SelectedItem as ListItem)?.Value ?? "";
            // One staff member should map to at most one login account.
            if (!string.IsNullOrEmpty(staffId))
            {
                bool takenByOther = DataStore.Users.Any(x =>
                    string.Equals(x.StaffId, staffId, StringComparison.OrdinalIgnoreCase) &&
                    (original == null || !string.Equals(x.UserId, original.UserId, StringComparison.OrdinalIgnoreCase)));
                if (takenByOther)
                {
                    UiTheme.ShowWarning(this, "That staff member is already linked to another account.");
                    return;
                }
            }

            if (original == null)
            {
                if (DataStore.Users.Any(x => string.Equals(x.Username, txtUser.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    UiTheme.ShowWarning(this, "Username already exists.");
                    return;
                }
                var u = new User
                {
                    UserId = txtId.Text,
                    Username = txtUser.Text.Trim(),
                    FullName = txtName.Text.Trim(),
                    Role = cmbRole.SelectedItem as string ?? "Sales",
                    PasswordHash = SecurityService.Hash(txtPwd.Text),
                    Active = chkActive.Checked,
                    StaffId = staffId,
                    IsManager = chkManager.Checked
                };
                DataStore.Users.Add(u);
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "New User", u.Username);
            }
            else
            {
                original.FullName = txtName.Text.Trim();
                original.Role = cmbRole.SelectedItem as string ?? original.Role;
                original.Active = chkActive.Checked;
                original.StaffId = staffId;
                original.IsManager = chkManager.Checked;
                if (!string.IsNullOrEmpty(txtPwd.Text))
                {
                    if (txtPwd.Text.Length < 5) { UiTheme.ShowWarning(this, "Password must be at least 5 characters."); return; }
                    original.PasswordHash = SecurityService.Hash(txtPwd.Text);
                }
                SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Update User", original.Username);
            }
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
