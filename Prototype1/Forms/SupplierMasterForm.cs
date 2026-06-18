using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class SupplierMasterForm : Form
    {
        private DataGridView grid;
        private TextBox txtSearch;

        public SupplierMasterForm()
        {
            Text = "Master Data - Suppliers";
            ClientSize = new Size(940, 540);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            var top = UiTheme.BuildToolbar(64);
            var lblTitle = UiTheme.BuildHeading("Suppliers");
            lblTitle.Location = new Point(0, 8);
            top.Controls.Add(lblTitle);
            top.Controls.Add(new Label { Text = "Search:", Location = new Point(0, 38), AutoSize = true, ForeColor = UiTheme.TextMuted });
            txtSearch = new TextBox { Location = new Point(56, 35), Width = 280, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid();
            top.Controls.Add(txtSearch);

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
            var btnDelete = new Button { Text = "Delete", Width = 90, Height = 34 };
            btnDelete.Click += (s, e) => DoDelete();
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDelete, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleDanger(btnDelete);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnDelete, btnClose);
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
            grid.Columns.Add("Id", "Supplier ID");
            grid.Columns.Add("Name", "Company");
            grid.Columns.Add("Contact", "Contact");
            grid.Columns.Add("Phone", "Phone");
            grid.Columns.Add("Email", "Email");
            grid.Columns.Add("Terms", "Payment Terms");
            grid.Columns.Add("Address", "Address");
            string filter = txtSearch.Text.Trim().ToLower();
            foreach (var s in DataStore.Suppliers)
            {
                if (filter.Length > 0)
                {
                    string hay = (s.SupplierId + " " + s.CompanyName + " " + s.ContactPerson).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                grid.Rows.Add(s.SupplierId, s.CompanyName, s.ContactPerson, s.Phone, s.Email, s.PaymentTerms, s.Address);
            }
        }

        private Supplier Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Suppliers.FirstOrDefault(s => s.SupplierId == id);
        }

        private void DoNew()
        {
            using (var f = new SupplierEditForm(null))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoEdit()
        {
            var s = Selected();
            if (s == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            using (var f = new SupplierEditForm(s))
            {
                if (f.ShowDialog(this) == DialogResult.OK) LoadGrid();
            }
        }

        private void DoDelete()
        {
            if (!SecurityService.IsManager)
            { UiTheme.ShowWarning(this, "Only a department manager can delete records."); return; }
            var s = Selected();
            if (s == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            if (DataStore.Items.Any(i => i.SupplierId == s.SupplierId) ||
                DataStore.Receipts.Any(r => r.SupplierId == s.SupplierId))
            {
                UiTheme.ShowWarning(this, "Cannot delete: this supplier is referenced by items or receipts.");
                return;
            }
            if (!UiTheme.ShowConfirm(this, "Delete " + s.CompanyName + "?")) return;
            DataStore.Suppliers.Remove(s);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Delete Supplier", s.SupplierId);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    public class SupplierEditForm : Form
    {
        private readonly Supplier original;
        private TextBox txtId, txtName, txtContact, txtPhone, txtEmail, txtAddress;
        private ComboBox cmbTerms;

        // Default suggestions - users can still type a new value
        private static readonly string[] DefaultPaymentTerms =
            { "Net 30", "Net 45", "Net 60", "Net 90", "COD", "Prepaid", "50% Deposit" };

        public SupplierEditForm(Supplier s)
        {
            original = s;
            Text = s == null ? "New Supplier" : "Edit Supplier - " + s.SupplierId;
            ClientSize = new Size(490, 470);
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
            header.Controls.Add(new Label { Text = original == null ? "New Supplier" : "Edit Supplier", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Supplier ID:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            Controls.Add(txtId);
            y += 35;
            Controls.Add(new Label { Text = "Company:", Location = new Point(20, y + 3), AutoSize = true });
            txtName = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtName);
            y += 35;
            Controls.Add(new Label { Text = "Contact:", Location = new Point(20, y + 3), AutoSize = true });
            txtContact = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtContact);
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
            Controls.Add(new Label { Text = "Payment Terms:", Location = new Point(20, y + 3), AutoSize = true });
            cmbTerms = new ComboBox
            {
                Location = new Point(140, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var t in BuildTermsOptions()) cmbTerms.Items.Add(t);
            UiTheme.StyleComboBox(cmbTerms);
            Controls.Add(cmbTerms);
            y += 35;
            Controls.Add(new Label { Text = "Address:", Location = new Point(20, y + 3), AutoSize = true });
            txtAddress = new TextBox { Location = new Point(140, y), Width = 280, Multiline = true, Height = 50 };
            Controls.Add(txtAddress);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 70), Width = 100, Height = 34 };
            btnSave.Click += BtnSave_Click;
            UiTheme.StylePrimary(btnSave);
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
                txtId.Text = DataStore.NextId("SP", DataStore.Suppliers.Select(s => s.SupplierId));
            }
            else
            {
                txtId.Text = original.SupplierId;
                txtName.Text = original.CompanyName;
                txtContact.Text = original.ContactPerson;
                txtPhone.Text = original.Phone;
                txtEmail.Text = original.Email;
                cmbTerms.Text = original.PaymentTerms;
                txtAddress.Text = original.Address;
            }
        }

        // Combine defaults with whatever already exists so old data isn't lost.
        private static IEnumerable<string> BuildTermsOptions()
        {
            var existing = DataStore.Suppliers
                .Select(s => s.PaymentTerms)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim());
            return DefaultPaymentTerms.Concat(existing)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t);
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
             .Required(cmbTerms, "Payment terms required.");
            if (!v.ValidateAll()) return;
            var target = original;
            if (target == null)
            {
                target = new Supplier { SupplierId = txtId.Text };
                DataStore.Suppliers.Add(target);
            }
            target.CompanyName = txtName.Text.Trim();
            target.ContactPerson = txtContact.Text.Trim();
            target.Phone = txtPhone.Text.Trim();
            target.Email = txtEmail.Text.Trim();
            target.PaymentTerms = cmbTerms.Text.Trim();
            target.Address = txtAddress.Text.Trim();
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                original == null ? "New Supplier" : "Update Supplier", target.SupplierId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
