using System;
using System.Drawing;
using System.Windows.Forms;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class ChangePasswordForm : Form
    {
        private TextBox txtOld, txtNew, txtConfirm;
        private Button btnOk, btnCancel;

        public ChangePasswordForm()
        {
            Text = "Change Password";
            ClientSize = new Size(420, 320);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            UiTheme.ApplyForm(this);

            var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiTheme.Primary };
            header.Controls.Add(new Label
            {
                Text = "Change Password",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 12F, FontStyle.Bold),
                Location = new Point(20, 14),
                AutoSize = true
            });
            Controls.Add(header);

            int y = 80;
            Controls.Add(new Label { Text = "Current password:", Location = new Point(24, y + 4), AutoSize = true });
            txtOld = new TextBox { Location = new Point(160, y), Width = 220, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtOld);
            Controls.Add(txtOld);

            y += 40;
            Controls.Add(new Label { Text = "New password:", Location = new Point(24, y + 4), AutoSize = true });
            txtNew = new TextBox { Location = new Point(160, y), Width = 220, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtNew);
            Controls.Add(txtNew);

            y += 40;
            Controls.Add(new Label { Text = "Confirm new:", Location = new Point(24, y + 4), AutoSize = true });
            txtConfirm = new TextBox { Location = new Point(160, y), Width = 220, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
            UiTheme.StyleTextBox(txtConfirm);
            Controls.Add(txtConfirm);

            var lblHint = new Label
            {
                Text = "Password must be at least 5 characters.",
                Location = new Point(24, y + 38),
                Size = new Size(360, 18),
                ForeColor = UiTheme.TextMuted,
                Font = new Font(UiTheme.FontFamily, 8.5F)
            };
            Controls.Add(lblHint);

            btnOk = new Button { Text = "Save", Location = new Point(24, 250), Width = 100, Height = 34 };
            UiTheme.StylePrimary(btnOk);
            btnOk.Click += BtnOk_Click;
            Controls.Add(btnOk);
            AcceptButton = btnOk;

            btnCancel = new Button { Text = "Cancel", Location = new Point(132, 250), Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
            UiTheme.StyleSecondary(btnCancel);
            Controls.Add(btnCancel);
            CancelButton = btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (SecurityService.CurrentUser == null) { Close(); return; }
            if (string.IsNullOrEmpty(txtNew.Text) || txtNew.Text.Length < 5)
            {
                UiTheme.ShowWarning(this, "New password must be at least 5 characters.");
                return;
            }
            if (txtNew.Text != txtConfirm.Text)
            {
                UiTheme.ShowWarning(this, "Passwords do not match.");
                return;
            }
            if (!SecurityService.ChangePassword(SecurityService.CurrentUser.Username, txtOld.Text, txtNew.Text))
            {
                UiTheme.ShowError(this, "Current password is incorrect.");
                return;
            }
            UiTheme.ShowInfo(this, "Password changed successfully.");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
