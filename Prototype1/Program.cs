using System;
using System.Windows.Forms;
using Prototype1.Forms;
using Prototype1.Database;

namespace Prototype1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DataStore.LoadAll();

            while (true)
            {
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }

                using (var menu = new MainMenuForm())
                {
                    var result = menu.ShowDialog();
                    if (result != DialogResult.Retry)
                    {
                        DataStore.SaveAll();
                        return;
                    }
                }
            }
        }
    }
}
