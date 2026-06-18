using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class CustomerMasterForm : Form
    {
        private DataGridView grid;
        private TextBox txtSearch;
        private Button btnNew, btnEdit, btnDelete, btnClose;

        public CustomerMasterForm()
        {
            Text = "Master Data - Customers";
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
            var lblTitle = UiTheme.BuildHeading("Customers");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            var lblSearch = new Label { Text = "Search:", Location = new Point(0, 38), AutoSize = true, ForeColor = UiTheme.TextMuted };
            top.Controls.Add(lblSearch);
            txtSearch = new TextBox { Location = new Point(56, 35), Width = 280, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

            // 2. BOTTOM

            var bottom = UiTheme.BuildBottomBar(64);
            btnNew = new Button { Text = "New", Width = 90, Height = 34 };
            btnNew.Click += (s, e) => DoNew();
            btnEdit = new Button { Text = "Edit", Width = 90, Height = 34 };
            btnEdit.Click += (s, e) => DoEdit();
            btnDelete = new Button { Text = "Delete", Width = 90, Height = 34 };
            btnDelete.Click += (s, e) => DoDelete();
            btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleDanger(btnDelete);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnDelete, btnClose);

            CancelButton = btnClose;

            // 3. FILL (must be last)
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
            grid.Columns.Add("Id", "Customer ID");
            grid.Columns.Add("Name", "Company");
            grid.Columns.Add("Contact", "Contact");
            grid.Columns.Add("Phone", "Phone");
            grid.Columns.Add("Email", "Email");
            grid.Columns.Add("Type", "Type");
            grid.Columns.Add("Address", "Address");

            string filter = txtSearch.Text.Trim().ToLower();
            foreach (var c in DataStore.Customers)
            {
                if (filter.Length > 0)
                {
                    string hay = (c.CustomerId + " " + c.CompanyName + " " + c.ContactPerson + " " + c.Phone).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(c.CustomerId, c.CompanyName, c.ContactPerson, c.Phone, c.Email, c.CustomerType, c.Address);
            }
        }

        private Customer Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Customers.FirstOrDefault(c => c.CustomerId == id);
        }

        private void DoNew()
        {
            using (var f = new CustomerEditForm(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoEdit()
        {
            var c = Selected();
            if (c == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            using (var f = new CustomerEditForm(c))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoDelete()
        {
            if (!SecurityService.IsManager)
            { UiTheme.ShowWarning(this, "Only a department manager can delete records."); return; }
            var c = Selected();
            if (c == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            if (DataStore.SalesOrders.Any(o => o.CustomerId == c.CustomerId))
            {
                UiTheme.ShowWarning(this, "Cannot delete: this customer has sales orders.");
                return;
            }
            if (!UiTheme.ShowConfirm(this, "Delete " + c.CompanyName + "?")) return;
            DataStore.Customers.Remove(c);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Delete Customer", c.CustomerId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    public class CustomerEditForm : Form
    {
        private readonly Customer original;
        private TextBox txtId, txtName, txtContact, txtPhone, txtEmail, txtAddress;
        private ComboBox cmbType;

        public CustomerEditForm(Customer c)
        {
            original = c;
            Text = c == null ? "New Customer" : "Edit Customer - " + c.CustomerId;
            ClientSize = new Size(480, 410);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            Load_();
        }

        private void BuildUI()
        {
            // ----- Build controls first (don't add to Form yet) -----
            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Primary };
            var lblH = new Label
            {
                Text = original == null ? "New Customer" : "Edit Customer",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                Location = new Point(20, 14),
                AutoSize = true
            };
            header.Controls.Add(lblH);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 18, 24, 18), BackColor = UiTheme.Surface };

            int y = 6;
            body.Controls.Add(MkLabel("Customer ID:", 0, y + 4));
            txtId = MkText(130, y, 200); txtId.ReadOnly = true; body.Controls.Add(txtId);

            y += 38;
            body.Controls.Add(MkLabel("Company:", 0, y + 4));
            txtName = MkText(130, y, 290); body.Controls.Add(txtName);

            y += 38;
            body.Controls.Add(MkLabel("Contact:", 0, y + 4));
            txtContact = MkText(130, y, 290); body.Controls.Add(txtContact);

            y += 38;
            body.Controls.Add(MkLabel("Phone:", 0, y + 4));
            txtPhone = MkText(130, y, 200);
            txtPhone.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && "+- ".IndexOf(e.KeyChar) < 0) e.Handled = true; };
            body.Controls.Add(txtPhone);

            y += 38;
            body.Controls.Add(MkLabel("Email:", 0, y + 4));
            txtEmail = MkText(130, y, 290); body.Controls.Add(txtEmail);

            y += 38;
            body.Controls.Add(MkLabel("Type:", 0, y + 4));
            cmbType = new ComboBox { Location = new Point(130, y), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Corporate", "Retail", "Government" });
            UiTheme.StyleComboBox(cmbType);
            body.Controls.Add(cmbType);

            y += 38;
            body.Controls.Add(MkLabel("Address:", 0, y + 4));
            txtAddress = new TextBox { Location = new Point(130, y), Width = 290, Multiline = true, Height = 56, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtAddress);
            body.Controls.Add(txtAddress);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = UiTheme.Surface, Padding = new Padding(24, 12, 24, 12) };
            var btnSave = new Button { Text = "Save", Width = 100, Height = 34 };
            var btnCancel = new Button { Text = "Cancel", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StylePrimary(btnSave);
            UiTheme.StyleSecondary(btnCancel);
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(btnSave); footer.Controls.Add(btnCancel);
            UiTheme.LayoutLeft(footer, 0, btnSave, btnCancel);

            // ----- Add to Form in correct dock z-order: Fill FIRST, then Top/Bottom -----
            Controls.Add(body);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Top));
            Controls.Add(header);
            Controls.Add(UiTheme.BuildSeparator(DockStyle.Bottom));
            Controls.Add(footer);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private static Label MkLabel(string text, int x, int y)
            => new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = UiTheme.TextPrimary };

        private static TextBox MkText(int x, int y, int w)
        {
            var t = new TextBox { Location = new Point(x, y), Width = w, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(t);
            return t;
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("C", DataStore.Customers.Select(c => c.CustomerId));
                cmbType.SelectedIndex = 0;
            }
            else
            {
                txtId.Text = original.CustomerId;
                txtName.Text = original.CompanyName;
                txtContact.Text = original.ContactPerson;
                txtPhone.Text = original.Phone;
                txtEmail.Text = original.Email;
                cmbType.SelectedItem = original.CustomerType ?? "Corporate";
                txtAddress.Text = original.Address;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var v = new Validation(this);
            v.Required(txtName, "Company name is required.")
             .MinLength(txtName, 2, "Company name too short.")
             .Required(txtPhone, "Phone is required.")
             .Phone(txtPhone)
             .Required(txtEmail, "Email is required.")
             .Email(txtEmail)
             .Selected(cmbType, "Please select customer type.");
            if (!v.ValidateAll()) return;

            // (legacy guard kept for safety)
            if (string.IsNullOrWhiteSpace(txtName.Text)) { UiTheme.ShowWarning(this, "Company name is required."); return; }
            Customer target = original;
            if (target == null)
            {
                target = new Customer { CustomerId = txtId.Text };
                DataStore.Customers.Add(target);
            }
            target.CompanyName = txtName.Text.Trim();
            target.ContactPerson = txtContact.Text.Trim();
            target.Phone = txtPhone.Text.Trim();
            target.Email = txtEmail.Text.Trim();
            target.CustomerType = cmbType.SelectedItem as string;
            target.Address = txtAddress.Text.Trim();
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                original == null ? "New Customer" : "Update Customer", target.CustomerId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
