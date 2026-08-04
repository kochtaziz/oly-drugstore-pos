using System;
using System.Drawing;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    public class LoginForm : Form
    {
        private readonly DataStore store;
        private TextBox usernameTextBox;
        private TextBox passwordTextBox;
        private ComboBox languageComboBox;

        public User AuthenticatedUser { get; private set; }

        public LoginForm(DataStore store)
        {
            this.store = store;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Oly Drugstore POS";
            Width = 420;
            Height = 330;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10);

            Label title = new Label();
            title.Text = "Oly Drugstore POS";
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.Left = 30;
            title.Top = 24;
            title.Width = 340;
            Controls.Add(title);

            languageComboBox = new ComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Items.AddRange(new object[] { "FR", "EN" });
            languageComboBox.SelectedItem = Localization.Language;
            languageComboBox.Left = 300;
            languageComboBox.Top = 28;
            languageComboBox.Width = 70;
            languageComboBox.SelectedIndexChanged += delegate
            {
                Localization.Language = languageComboBox.SelectedItem.ToString();
                RefreshLabels();
            };
            Controls.Add(languageComboBox);

            Label userLabel = new Label();
            userLabel.Name = "userLabel";
            userLabel.Left = 30;
            userLabel.Top = 92;
            userLabel.Width = 340;
            Controls.Add(userLabel);

            usernameTextBox = new TextBox();
            usernameTextBox.Left = 30;
            usernameTextBox.Top = 118;
            usernameTextBox.Width = 340;
            usernameTextBox.Text = "admin";
            Controls.Add(usernameTextBox);

            Label passLabel = new Label();
            passLabel.Name = "passLabel";
            passLabel.Left = 30;
            passLabel.Top = 156;
            passLabel.Width = 340;
            Controls.Add(passLabel);

            passwordTextBox = new TextBox();
            passwordTextBox.Left = 30;
            passwordTextBox.Top = 182;
            passwordTextBox.Width = 340;
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Text = "admin";
            passwordTextBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SignIn();
                }
            };
            Controls.Add(passwordTextBox);

            Button signInButton = new Button();
            signInButton.Name = "signInButton";
            signInButton.Left = 30;
            signInButton.Top = 228;
            signInButton.Width = 340;
            signInButton.Height = 42;
            signInButton.Click += delegate { SignIn(); };
            Controls.Add(signInButton);

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            Control[] userLabels = Controls.Find("userLabel", true);
            Control[] passLabels = Controls.Find("passLabel", true);
            Control[] signButtons = Controls.Find("signInButton", true);
            if (userLabels.Length > 0) userLabels[0].Text = Localization.T("Username");
            if (passLabels.Length > 0) passLabels[0].Text = Localization.T("Password");
            if (signButtons.Length > 0) signButtons[0].Text = Localization.T("SignIn");
        }

        private void SignIn()
        {
            AuthenticatedUser = store.Authenticate(usernameTextBox.Text, passwordTextBox.Text);
            if (AuthenticatedUser == null)
            {
                MessageBox.Show(Localization.T("InvalidLogin"));
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
