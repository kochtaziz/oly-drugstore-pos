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
            Width = 760;
            Height = 470;
            MinimumSize = new Size(760, 470);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.Background;
            Font = UiTheme.FontNormal;

            TableLayoutPanel shell = new TableLayoutPanel();
            shell.Dock = DockStyle.Fill;
            shell.ColumnCount = 2;
            shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            shell.Padding = new Padding(22);
            Controls.Add(shell);

            Panel brandPanel = UiTheme.CardPanel();
            brandPanel.BackColor = UiTheme.Primary;
            brandPanel.Dock = DockStyle.Fill;
            shell.Controls.Add(brandPanel, 0, 0);

            Label logo = new Label();
            logo.Text = "OLY";
            logo.ForeColor = Color.White;
            logo.Font = new Font("Segoe UI", 30, FontStyle.Bold);
            logo.Left = 30;
            logo.Top = 38;
            logo.Width = 220;
            brandPanel.Controls.Add(logo);

            Label subtitle = new Label();
            subtitle.Text = "Drugstore POS\nStock, caisse et ventes";
            subtitle.ForeColor = Color.FromArgb(210, 232, 255);
            subtitle.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            subtitle.Left = 34;
            subtitle.Top = 110;
            subtitle.Width = 250;
            subtitle.Height = 80;
            brandPanel.Controls.Add(subtitle);

            Label note = new Label();
            note.Text = "Mode local hors ligne\nPret pour scanner et ticket";
            note.ForeColor = Color.FromArgb(180, 205, 230);
            note.Font = UiTheme.FontSmall;
            note.Left = 34;
            note.Top = 285;
            note.Width = 260;
            note.Height = 70;
            brandPanel.Controls.Add(note);

            Panel formCard = UiTheme.CardPanel();
            formCard.Dock = DockStyle.Fill;
            formCard.Padding = new Padding(36);
            shell.Controls.Add(formCard, 1, 0);

            Label title = new Label();
            title.Text = "Connexion";
            title.ForeColor = UiTheme.Text;
            title.Font = UiTheme.FontTitle;
            title.Left = 36;
            title.Top = 34;
            title.Width = 260;
            formCard.Controls.Add(title);

            languageComboBox = new ComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Items.AddRange(new object[] { "FR", "EN" });
            languageComboBox.SelectedItem = Localization.Language;
            languageComboBox.Left = 300;
            languageComboBox.Top = 38;
            languageComboBox.Width = 74;
            languageComboBox.SelectedIndexChanged += delegate
            {
                Localization.Language = languageComboBox.SelectedItem.ToString();
                RefreshLabels();
            };
            formCard.Controls.Add(languageComboBox);

            usernameLabel = UiTheme.FieldLabel(Localization.T("Username"), 36, 112);
            formCard.Controls.Add(usernameLabel);

            usernameTextBox = UiTheme.TextInput(36, 140, 338);
            usernameTextBox.Text = "admin";
            formCard.Controls.Add(usernameTextBox);

            passwordLabel = UiTheme.FieldLabel(Localization.T("Password"), 36, 202);
            formCard.Controls.Add(passwordLabel);

            passwordTextBox = UiTheme.TextInput(36, 230, 338);
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
            signInButton.Left = 36;
            signInButton.Top = 306;
            signInButton.Width = 338;
            signInButton.Height = 52;
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
