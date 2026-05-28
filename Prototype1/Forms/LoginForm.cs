using System;
using System.Drawing;
using System.Windows.Forms;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Button btnExit;

        public LoginForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "IDSMS - Login";
            ClientSize = new Size(720, 440);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.Surface;
            Font = UiTheme.FontBase;

            var sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = UiTheme.Primary
            };

            var lblBrand = new Label
            {
                Text = "Premium Living",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 60)
            };
            var lblBrand2 = new Label
            {
                Text = "Furniture Co. Ltd.",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 16F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(28, 95)
            };

            var divider = new Panel
            {
                Location = new Point(28, 145),
                Size = new Size(60, 3),
                BackColor = UiTheme.Accent
            };

            var lblProduct = new Label
            {
                Text = "Integrated Delivery",
                ForeColor = Color.White,
                Font = new Font(UiTheme.FontFamily, 13F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 165)
            };
            var lblProduct2 = new Label
            {
                Text = "Services Management System",
                ForeColor = Color.FromArgb(220, 232, 244),
                Font = new Font(UiTheme.FontFamily, 11F, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(28, 195)
            };

            var lblTagline = new Label
            {
                Text = "Order Processing  -  Logistics\nInventory  -  After-Service",
                ForeColor = Color.FromArgb(190, 210, 228),
                Font = new Font(UiTheme.FontFamily, 9F),
                AutoSize = false,
                Size = new Size(240, 50),
                Location = new Point(28, 240)
            };

            var lblFooter = new Label
            {
                Text = "Prototype I  -  ITP4915M Group 17",
                ForeColor = Color.FromArgb(170, 195, 215),
                Font = new Font(UiTheme.FontFamily, 8F),
                AutoSize = true,
                Location = new Point(28, 390)
            };

            sidePanel.Controls.AddRange(new Control[] { lblBrand, lblBrand2, divider, lblProduct, lblProduct2, lblTagline, lblFooter });

            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.Surface,
                Padding = new Padding(40, 50, 40, 40)
            };

            var lblWelcome = new Label
            {
                Text = "Welcome Back",
                Font = new Font(UiTheme.FontFamily, 18F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 50)
            };

            var lblSubtitle = new Label
            {
                Text = "Sign in to continue to IDSMS",
                Font = new Font(UiTheme.FontFamily, 10F),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Location = new Point(40, 84)
            };

            var lblUser = new Label
            {
                Text = "Username",
                Font = new Font(UiTheme.FontFamily, 9F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 130)
            };
            txtUser = new TextBox
            {
                Location = new Point(40, 152),
                Width = 320,
                Font = new Font(UiTheme.FontFamily, 11F),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblPass = new Label
            {
                Text = "Password",
                Font = new Font(UiTheme.FontFamily, 9F, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(40, 195)
            };
            txtPass = new TextBox
            {
                Location = new Point(40, 217),
                Width = 320,
                Font = new Font(UiTheme.FontFamily, 11F),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            btnLogin = new Button
            {
                Text = "Sign In",
                Location = new Point(40, 265),
                Width = 200,
                Height = 38,
                Font = new Font(UiTheme.FontFamily, 10F, FontStyle.Bold)
            };
            UiTheme.StylePrimary(btnLogin);
            btnLogin.Click += BtnLogin_Click;
            AcceptButton = btnLogin;

            btnExit = new Button
            {
                Text = "Exit",
                Location = new Point(250, 265),
                Width = 110,
                Height = 38
            };
            UiTheme.StyleSecondary(btnExit);
            btnExit.Click += (s, e) => Application.Exit();

            var lblHintTitle = new Label
            {
                Text = "Demo Accounts",
                Font = new Font(UiTheme.FontFamily, 8.5F, FontStyle.Bold),
                ForeColor = UiTheme.TextMuted,
                Location = new Point(40, 320),
                AutoSize = true
            };

            var lblHint = new Label
            {
                Text = "admin/admin123     sales/sales123     logistics/log123\nwarehouse/ware123     service/svc123",
                Location = new Point(40, 340),
                Size = new Size(330, 50),
                ForeColor = UiTheme.TextMuted,
                Font = new Font(UiTheme.FontFamily, 8.5F)
            };

            rightPanel.Controls.AddRange(new Control[] {
                lblWelcome, lblSubtitle,
                lblUser, txtUser, lblPass, txtPass,
                btnLogin, btnExit,
                lblHintTitle, lblHint
            });

            Controls.Add(rightPanel);
            Controls.Add(sidePanel);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (SecurityService.Login(txtUser.Text.Trim(), txtPass.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(this, "Invalid username or password.", UiTheme.AppTitle + " - Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPass.Clear();
                txtPass.Focus();
            }
        }
    }
}
