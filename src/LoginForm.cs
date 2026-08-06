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
        private Label usernameLabel;
        private Label passwordLabel;
        private Button signInButton;

        public User AuthenticatedUser { get; private set; }

        public LoginForm(DataStore store)
        {
            this.store = store;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Oly Drugstore POS";
            Width = 860;
            Height = 540;
            MinimumSize = new Size(860, 540);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Background;
            Font = UiTheme.FontNormal;

            TableLayoutPanel shell = new TableLayoutPanel();
            shell.Dock = DockStyle.Fill;
            shell.ColumnCount = 2;
            shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            shell.Padding = new Padding(28);
            Controls.Add(shell);

            Panel brandPanel = UiTheme.CardPanel();
            brandPanel.BackColor = UiTheme.Primary;
            brandPanel.Dock = DockStyle.Fill;
            shell.Controls.Add(brandPanel, 0, 0);

            Label logo = new Label();
            logo.Text = "OLY";
            logo.ForeColor = Color.White;
            logo.Font = new Font("Segoe UI", 40, FontStyle.Bold);
            logo.Left = 36;
            logo.Top = 42;
            logo.Width = 260;
            logo.Height = 76;
            logo.TextAlign = ContentAlignment.MiddleLeft;
            brandPanel.Controls.Add(logo);

            Label subtitle = new Label();
            subtitle.Text = "Drugstore POS\nStock, caisse et ventes";
            subtitle.ForeColor = Color.FromArgb(210, 232, 255);
            subtitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            subtitle.Left = 40;
            subtitle.Top = 138;
            subtitle.Width = 270;
            subtitle.Height = 82;
            brandPanel.Controls.Add(subtitle);

            Label note = new Label();
            note.Text = "Mode local hors ligne\nPret pour scanner et ticket";
            note.ForeColor = Color.FromArgb(180, 205, 230);
            note.Font = UiTheme.FontSmall;
            note.Left = 40;
            note.Top = 330;
            note.Width = 280;
            note.Height = 76;
            brandPanel.Controls.Add(note);

            Panel formCard = UiTheme.CardPanel();
            formCard.Dock = DockStyle.Fill;
            formCard.Padding = new Padding(42);
            shell.Controls.Add(formCard, 1, 0);

            Label title = new Label();
            title.Text = "Connexion";
            title.ForeColor = UiTheme.Text;
            title.Font = UiTheme.FontTitle;
            title.Left = 42;
            title.Top = 44;
            title.Width = 280;
            title.Height = 64;
            title.TextAlign = ContentAlignment.MiddleLeft;
            formCard.Controls.Add(title);

            languageComboBox = new ComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Items.AddRange(new object[] { "FR", "EN" });
            languageComboBox.SelectedItem = Localization.Language;
            languageComboBox.Left = 340;
            languageComboBox.Top = 62;
            languageComboBox.Width = 80;
            languageComboBox.SelectedIndexChanged += delegate
            {
                Localization.Language = languageComboBox.SelectedItem.ToString();
                RefreshLabels();
            };
            formCard.Controls.Add(languageComboBox);

            usernameLabel = UiTheme.FieldLabel(Localization.T("Username"), 42, 134);
            formCard.Controls.Add(usernameLabel);

            usernameTextBox = UiTheme.TextInput(42, 164, 378);
            usernameTextBox.Text = "admin";
            formCard.Controls.Add(usernameTextBox);

            passwordLabel = UiTheme.FieldLabel(Localization.T("Password"), 42, 232);
            formCard.Controls.Add(passwordLabel);

            passwordTextBox = UiTheme.TextInput(42, 262, 378);
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Text = "admin";
            passwordTextBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    SignIn();
                }
            };
            formCard.Controls.Add(passwordTextBox);

            signInButton = UiTheme.PrimaryButton(Localization.T("SignIn"));
            signInButton.Left = 42;
            signInButton.Top = 354;
            signInButton.Width = 378;
            signInButton.Height = 56;
            signInButton.Click += delegate { SignIn(); };
            formCard.Controls.Add(signInButton);
        }

        private void RefreshLabels()
        {
            usernameLabel.Text = Localization.T("Username");
            passwordLabel.Text = Localization.T("Password");
            signInButton.Text = Localization.T("SignIn");
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
