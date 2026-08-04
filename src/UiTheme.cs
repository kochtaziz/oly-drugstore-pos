using System.Drawing;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    public static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(241, 245, 249);
        public static readonly Color Card = Color.White;
        public static readonly Color Primary = Color.FromArgb(15, 23, 42);
        public static readonly Color Accent = Color.FromArgb(22, 101, 52);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Border = Color.FromArgb(203, 213, 225);
        public static readonly Color Text = Color.FromArgb(15, 23, 42);

        public static readonly Font FontSmall = new Font("Segoe UI", 9, FontStyle.Regular);
        public static readonly Font FontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        public static readonly Font FontBold = new Font("Segoe UI", 10, FontStyle.Bold);
        public static readonly Font FontLarge = new Font("Segoe UI", 14, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 22, FontStyle.Bold);

        public static Panel CardPanel()
        {
            Panel panel = new Panel();
            panel.BackColor = Card;
            panel.BorderStyle = BorderStyle.FixedSingle;
            return panel;
        }

        public static Label FieldLabel(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = 140;
            label.Height = 28;
            label.ForeColor = Muted;
            label.Font = FontBold;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        public static TextBox TextInput(int left, int top, int width)
        {
            TextBox input = new TextBox();
            input.Left = left;
            input.Top = top;
            input.Width = width;
            input.Height = 34;
            input.Font = new Font("Segoe UI", 12);
            ApplyInputStyle(input);
            return input;
        }

        public static Button PrimaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Font = FontBold;
            button.Cursor = Cursors.Hand;
            return button;
        }

        public static Button SecondaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Color.White;
            button.ForeColor = Text;
            button.Font = FontBold;
            button.Cursor = Cursors.Hand;
            return button;
        }

        public static DataGridView Grid()
        {
            DataGridView grid = new DataGridView();
            grid.BackgroundColor = Card;
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(226, 232, 240);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.Font = FontNormal;
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 252, 231);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.RowTemplate.Height = 34;
            return grid;
        }

        private static void ApplyInputStyle(Control control)
        {
            control.BackColor = Color.White;
        }
    }
}
