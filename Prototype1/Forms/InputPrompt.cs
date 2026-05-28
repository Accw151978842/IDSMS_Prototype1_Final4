using System.Drawing;
using System.Windows.Forms;
using Prototype1.Database;

namespace Prototype1.Forms
{
    public static class InputPrompt
    {
        public static string Show(IWin32Window owner, string prompt, string title, string defaultValue)
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.ClientSize = new Size(400, 180);
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false; f.MinimizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent;
                UiTheme.ApplyForm(f);

                var header = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = UiTheme.Primary };
                header.Controls.Add(new Label
                {
                    Text = title,
                    ForeColor = Color.White,
                    Font = new Font(UiTheme.FontFamily, 11F, FontStyle.Bold),
                    Location = new Point(16, 10),
                    AutoSize = true
                });
                f.Controls.Add(header);

                var lbl = new Label { Text = prompt, Location = new Point(20, 55), Size = new Size(360, 35), ForeColor = UiTheme.TextPrimary };
                var txt = new TextBox { Location = new Point(20, 92), Width = 360, Text = defaultValue ?? "", BorderStyle = BorderStyle.FixedSingle };
                UiTheme.StyleTextBox(txt);
                var ok = new Button { Text = "OK", Location = new Point(20, 130), Width = 90, Height = 34, DialogResult = DialogResult.OK };
                UiTheme.StylePrimary(ok);
                var cancel = new Button { Text = "Cancel", Location = new Point(118, 130), Width = 95, Height = 34, DialogResult = DialogResult.Cancel };
                UiTheme.StyleSecondary(cancel);
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                return f.ShowDialog(owner) == DialogResult.OK ? txt.Text : null;
            }
        }
    }
}
