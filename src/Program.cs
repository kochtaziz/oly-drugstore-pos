using System;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DataStore store = new DataStore();
            using (LoginForm login = new LoginForm(store))
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new MainForm(store, login.AuthenticatedUser));
                }
            }
        }
    }
}
