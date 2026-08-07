using System.Drawing;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    public class SmoothPanel : Panel
    {
        public Color BorderColor { get; set; }

        public SmoothPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BorderColor = UiTheme.Border;
            BorderStyle = BorderStyle.None;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(BorderColor))
            {
                Rectangle border = ClientRectangle;
                border.Width -= 1;
                border.Height -= 1;
                e.Graphics.DrawRectangle(pen, border);
            }
        }
    }

    public class SmoothFlowLayoutPanel : FlowLayoutPanel
    {
        public SmoothFlowLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    public class SmoothDataGridView : DataGridView
    {
        public SmoothDataGridView()
        {
            DoubleBuffered = true;
        }
    }

    public class ThemedButton : Button
    {
        private Color normalBackColor;
        private Color hoverBackColor;
        private Color pressedBackColor;

        public Color NormalBackColor
        {
            get { return normalBackColor; }
            set
            {
                normalBackColor = value;
                BackColor = value;
            }
        }

        public Color HoverBackColor
        {
            get { return hoverBackColor; }
            set { hoverBackColor = value; }
        }

        public Color PressedBackColor
        {
            get { return pressedBackColor; }
            set { pressedBackColor = value; }
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            BackColor = hoverBackColor;
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = normalBackColor;
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            BackColor = pressedBackColor;
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            BackColor = ClientRectangle.Contains(PointToClient(Cursor.Position)) ? hoverBackColor : normalBackColor;
        }
    }

    public static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(245, 247, 251);
        public static readonly Color Card = Color.White;
        public static readonly Color CardAlt = Color.FromArgb(248, 250, 252);
        public static readonly Color Primary = Color.FromArgb(12, 18, 32);
        public static readonly Color PrimarySoft = Color.FromArgb(30, 41, 59);
        public static readonly Color Accent = Color.FromArgb(22, 121, 70);
        public static readonly Color AccentHover = Color.FromArgb(18, 101, 58);
        public static readonly Color AccentSoft = Color.FromArgb(225, 249, 235);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Border = Color.FromArgb(218, 226, 236);
        public static readonly Color BorderStrong = Color.FromArgb(188, 199, 213);
        public static readonly Color Text = Color.FromArgb(17, 24, 39);
        public static readonly Color DangerSoft = Color.FromArgb(255, 247, 237);

        public static readonly Font FontSmall = new Font("Segoe UI", 9, FontStyle.Regular);
        public static readonly Font FontNormal = new Font("Segoe UI", 10, FontStyle.Regular);
        public static readonly Font FontBold = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        public static readonly Font FontLarge = new Font("Segoe UI", 14, FontStyle.Bold);
        public static readonly Font FontTitle = new Font("Segoe UI", 22, FontStyle.Bold);

        public static Panel CardPanel()
        {
            Panel panel = new SmoothPanel();
            panel.BackColor = Card;
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
            input.Height = 42;
            input.AutoSize = false;
            input.Font = new Font("Segoe UI", 12);
            input.BorderStyle = BorderStyle.FixedSingle;
            ApplyInputStyle(input);
            return input;
        }

        public static Button PrimaryButton(string text)
        {
            ThemedButton button = new ThemedButton();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.NormalBackColor = Accent;
            button.HoverBackColor = AccentHover;
            button.PressedBackColor = Color.FromArgb(13, 84, 46);
            button.ForeColor = Color.White;
            button.Font = FontBold;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        public static Button SecondaryButton(string text)
        {
            ThemedButton button = new ThemedButton();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderStrong;
            button.FlatAppearance.BorderSize = 1;
            button.NormalBackColor = Color.White;
            button.HoverBackColor = CardAlt;
            button.PressedBackColor = Color.FromArgb(232, 238, 247);
            button.ForeColor = Text;
            button.Font = FontBold;
            button.Cursor = Cursors.Hand;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
            return button;
        }

        public static DataGridView Grid()
        {
            DataGridView grid = new SmoothDataGridView();
            grid.BackgroundColor = Card;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 241, 247);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = FontBold;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersHeight = 44;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = Card;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            grid.DefaultCellStyle.SelectionBackColor = AccentSoft;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.RowTemplate.Height = 42;
            return grid;
        }

        private static void ApplyInputStyle(Control control)
        {
            control.BackColor = Color.White;
        }
    }
}
