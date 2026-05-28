using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Prototype1.Models;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class ItemMasterForm : Form
    {
        private DataGridView grid;
        private TextBox txtSearch;

        public ItemMasterForm()
        {
            Text = "Master Data - Items / Inventory";
            ClientSize = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);
            BuildUI();
            LoadGrid();
        }

        private void BuildUI()
        {
            // 1. TOP
            var top = UiTheme.BuildToolbar(64);
            var lblTitle = UiTheme.BuildHeading("Items / Inventory");
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
            var btnAdj = new Button { Text = "Adjust Stock", Width = 120, Height = 34 };
            btnAdj.Click += (s, e) => DoAdjust();
            var btnClose = new Button { Text = "Close", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            bottom.Controls.AddRange(new Control[] { btnNew, btnEdit, btnDel, btnAdj, btnClose });
            UiTheme.StylePrimary(btnNew);
            UiTheme.StyleSecondary(btnEdit);
            UiTheme.StyleDanger(btnDel);
            UiTheme.StyleAccent(btnAdj);
            UiTheme.StyleSecondary(btnClose);
            UiTheme.LayoutLeft(bottom, 8, btnNew, btnEdit, btnDel, btnAdj, btnClose);

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
            grid.Columns.Add("Id", "Item ID");
            grid.Columns.Add("Name", "Item Name");
            grid.Columns.Add("Cat", "Category");
            grid.Columns.Add("Unit", "Unit");
            grid.Columns.Add("Price", "Unit Price");
            grid.Columns.Add("Stock", "Stock Qty");
            grid.Columns.Add("Reorder", "Reorder Lvl");
            grid.Columns.Add("Supplier", "Supplier");
            UiTheme.AlignNumericColumns(grid);
            string filter = txtSearch.Text.Trim().ToLower();
            foreach (var i in DataStore.Items)
            {
                if (filter.Length > 0)
                {
                    string hay = (i.ItemId + " " + i.ItemName + " " + i.Category).ToLower();
                    if (!hay.Contains(filter)) continue;
                }
                var sup = DataStore.Suppliers.FirstOrDefault(x => x.SupplierId == i.SupplierId);
                int rowIdx = grid.Rows.Add(i.ItemId, i.ItemName, i.Category, i.Unit,
                    i.UnitPrice.ToString("N2"), i.StockQty, i.ReorderLevel,
                    sup != null ? sup.CompanyName : i.SupplierId);
                if (i.StockQty <= i.ReorderLevel)
                {
                    grid.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(253, 235, 232);
                    grid.Rows[rowIdx].DefaultCellStyle.ForeColor = UiTheme.Danger;
                }
            }
        }

        private Item Selected()
        {
            if (grid.CurrentRow == null) return null;
            string id = grid.CurrentRow.Cells[0].Value as string;
            return DataStore.Items.FirstOrDefault(x => x.ItemId == id);
        }

        private void DoNew()
        {
            using (var f = new ItemEditForm(null)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoEdit()
        {
            var i = Selected();
            if (i == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            using (var f = new ItemEditForm(i)) { if (f.ShowDialog(this) == DialogResult.OK) LoadGrid(); }
        }

        private void DoDelete()
        {
            var i = Selected();
            if (i == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            if (DataStore.SalesOrders.Any(o => o.Lines.Any(l => l.ItemId == i.ItemId)) ||
                DataStore.Receipts.Any(r => r.ItemId == i.ItemId))
            {
                UiTheme.ShowWarning(this, "Cannot delete: this item is referenced by orders or receipts.");
                return;
            }
            if (!UiTheme.ShowConfirm(this, "Delete " + i.ItemName + "?")) return;
            DataStore.Items.Remove(i);
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Delete Item", i.ItemId);
            DataStore.SaveAll();
            LoadGrid();
        }

        private void DoAdjust()
        {
            var i = Selected();
            if (i == null) { UiTheme.ShowWarning(this, "Select a row."); return; }
            string input = InputPrompt.Show(this,
                "Enter adjustment (positive to add, negative to subtract) for " + i.ItemName + ":",
                "Adjust Stock", "0");
            if (input == null) return;
            int adj;
            if (!int.TryParse(input, out adj)) { return; }
            if (i.StockQty + adj < 0) { UiTheme.ShowWarning(this, "Stock cannot go below zero."); return; }
            i.StockQty += adj;
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "", "Adjust Stock",
                i.ItemId + " by " + adj);
            DataStore.SaveAll();
            LoadGrid();
        }
    }

    public class ItemEditForm : Form
    {
        private readonly Item original;
        private TextBox txtId, txtName, txtPrice, txtStock, txtReorder;
        // KeyPress handlers attached in BuildUI
        private ComboBox cmbCat, cmbUnit, cmbSupplier;

        // Default suggestions - users can also type a new value
        private static readonly string[] DefaultCategories =
            { "Living", "Bedroom", "Dining", "Office", "Storage", "Outdoor", "Lighting", "Decor" };
        private static readonly string[] DefaultUnits =
            { "PC", "SET", "PAIR", "BOX", "METER", "KG" };

        public ItemEditForm(Item i)
        {
            original = i;
            Text = i == null ? "New Item" : "Edit Item - " + i.ItemId;
            ClientSize = new Size(490, 490);
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
            header.Controls.Add(new Label { Text = original == null ? "New Item" : "Edit Item", ForeColor = Color.White, Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Item ID:", Location = new Point(20, y + 3), AutoSize = true });
            txtId = new TextBox { Location = new Point(140, y), Width = 200, ReadOnly = true };
            Controls.Add(txtId);
            y += 35;
            Controls.Add(new Label { Text = "Item Name:", Location = new Point(20, y + 3), AutoSize = true });
            txtName = new TextBox { Location = new Point(140, y), Width = 280 };
            Controls.Add(txtName);
            y += 35;
            Controls.Add(new Label { Text = "Category:", Location = new Point(20, y + 3), AutoSize = true });
            cmbCat = new ComboBox
            {
                Location = new Point(140, y),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDown,    // editable + dropdown
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var c in BuildCategoryOptions()) cmbCat.Items.Add(c);
            UiTheme.StyleComboBox(cmbCat);
            Controls.Add(cmbCat);
            y += 35;
            Controls.Add(new Label { Text = "Unit:", Location = new Point(20, y + 3), AutoSize = true });
            cmbUnit = new ComboBox
            {
                Location = new Point(140, y),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            foreach (var u in BuildUnitOptions()) cmbUnit.Items.Add(u);
            UiTheme.StyleComboBox(cmbUnit);
            Controls.Add(cmbUnit);
            y += 35;
            Controls.Add(new Label { Text = "Unit Price:", Location = new Point(20, y + 3), AutoSize = true });
            txtPrice = new TextBox { Location = new Point(140, y), Width = 120 };
            txtPrice.KeyPress += Validation.OnlyDecimal;
            Controls.Add(txtPrice);
            y += 35;
            Controls.Add(new Label { Text = "Stock Qty:", Location = new Point(20, y + 3), AutoSize = true });
            txtStock = new TextBox { Location = new Point(140, y), Width = 80 };
            txtStock.KeyPress += Validation.OnlyDigits;
            Controls.Add(txtStock);
            y += 35;
            Controls.Add(new Label { Text = "Reorder Level:", Location = new Point(20, y + 3), AutoSize = true });
            txtReorder = new TextBox { Location = new Point(140, y), Width = 80 };
            txtReorder.KeyPress += Validation.OnlyDigits;
            Controls.Add(txtReorder);
            y += 35;
            Controls.Add(new Label { Text = "Supplier:", Location = new Point(20, y + 3), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(140, y), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSupplier.Items.Add(new ListItem("", "(none)"));
            foreach (var s in DataStore.Suppliers) cmbSupplier.Items.Add(new ListItem(s.SupplierId, s.SupplierId + " - " + s.CompanyName));
            cmbSupplier.DisplayMember = "Display";
            UiTheme.StyleComboBox(cmbSupplier);
            Controls.Add(cmbSupplier);

            var btnSave = new Button { Text = "Save", Location = new Point(20, y + 60), Width = 100, Height = 34 };
            btnSave.Click += BtnSave_Click;
            UiTheme.StylePrimary(btnSave);
            Controls.Add(btnSave);
            AcceptButton = btnSave;
            var btnCancel = new Button { Text = "Cancel", Location = new Point(128, y + 60), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void Load_()
        {
            if (original == null)
            {
                txtId.Text = DataStore.NextId("I", DataStore.Items.Select(i => i.ItemId));
                cmbUnit.Text = "PC";
                txtPrice.Text = "0";
                txtStock.Text = "0";
                txtReorder.Text = "0";
                cmbSupplier.SelectedIndex = 0;
            }
            else
            {
                txtId.Text = original.ItemId;
                txtName.Text = original.ItemName;
                cmbCat.Text = original.Category;
                cmbUnit.Text = original.Unit;
                txtPrice.Text = original.UnitPrice.ToString();
                txtStock.Text = original.StockQty.ToString();
                txtReorder.Text = original.ReorderLevel.ToString();
                for (int i = 0; i < cmbSupplier.Items.Count; i++)
                {
                    if (((ListItem)cmbSupplier.Items[i]).Value == (original.SupplierId ?? ""))
                    { cmbSupplier.SelectedIndex = i; break; }
                }
                if (cmbSupplier.SelectedIndex < 0) cmbSupplier.SelectedIndex = 0;
            }
        }

        // Combine defaults with whatever already exists in DataStore so we don't
        // lose categories/units that users typed before this update.
        private static IEnumerable<string> BuildCategoryOptions()
        {
            var existing = DataStore.Items
                .Select(i => i.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim());
            return DefaultCategories.Concat(existing)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c);
        }

        private static IEnumerable<string> BuildUnitOptions()
        {
            var existing = DataStore.Items
                .Select(i => i.Unit)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim().ToUpperInvariant());
            return DefaultUnits.Concat(existing)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(u => u);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var v = new Validation(this);
            v.Required(txtName, "Item name required.")
             .MinLength(txtName, 2, "Name too short.")
             .Required(cmbCat, "Category required.")
             .Required(cmbUnit, "Unit required.")
             .Required(txtPrice, "Unit price required.")
             .Money(txtPrice, "Unit price must be a number >= 0.")
             .Required(txtStock, "Stock required.")
             .Integer(txtStock, 0, 999999, "Stock must be 0-999999.")
             .Required(txtReorder, "Reorder level required.")
             .Integer(txtReorder, 0, 999999, "Reorder level must be 0-999999.");
            if (!v.ValidateAll()) return;

            decimal price; int stock, reorder;
            decimal.TryParse(txtPrice.Text, out price);
            int.TryParse(txtStock.Text, out stock);
            int.TryParse(txtReorder.Text, out reorder);

            var t = original;
            if (t == null) { t = new Item { ItemId = txtId.Text }; DataStore.Items.Add(t); }
            t.ItemName = txtName.Text.Trim();
            t.Category = cmbCat.Text.Trim();
            t.Unit = cmbUnit.Text.Trim();
            t.UnitPrice = price;
            t.StockQty = stock;
            t.ReorderLevel = reorder;
            t.SupplierId = cmbSupplier.SelectedItem != null ? ((ListItem)cmbSupplier.SelectedItem).Value : "";
            SecurityService.Audit(SecurityService.CurrentUser != null ? SecurityService.CurrentUser.Username : "",
                original == null ? "New Item" : "Update Item", t.ItemId);
            DataStore.SaveAll();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
