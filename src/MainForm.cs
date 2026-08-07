using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    public class MainForm : Form
    {
        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private readonly DataStore store;
        private readonly User user;
        private readonly List<SaleItem> cart = new List<SaleItem>();
        private string activeStoreId = "STORE-1";

        private TabControl tabs;
        private ComboBox storeComboBox;
        private ComboBox languageComboBox;
        private TextBox searchTextBox;
        private Panel productViewportPanel;
        private FlowLayoutPanel productButtonsPanel;
        private VScrollBar productScrollBar;
        private FlowLayoutPanel categoryButtonsPanel;
        private DataGridView cartGrid;
        private Label totalLabel;
        private Label sessionSummaryLabel;
        private NumericUpDown addQuantityInput;
        private NumericUpDown saleDiscountInput;
        private ComboBox paymentComboBox;
        private CheckBox employeeDiscountCheckBox;
        private CheckBox returnCheckBox;
        private CheckBox debtCheckBox;
        private TextBox customerTextBox;
        private Button increaseQuantityButton;
        private Button decreaseQuantityButton;
        private Button clearCartButton;
        private Button printReceiptButton;
        private Sale lastCompletedSale;
        private string activeCategory = "All";
        private bool suppressSearchAutoAdd;
        private bool refreshingCategories;
        private string lastCategoryRenderKey = "";
        private string lastProductRenderKey = "";
        private Timer responsiveLayoutTimer;

        private DataGridView stockGrid;
        private TextBox productNameInput;
        private TextBox productCategoryInput;
        private TextBox barcodeInput;
        private NumericUpDown purchasePriceInput;
        private NumericUpDown salePriceInput;
        private NumericUpDown taxInput;
        private NumericUpDown quantityInput;
        private NumericUpDown minimumInput;
        private DateTimePicker expiryInput;
        private ComboBox productStoreInput;
        private Product selectedProduct;

        private Label cashStatusLabel;
        private Label cashKpiLabel;
        private ComboBox movementTypeInput;
        private NumericUpDown movementAmountInput;
        private TextBox movementReasonInput;
        private NumericUpDown countedCashInput;
        private TextBox reportTextBox;

        public MainForm(DataStore store, User user)
        {
            this.store = store;
            this.user = user;
            BuildUi();
            RefreshAll();
        }

        private void BuildUi()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            Text = "Oly Drugstore POS - " + user.FullName;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(980, 640);
            BackColor = UiTheme.Background;
            Font = UiTheme.FontNormal;

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = UiTheme.FontBold;
            tabs.Padding = new Point(18, 8);
            tabs.ItemSize = new Size(118, 34);
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += DrawMainTab;
            Controls.Add(tabs);
            Controls.Add(BuildTopBar());

            if (user.Role == UserRole.Admin)
            {
                BuildProductsTab();
            }
            else
            {
                BuildSalesTab();
                BuildCashTab();
                BuildReportsTab();
            }
            BuildSettingsTab();
            RefreshText();
            responsiveLayoutTimer = new Timer();
            responsiveLayoutTimer.Interval = 33;
            responsiveLayoutTimer.Tick += delegate
            {
                responsiveLayoutTimer.Stop();
                ApplyResponsiveLayout();
            };
            Shown += delegate { ApplyResponsiveLayout(); };
            Resize += delegate { ScheduleResponsiveLayout(); };
        }

        private Control BuildTopBar()
        {
            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Top;
            top.Height = 96;
            top.BackColor = UiTheme.Primary;
            top.ColumnCount = 4;
            top.RowCount = 1;
            top.Padding = new Padding(24, 14, 24, 14);
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Panel brand = new Panel();
            brand.Dock = DockStyle.Fill;
            brand.BackColor = UiTheme.Primary;
            top.Controls.Add(brand, 0, 0);

            Label logo = new Label();
            logo.Text = "OLY Drugstore POS";
            logo.ForeColor = Color.White;
            logo.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            logo.Left = 0;
            logo.Top = 4;
            logo.Width = 360;
            logo.Height = 36;
            logo.TextAlign = ContentAlignment.MiddleLeft;
            brand.Controls.Add(logo);

            Label userLabel = new Label();
            userLabel.Text = user.FullName + "  |  " + user.Role;
            userLabel.ForeColor = Color.FromArgb(196, 211, 229);
            userLabel.Font = UiTheme.FontSmall;
            userLabel.Left = 2;
            userLabel.Top = 46;
            userLabel.Width = 360;
            userLabel.Height = 22;
            userLabel.TextAlign = ContentAlignment.MiddleLeft;
            brand.Controls.Add(userLabel);

            storeComboBox = new ComboBox();
            storeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            storeComboBox.Font = UiTheme.FontBold;
            storeComboBox.BackColor = Color.White;
            storeComboBox.ForeColor = UiTheme.Text;
            storeComboBox.Dock = DockStyle.Fill;
            storeComboBox.Margin = new Padding(10, 20, 10, 18);
            foreach (Store item in store.Database.Stores)
            {
                storeComboBox.Items.Add(item.Id + " - " + item.Name);
            }
            storeComboBox.SelectedIndex = 0;
            storeComboBox.SelectedIndexChanged += delegate
            {
                activeStoreId = store.Database.Stores[storeComboBox.SelectedIndex].Id;
                RefreshAll();
            };
            top.Controls.Add(storeComboBox, 1, 0);

            languageComboBox = new ComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Font = UiTheme.FontBold;
            languageComboBox.BackColor = Color.White;
            languageComboBox.ForeColor = UiTheme.Text;
            languageComboBox.Items.AddRange(new object[] { "FR", "EN" });
            languageComboBox.SelectedItem = Localization.Language;
            languageComboBox.Dock = DockStyle.Fill;
            languageComboBox.Margin = new Padding(10, 20, 10, 18);
            languageComboBox.SelectedIndexChanged += delegate
            {
                Localization.Language = languageComboBox.SelectedItem.ToString();
                RefreshText();
            };
            top.Controls.Add(languageComboBox, 2, 0);

            sessionSummaryLabel = new Label();
            sessionSummaryLabel.ForeColor = Color.FromArgb(240, 253, 244);
            sessionSummaryLabel.Font = UiTheme.FontBold;
            sessionSummaryLabel.TextAlign = ContentAlignment.MiddleRight;
            sessionSummaryLabel.Dock = DockStyle.Fill;
            sessionSummaryLabel.Margin = new Padding(10, 18, 0, 16);
            top.Controls.Add(sessionSummaryLabel, 3, 0);

            return top;
        }

        private void DrawMainTab(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabs.TabPages[e.Index];
            bool selected = e.Index == tabs.SelectedIndex;
            Rectangle tabBounds = e.Bounds;
            Color backColor = selected ? Color.White : UiTheme.Background;
            Color textColor = selected ? UiTheme.Accent : UiTheme.Text;

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, tabBounds);
            }

            if (selected)
            {
                using (SolidBrush brush = new SolidBrush(UiTheme.Accent))
                {
                    e.Graphics.FillRectangle(brush, tabBounds.Left + 16, tabBounds.Bottom - 3, tabBounds.Width - 32, 3);
                }
            }

            TextRenderer.DrawText(
                e.Graphics,
                page.Text,
                UiTheme.FontBold,
                tabBounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void BuildSalesTab()
        {
            TabPage tab = NewTab("salesTab");

            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            tab.Controls.Add(shell);

            Panel productCard = UiTheme.CardPanel();
            productCard.Dock = DockStyle.Fill;
            productCard.Padding = new Padding(18);
            productCard.AutoScroll = true;
            shell.Controls.Add(productCard, 0, 0);

            Label productTitle = CardTitle("Catalogue / Scanner", 18, 14);
            productCard.Controls.Add(productTitle);

            Label scannerLabel = UiTheme.FieldLabel("Scanner / code a barres", 18, 52);
            scannerLabel.Name = "scannerLabel";
            scannerLabel.Width = 260;
            productCard.Controls.Add(scannerLabel);

            searchTextBox = UiTheme.TextInput(18, 84, 250);
            searchTextBox.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            searchTextBox.TextChanged += delegate { HandleSearchChanged(); };
            searchTextBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    AddProductToCart(searchTextBox.Text);
                    e.SuppressKeyPress = true;
                }
            };
            productCard.Controls.Add(searchTextBox);

            AddLabel(productCard, "addQuantityLabel", Localization.T("Quantity"), 290, 52);
            addQuantityInput = new NumericUpDown();
            addQuantityInput.Minimum = 1;
            addQuantityInput.Maximum = 999;
            addQuantityInput.Value = 1;
            addQuantityInput.Left = 290;
            addQuantityInput.Top = 84;
            addQuantityInput.Width = 70;
            addQuantityInput.Height = 56;
            addQuantityInput.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            productCard.Controls.Add(addQuantityInput);

            Button quantityDownButton = UiTheme.SecondaryButton("-");
            quantityDownButton.Name = "quantityDownButton";
            quantityDownButton.Left = 370;
            quantityDownButton.Top = 84;
            quantityDownButton.Width = 52;
            quantityDownButton.Height = 56;
            quantityDownButton.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            quantityDownButton.Click += delegate { ChangeAddQuantity(-1); };
            productCard.Controls.Add(quantityDownButton);

            Button quantityUpButton = UiTheme.SecondaryButton("+");
            quantityUpButton.Name = "quantityUpButton";
            quantityUpButton.Left = 430;
            quantityUpButton.Top = 84;
            quantityUpButton.Width = 52;
            quantityUpButton.Height = 56;
            quantityUpButton.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            quantityUpButton.Click += delegate { ChangeAddQuantity(1); };
            productCard.Controls.Add(quantityUpButton);

            Button addButton = UiTheme.PrimaryButton(Localization.T("Add"));
            addButton.Name = "addButton";
            addButton.Left = 490;
            addButton.Top = 84;
            addButton.Width = 130;
            addButton.Height = 56;
            addButton.Click += delegate { AddProductToCart(searchTextBox.Text); };
            productCard.Controls.Add(addButton);

            categoryButtonsPanel = new SmoothFlowLayoutPanel();
            categoryButtonsPanel.Left = 18;
            categoryButtonsPanel.Top = 158;
            categoryButtonsPanel.Width = 145;
            categoryButtonsPanel.Height = 345;
            categoryButtonsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            categoryButtonsPanel.AutoScroll = true;
            categoryButtonsPanel.WrapContents = true;
            categoryButtonsPanel.FlowDirection = FlowDirection.TopDown;
            categoryButtonsPanel.BackColor = UiTheme.CardAlt;
            categoryButtonsPanel.Padding = new Padding(0);
            productCard.Controls.Add(categoryButtonsPanel);

            productViewportPanel = new SmoothPanel();
            productViewportPanel.Left = 172;
            productViewportPanel.Top = 158;
            productViewportPanel.Width = 390;
            productViewportPanel.Height = 345;
            productViewportPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            productViewportPanel.BackColor = UiTheme.CardAlt;
            productViewportPanel.BorderStyle = BorderStyle.None;
            productCard.Controls.Add(productViewportPanel);

            productButtonsPanel = new SmoothFlowLayoutPanel();
            productButtonsPanel.Left = 0;
            productButtonsPanel.Top = 0;
            productButtonsPanel.Width = 390;
            productButtonsPanel.Height = 345;
            productButtonsPanel.AutoScroll = false;
            productButtonsPanel.WrapContents = true;
            productButtonsPanel.BackColor = UiTheme.CardAlt;
            productButtonsPanel.Padding = new Padding(2);
            productViewportPanel.Controls.Add(productButtonsPanel);

            productScrollBar = new VScrollBar();
            productScrollBar.Left = 570;
            productScrollBar.Top = 158;
            productScrollBar.Width = 44;
            productScrollBar.Height = 345;
            productScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            productScrollBar.SmallChange = 48;
            productScrollBar.LargeChange = 144;
            productScrollBar.Scroll += delegate(object sender, ScrollEventArgs e)
            {
                ScrollProductsTo(e.NewValue);
            };
            productCard.Controls.Add(productScrollBar);

            Panel checkoutCard = UiTheme.CardPanel();
            checkoutCard.Dock = DockStyle.Fill;
            checkoutCard.Padding = new Padding(18);
            checkoutCard.AutoScroll = true;
            shell.Controls.Add(checkoutCard, 1, 0);

            checkoutCard.Controls.Add(CardTitle("Ticket en cours", 18, 14));

            cartGrid = UiTheme.Grid();
            cartGrid.Left = 18;
            cartGrid.Top = 56;
            cartGrid.Width = 410;
            cartGrid.Height = 160;
            cartGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkoutCard.Controls.Add(cartGrid);

            Button removeButton = UiTheme.SecondaryButton(Localization.T("Remove"));
            removeButton.Name = "removeButton";
            removeButton.Left = 18;
            removeButton.Top = 234;
            removeButton.Width = 96;
            removeButton.Height = 42;
            removeButton.Click += delegate { RemoveSelectedCartItem(); };
            checkoutCard.Controls.Add(removeButton);

            decreaseQuantityButton = UiTheme.SecondaryButton(Localization.T("Decrease"));
            decreaseQuantityButton.Name = "decreaseQuantityButton";
            decreaseQuantityButton.Left = 122;
            decreaseQuantityButton.Top = 234;
            decreaseQuantityButton.Width = 92;
            decreaseQuantityButton.Height = 42;
            decreaseQuantityButton.Click += delegate { ChangeSelectedQuantity(-1); };
            checkoutCard.Controls.Add(decreaseQuantityButton);

            increaseQuantityButton = UiTheme.SecondaryButton(Localization.T("Increase"));
            increaseQuantityButton.Name = "increaseQuantityButton";
            increaseQuantityButton.Left = 222;
            increaseQuantityButton.Top = 234;
            increaseQuantityButton.Width = 92;
            increaseQuantityButton.Height = 42;
            increaseQuantityButton.Click += delegate { ChangeSelectedQuantity(1); };
            checkoutCard.Controls.Add(increaseQuantityButton);

            clearCartButton = UiTheme.SecondaryButton(Localization.T("ClearCart"));
            clearCartButton.Name = "clearCartButton";
            clearCartButton.Left = 322;
            clearCartButton.Top = 234;
            clearCartButton.Width = 116;
            clearCartButton.Height = 42;
            clearCartButton.Click += delegate { cart.Clear(); RefreshCart(); };
            checkoutCard.Controls.Add(clearCartButton);

            employeeDiscountCheckBox = new CheckBox();
            employeeDiscountCheckBox.Name = "employeeDiscountCheckBox";
            employeeDiscountCheckBox.Left = 18;
            employeeDiscountCheckBox.Top = 284;
            employeeDiscountCheckBox.Width = 230;
            employeeDiscountCheckBox.Height = 34;
            employeeDiscountCheckBox.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            employeeDiscountCheckBox.ForeColor = UiTheme.Text;
            employeeDiscountCheckBox.CheckedChanged += delegate { ApplyEmployeeDiscount(); };
            checkoutCard.Controls.Add(employeeDiscountCheckBox);

            AddLabel(checkoutCard, "discountLabel", Localization.T("Discount"), 18, 316);
            saleDiscountInput = MoneyInput();
            saleDiscountInput.Left = 18;
            saleDiscountInput.Top = 342;
            saleDiscountInput.Width = 130;
            saleDiscountInput.ValueChanged += delegate { RefreshCart(); };
            checkoutCard.Controls.Add(saleDiscountInput);

            AddLabel(checkoutCard, "paymentLabel", Localization.T("Payment"), 150, 316);
            paymentComboBox = new ComboBox();
            paymentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentComboBox.Items.AddRange(new object[] { "Cash", "Card", "Online", "In store" });
            paymentComboBox.SelectedIndex = 0;
            paymentComboBox.Left = 150;
            paymentComboBox.Top = 342;
            paymentComboBox.Width = 130;
            paymentComboBox.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            paymentComboBox.BackColor = Color.White;
            paymentComboBox.ForeColor = UiTheme.Text;
            checkoutCard.Controls.Add(paymentComboBox);

            returnCheckBox = Check(Localization.T("Return"), "returnCheckBox", 250, 284);
            checkoutCard.Controls.Add(returnCheckBox);

            debtCheckBox = Check(Localization.T("Debt"), "debtCheckBox", 340, 284);
            checkoutCard.Controls.Add(debtCheckBox);

            AddLabel(checkoutCard, "customerLabel", Localization.T("Customer"), 300, 316);
            customerTextBox = UiTheme.TextInput(300, 342, 135);
            checkoutCard.Controls.Add(customerTextBox);

            totalLabel = new Label();
            totalLabel.Left = 18;
            totalLabel.Top = 372;
            totalLabel.Width = 410;
            totalLabel.Height = 36;
            totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            totalLabel.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            totalLabel.ForeColor = UiTheme.Accent;
            checkoutCard.Controls.Add(totalLabel);

            Button checkoutButton = UiTheme.PrimaryButton(Localization.T("Checkout"));
            checkoutButton.Name = "checkoutButton";
            checkoutButton.Left = 18;
            checkoutButton.Top = 410;
            checkoutButton.Width = 410;
            checkoutButton.Height = 48;
            checkoutButton.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            checkoutButton.Click += delegate { Checkout(); };
            checkoutCard.Controls.Add(checkoutButton);

            printReceiptButton = UiTheme.SecondaryButton(Localization.T("PrintTicket"));
            printReceiptButton.Name = "printReceiptButton";
            printReceiptButton.Left = 18;
            printReceiptButton.Top = 466;
            printReceiptButton.Width = 410;
            printReceiptButton.Height = 44;
            printReceiptButton.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            printReceiptButton.Click += delegate { PrintLastReceipt(); };
            checkoutCard.Controls.Add(printReceiptButton);
        }

        private void BuildProductsTab()
        {
            TabPage tab = NewTab("productsTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            tab.Controls.Add(shell);

            Panel listCard = UiTheme.CardPanel();
            listCard.Dock = DockStyle.Fill;
            listCard.Padding = new Padding(18);
            listCard.AutoScroll = true;
            shell.Controls.Add(listCard, 0, 0);
            listCard.Controls.Add(CardTitle("Stock par magasin", 18, 14));

            stockGrid = UiTheme.Grid();
            stockGrid.Left = 18;
            stockGrid.Top = 58;
            stockGrid.Width = 650;
            stockGrid.Height = 450;
            stockGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            stockGrid.CellClick += delegate { LoadSelectedProduct(); };
            listCard.Controls.Add(stockGrid);

            Panel form = UiTheme.CardPanel();
            form.Dock = DockStyle.Fill;
            form.Padding = new Padding(22);
            form.AutoScroll = true;
            shell.Controls.Add(form, 1, 0);
            form.Controls.Add(CardTitle("Fiche produit", 22, 18));

            productNameInput = AddTextField(form, "productNameLabel", 22, 76, 410);
            productCategoryInput = AddTextField(form, "productCategoryLabel", 22, 142, 410);
            barcodeInput = AddTextField(form, "barcodeLabel", 22, 208, 410);
            purchasePriceInput = AddMoneyField(form, "purchaseLabel", 22, 274, 185);
            salePriceInput = AddMoneyField(form, "salePriceLabel", 245, 274, 185);
            taxInput = AddMoneyField(form, "taxLabel", 22, 340, 125);
            quantityInput = AddNumberField(form, "quantityLabel", 170, 340, 125);
            minimumInput = AddNumberField(form, "minimumLabel", 318, 340, 125);

            AddLabel(form, "expiryLabel", Localization.T("Expiry"), 22, 406);
            expiryInput = new DateTimePicker();
            expiryInput.Left = 22;
            expiryInput.Top = 432;
            expiryInput.Width = 160;
            expiryInput.Format = DateTimePickerFormat.Short;
            form.Controls.Add(expiryInput);

            AddLabel(form, "storeFieldLabel", "Store", 245, 406);
            productStoreInput = new ComboBox();
            productStoreInput.DropDownStyle = ComboBoxStyle.DropDownList;
            productStoreInput.Left = 245;
            productStoreInput.Top = 432;
            productStoreInput.Width = 198;
            foreach (Store item in store.Database.Stores) productStoreInput.Items.Add(item.Id);
            productStoreInput.SelectedIndex = 0;
            form.Controls.Add(productStoreInput);

            Button save = UiTheme.PrimaryButton(Localization.T("SaveProduct"));
            save.Name = "saveProductButton";
            save.Left = 22;
            save.Top = 486;
            save.Width = 175;
            save.Height = 46;
            save.Click += delegate { SaveProduct(); };
            form.Controls.Add(save);

            Button delete = UiTheme.SecondaryButton(Localization.T("DeleteProduct"));
            delete.Name = "deleteProductButton";
            delete.Left = 220;
            delete.Top = 486;
            delete.Width = 175;
            delete.Height = 46;
            delete.Click += delegate { DeleteSelectedProduct(); };
            form.Controls.Add(delete);
        }

        private void BuildCashTab()
        {
            TabPage tab = NewTab("cashTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            tab.Controls.Add(shell);

            Panel sessionCard = UiTheme.CardPanel();
            sessionCard.Dock = DockStyle.Fill;
            sessionCard.Padding = new Padding(24);
            shell.Controls.Add(sessionCard, 0, 0);
            sessionCard.Controls.Add(CardTitle("Cloture de caisse", 24, 20));

            cashStatusLabel = new Label();
            cashStatusLabel.Left = 24;
            cashStatusLabel.Top = 74;
            cashStatusLabel.Width = 580;
            cashStatusLabel.Height = 42;
            cashStatusLabel.Font = UiTheme.FontLarge;
            cashStatusLabel.ForeColor = UiTheme.Text;
            sessionCard.Controls.Add(cashStatusLabel);

            cashKpiLabel = new Label();
            cashKpiLabel.Left = 24;
            cashKpiLabel.Top = 132;
            cashKpiLabel.Width = 580;
            cashKpiLabel.Height = 110;
            cashKpiLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            cashKpiLabel.ForeColor = UiTheme.Muted;
            sessionCard.Controls.Add(cashKpiLabel);

            Button openButton = UiTheme.PrimaryButton(Localization.T("OpenShift"));
            openButton.Name = "openShiftButton";
            openButton.Left = 24;
            openButton.Top = 265;
            openButton.Width = 220;
            openButton.Height = 54;
            openButton.Click += delegate { store.OpenSession(user.Username, activeStoreId, 200m); RefreshAll(); };
            sessionCard.Controls.Add(openButton);

            AddLabel(sessionCard, "countedLabel", Localization.T("CountedCash"), 24, 360);
            countedCashInput = MoneyInput();
            countedCashInput.Left = 24;
            countedCashInput.Top = 388;
            countedCashInput.Width = 180;
            sessionCard.Controls.Add(countedCashInput);

            Button closeButton = UiTheme.SecondaryButton(Localization.T("CloseShift"));
            closeButton.Name = "closeShiftButton";
            closeButton.Left = 230;
            closeButton.Top = 384;
            closeButton.Width = 220;
            closeButton.Height = 48;
            closeButton.Click += delegate { CloseShift(); };
            sessionCard.Controls.Add(closeButton);

            Panel movementCard = UiTheme.CardPanel();
            movementCard.Dock = DockStyle.Fill;
            movementCard.Padding = new Padding(24);
            shell.Controls.Add(movementCard, 1, 0);
            movementCard.Controls.Add(CardTitle("Mouvements caisse", 24, 20));

            AddLabel(movementCard, "movementTypeLabel", "Type", 24, 84);
            movementTypeInput = new ComboBox();
            movementTypeInput.DropDownStyle = ComboBoxStyle.DropDownList;
            movementTypeInput.Items.AddRange(new object[] { "Withdrawal", "Deposit" });
            movementTypeInput.SelectedIndex = 0;
            movementTypeInput.Left = 24;
            movementTypeInput.Top = 110;
            movementTypeInput.Width = 200;
            movementCard.Controls.Add(movementTypeInput);

            AddLabel(movementCard, "movementAmountLabel", "Montant", 250, 84);
            movementAmountInput = MoneyInput();
            movementAmountInput.Left = 250;
            movementAmountInput.Top = 110;
            movementAmountInput.Width = 150;
            movementCard.Controls.Add(movementAmountInput);

            AddLabel(movementCard, "movementReasonLabel", Localization.T("Reason"), 24, 174);
            movementReasonInput = UiTheme.TextInput(24, 202, 376);
            movementCard.Controls.Add(movementReasonInput);

            Button movementButton = UiTheme.PrimaryButton(Localization.T("Add"));
            movementButton.Name = "addMovementButton";
            movementButton.Left = 24;
            movementButton.Top = 270;
            movementButton.Width = 376;
            movementButton.Height = 52;
            movementButton.Click += delegate { AddMovement(); };
            movementCard.Controls.Add(movementButton);
        }

        private void BuildReportsTab()
        {
            TabPage tab = NewTab("reportsTab");
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            tab.Controls.Add(card);

            card.Controls.Add(CardTitle("Rapport magasin", 18, 14));
            reportTextBox = new TextBox();
            reportTextBox.Multiline = true;
            reportTextBox.ReadOnly = true;
            reportTextBox.ScrollBars = ScrollBars.Vertical;
            reportTextBox.Font = new Font("Consolas", 11);
            reportTextBox.Left = 18;
            reportTextBox.Top = 58;
            reportTextBox.Width = 1040;
            reportTextBox.Height = 610;
            reportTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(reportTextBox);
        }

        private void BuildSettingsTab()
        {
            TabPage tab = NewTab("settingsTab");
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(28);
            tab.Controls.Add(card);

            Label title = CardTitle("Systeme", 28, 24);
            card.Controls.Add(title);
            Label body = new Label();
            body.Text = "Oly Drugstore POS\n\n- Donnees locales hors ligne\n- Sauvegarde automatique a la cloture de caisse\n- Pret pour scanner code a barres USB\n- Ticket printer via imprimante Windows\n- Future sync backend pour boutique en ligne";
            body.Left = 28;
            body.Top = 76;
            body.Width = 720;
            body.Height = 240;
            body.Font = UiTheme.FontLarge;
            body.ForeColor = UiTheme.Muted;
            card.Controls.Add(body);
        }

        private TabPage NewTab(string name)
        {
            TabPage tab = new TabPage();
            tab.Name = name;
            tab.BackColor = UiTheme.Background;
            tab.Padding = new Padding(14);
            tab.AutoScroll = true;
            tabs.TabPages.Add(tab);
            return tab;
        }

        private TableLayoutPanel PageGrid(int columns, int rows)
        {
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = columns;
            grid.RowCount = rows;
            grid.Padding = new Padding(14);
            grid.BackColor = UiTheme.Background;
            return grid;
        }

        private Label CardTitle(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = "  " + text;
            label.Left = left;
            label.Top = top;
            label.Width = 360;
            label.Height = 32;
            label.Font = UiTheme.FontLarge;
            label.ForeColor = UiTheme.Text;
            label.BackColor = Color.Transparent;
            label.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (SolidBrush brush = new SolidBrush(UiTheme.Accent))
                {
                    e.Graphics.FillRectangle(brush, 0, 7, 4, 18);
                }
            };
            return label;
        }

        private void AddLabel(Control parent, string name, string text, int left, int top)
        {
            Label label = UiTheme.FieldLabel(text, left, top);
            label.Name = name;
            parent.Controls.Add(label);
        }

        private TextBox AddTextField(Control parent, string labelName, int left, int top, int width)
        {
            AddLabel(parent, labelName, "", left, top);
            TextBox input = UiTheme.TextInput(left, top + 26, width);
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown AddMoneyField(Control parent, string labelName, int left, int top)
        {
            return AddMoneyField(parent, labelName, left, top, 150);
        }

        private NumericUpDown AddMoneyField(Control parent, string labelName, int left, int top, int width)
        {
            AddLabel(parent, labelName, "", left, top);
            NumericUpDown input = MoneyInput();
            input.Left = left;
            input.Top = top + 26;
            input.Width = width;
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown AddNumberField(Control parent, string labelName, int left, int top)
        {
            return AddNumberField(parent, labelName, left, top, 150);
        }

        private NumericUpDown AddNumberField(Control parent, string labelName, int left, int top, int width)
        {
            AddLabel(parent, labelName, "", left, top);
            NumericUpDown input = new NumericUpDown();
            input.Minimum = 0;
            input.Maximum = 1000000;
            input.Left = left;
            input.Top = top + 26;
            input.Width = width;
            input.Font = new Font("Segoe UI", 12);
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown MoneyInput()
        {
            NumericUpDown input = new NumericUpDown();
            input.DecimalPlaces = 3;
            input.Minimum = 0;
            input.Maximum = 1000000;
            input.Font = new Font("Segoe UI", 12);
            return input;
        }

        private CheckBox Check(string text, string name, int left, int top)
        {
            CheckBox check = new CheckBox();
            check.Name = name;
            check.Text = text;
            check.Left = left;
            check.Top = top;
            check.Width = 160;
            check.Height = 34;
            check.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            check.ForeColor = UiTheme.Text;
            return check;
        }

        private void RefreshText()
        {
            if (tabs.TabPages.ContainsKey("salesTab")) tabs.TabPages["salesTab"].Text = Localization.T("Sales");
            if (tabs.TabPages.ContainsKey("productsTab")) tabs.TabPages["productsTab"].Text = Localization.T("Products");
            if (tabs.TabPages.ContainsKey("cashTab")) tabs.TabPages["cashTab"].Text = Localization.T("Cash");
            if (tabs.TabPages.ContainsKey("reportsTab")) tabs.TabPages["reportsTab"].Text = Localization.T("Reports");
            if (tabs.TabPages.ContainsKey("settingsTab")) tabs.TabPages["settingsTab"].Text = Localization.T("Settings");

            SetText("addButton", Localization.T("Add"));
            SetText("addQuantityLabel", Localization.T("Quantity"));
            SetText("removeButton", Localization.T("Remove"));
            SetText("increaseQuantityButton", Localization.T("Increase"));
            SetText("decreaseQuantityButton", Localization.T("Decrease"));
            SetText("clearCartButton", Localization.T("ClearCart"));
            SetText("checkoutButton", Localization.T("Checkout"));
            SetText("printReceiptButton", Localization.T("PrintTicket"));
            SetText("discountLabel", Localization.T("Discount"));
            SetText("employeeDiscountCheckBox", Localization.T("EmployeeDiscount"));
            SetText("paymentLabel", Localization.T("Payment"));
            SetText("returnCheckBox", Localization.T("Return"));
            SetText("debtCheckBox", Localization.T("Debt"));
            SetText("customerLabel", Localization.T("Customer"));
            SetText("productNameLabel", Localization.T("Name"));
            SetText("productCategoryLabel", Localization.T("Category"));
            SetText("barcodeLabel", Localization.T("Barcode"));
            SetText("purchaseLabel", Localization.T("PurchasePrice"));
            SetText("salePriceLabel", Localization.T("SalePrice"));
            SetText("taxLabel", Localization.T("Tax"));
            SetText("quantityLabel", Localization.T("Quantity"));
            SetText("minimumLabel", Localization.T("Minimum"));
            SetText("expiryLabel", Localization.T("Expiry"));
            SetText("saveProductButton", Localization.T("SaveProduct"));
            SetText("deleteProductButton", Localization.T("DeleteProduct"));
            SetText("openShiftButton", Localization.T("OpenShift"));
            SetText("closeShiftButton", Localization.T("CloseShift"));
            SetText("addMovementButton", Localization.T("Add"));
            SetText("countedLabel", Localization.T("CountedCash"));
            SetText("movementReasonLabel", Localization.T("Reason"));
            RefreshAll();
        }

        private void SetText(string name, string value)
        {
            Control[] matches = Controls.Find(name, true);
            if (matches.Length > 0) matches[0].Text = value;
        }

        private void RefreshAll()
        {
            RefreshProducts();
            RefreshCart();
            RefreshCash();
            RefreshReports();
        }

        private void RefreshProducts()
        {
            RefreshCategories();

            string query = searchTextBox == null ? "" : searchTextBox.Text.Trim();
            string normalizedQuery = query.ToLowerInvariant();
            List<Product> products = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .Where(p => activeCategory == "All" || p.Category == activeCategory)
                .Where(p => searchTextBox == null ||
                            string.IsNullOrEmpty(normalizedQuery) ||
                            p.Name.ToLowerInvariant().Contains(normalizedQuery) ||
                            p.Category.ToLowerInvariant().Contains(normalizedQuery) ||
                            (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(query)))
                .OrderBy(p => p.Name)
                .ToList();

            if (productButtonsPanel != null)
            {
                RenderProductButtons(products);
            }

            if (stockGrid != null)
            {
                SetRedraw(stockGrid, false);
                try
                {
                    stockGrid.DataSource = null;
                    stockGrid.DataSource = products.ToList();
                    FormatProductsGrid(stockGrid, true);
                }
                finally
                {
                    SetRedraw(stockGrid, true);
                }
            }
        }

        private void RefreshCategories()
        {
            if (categoryButtonsPanel == null)
            {
                return;
            }

            List<string> categories = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            if (activeCategory != "All" && !categories.Contains(activeCategory))
            {
                activeCategory = "All";
            }

            string renderKey = activeStoreId + "|" + activeCategory + "|" + string.Join(";", categories.ToArray());
            if (renderKey == lastCategoryRenderKey)
            {
                return;
            }
            lastCategoryRenderKey = renderKey;

            refreshingCategories = true;
            try
            {
                SetRedraw(categoryButtonsPanel, false);
                categoryButtonsPanel.SuspendLayout();
                categoryButtonsPanel.Controls.Clear();
                RenderCategoryButton("All");
                foreach (string category in categories)
                {
                    RenderCategoryButton(category);
                }
            }
            finally
            {
                categoryButtonsPanel.ResumeLayout(true);
                SetRedraw(categoryButtonsPanel, true);
                refreshingCategories = false;
            }
        }

        private void RenderCategoryButton(string category)
        {
            Button button = category == activeCategory
                ? UiTheme.PrimaryButton(category)
                : UiTheme.SecondaryButton(category);
            button.Width = category == "All" ? 86 : 132;
            button.Width = 130;
            button.Height = 56;
            button.Margin = new Padding(0, 0, 0, 10);
            button.Font = UiTheme.FontBold;
            button.FlatAppearance.BorderSize = activeCategory == category ? 0 : 1;
            button.Tag = category;
            button.Click += delegate(object sender, EventArgs e)
            {
                Button source = sender as Button;
                string selectedCategory = source == null ? "All" : source.Tag.ToString();
                if (refreshingCategories || activeCategory == selectedCategory)
                {
                    return;
                }

                activeCategory = selectedCategory;
                RefreshProducts();
            };
            categoryButtonsPanel.Controls.Add(button);
        }

        private void RenderProductButtons(List<Product> products)
        {
            string renderKey = activeStoreId + "|" + activeCategory + "|" +
                (searchTextBox == null ? "" : searchTextBox.Text.Trim()) + "|" +
                string.Join(";", products.Select(p => p.Id + ":" + p.Quantity + ":" + p.MinimumQuantity).ToArray());
            if (renderKey == lastProductRenderKey)
            {
                FitProductButtonsToViewport();
                return;
            }
            lastProductRenderKey = renderKey;

            SetRedraw(productButtonsPanel, false);
            productButtonsPanel.SuspendLayout();
            try
            {
                productButtonsPanel.Controls.Clear();

                foreach (Product product in products)
                {
                    Button button = UiTheme.SecondaryButton("");
                    button.Width = 160;
                    button.Height = 128;
                    button.Margin = new Padding(7);
                    Color productBackColor = product.Quantity <= product.MinimumQuantity
                        ? UiTheme.DangerSoft
                        : Color.White;
                    ThemedButton themedButton = button as ThemedButton;
                    if (themedButton != null)
                    {
                        themedButton.NormalBackColor = productBackColor;
                        themedButton.HoverBackColor = product.Quantity <= product.MinimumQuantity
                            ? Color.FromArgb(255, 237, 213)
                            : UiTheme.CardAlt;
                    }
                    else
                    {
                        button.BackColor = productBackColor;
                    }
                    button.FlatAppearance.BorderColor = product.Quantity <= product.MinimumQuantity
                        ? Color.FromArgb(253, 186, 116)
                        : UiTheme.Border;
                    button.ForeColor = UiTheme.Text;
                    button.Font = UiTheme.FontBold;
                    button.TextAlign = ContentAlignment.MiddleLeft;
                    button.Text =
                        product.Category.ToUpperInvariant() + "\n\n" +
                        product.Name + "\n" +
                        product.SalePrice.ToString("0.000") + " DT\n" +
                        "Stock: " + product.Quantity;
                    button.Tag = product;
                    button.Click += delegate(object sender, EventArgs e)
                    {
                        Button source = sender as Button;
                        Product selected = source == null ? null : source.Tag as Product;
                        if (selected != null)
                        {
                            AddProductToCart(!string.IsNullOrEmpty(selected.Barcode) ? selected.Barcode : selected.Name);
                        }
                    };
                    productButtonsPanel.Controls.Add(button);
                }

                productButtonsPanel.Top = 0;
                FitProductButtonsToViewport();
            }
            finally
            {
                productButtonsPanel.ResumeLayout(true);
                SetRedraw(productButtonsPanel, true);
            }
        }

        private void UpdateProductScrollBar()
        {
            if (productButtonsPanel == null || productViewportPanel == null || productScrollBar == null)
            {
                return;
            }

            int scrollableHeight = Math.Max(0, productButtonsPanel.Height - productViewportPanel.Height);
            productScrollBar.Enabled = scrollableHeight > 0;
            productScrollBar.Minimum = 0;
            productScrollBar.LargeChange = 144;
            productScrollBar.SmallChange = 48;
            productScrollBar.Maximum = scrollableHeight + productScrollBar.LargeChange - 1;
            int current = Math.Min(scrollableHeight, Math.Max(0, -productButtonsPanel.Top));
            productScrollBar.Value = Math.Min(productScrollBar.Maximum - productScrollBar.LargeChange + 1, current);
        }

        private void ScrollProductsTo(int value)
        {
            if (productButtonsPanel == null || productViewportPanel == null || productScrollBar == null)
            {
                return;
            }

            int scrollableHeight = Math.Max(0, productButtonsPanel.Height - productViewportPanel.Height);
            int target = Math.Min(scrollableHeight, Math.Max(0, value));
            productButtonsPanel.Top = -target;
        }

        private void SyncProductScrollBarFromPanel()
        {
            if (productButtonsPanel == null || productViewportPanel == null || productScrollBar == null || !productScrollBar.Enabled)
            {
                return;
            }

            int scrollableHeight = Math.Max(0, productButtonsPanel.Height - productViewportPanel.Height);
            int current = Math.Min(scrollableHeight, Math.Max(0, -productButtonsPanel.Top));
            int maxValue = Math.Max(productScrollBar.Minimum, productScrollBar.Maximum - productScrollBar.LargeChange + 1);
            productScrollBar.Value = Math.Min(maxValue, current);
        }

        private void ApplyResponsiveLayout()
        {
            LayoutSalesScreen();
            LayoutProductsScreen();
        }

        private void ScheduleResponsiveLayout()
        {
            if (responsiveLayoutTimer == null)
            {
                ApplyResponsiveLayout();
                return;
            }

            responsiveLayoutTimer.Stop();
            responsiveLayoutTimer.Start();
        }

        private void SetRedraw(Control control, bool enabled)
        {
            if (control == null || !control.IsHandleCreated)
            {
                return;
            }

            SendMessage(control.Handle, WM_SETREDRAW, enabled ? new IntPtr(1) : IntPtr.Zero, IntPtr.Zero);
            if (enabled)
            {
                control.Invalidate(true);
                control.Update();
            }
        }

        private void LayoutSalesScreen()
        {
            if (productViewportPanel == null || productButtonsPanel == null || productScrollBar == null || cartGrid == null)
            {
                return;
            }

            Control productCard = productViewportPanel.Parent;
            Control checkoutCard = cartGrid.Parent;
            if (productCard == null || checkoutCard == null || productCard.ClientSize.Width <= 0 || checkoutCard.ClientSize.Width <= 0)
            {
                return;
            }

            int pad = 18;
            int productWidth = productCard.ClientSize.Width;
            int productHeight = productCard.ClientSize.Height;
            int fieldTop = 84;
            int gap = 8;
            Control quantityDownButton = FindNamedControl(productCard, "quantityDownButton");
            Control quantityUpButton = FindNamedControl(productCard, "quantityUpButton");
            Control addButton = FindNamedControl(productCard, "addButton");
            if (quantityDownButton != null && quantityUpButton != null && addButton != null && searchTextBox != null)
            {
                addButton.Width = productWidth < 560 ? 104 : 130;
                addButton.Left = Math.Max(pad, productWidth - pad - addButton.Width);
                addButton.Top = fieldTop;
                quantityUpButton.Left = addButton.Left - gap - quantityUpButton.Width;
                quantityUpButton.Top = fieldTop;
                quantityDownButton.Left = quantityUpButton.Left - gap - quantityDownButton.Width;
                quantityDownButton.Top = fieldTop;
                addQuantityInput.Left = quantityDownButton.Left - gap - addQuantityInput.Width;
                addQuantityInput.Top = fieldTop;
                searchTextBox.Left = pad;
                searchTextBox.Top = fieldTop;
                searchTextBox.Width = Math.Max(160, addQuantityInput.Left - pad - gap);
                Control scannerLabel = FindNamedControl(productCard, "scannerLabel");
                if (scannerLabel != null)
                {
                    scannerLabel.Left = pad;
                    scannerLabel.Width = searchTextBox.Width;
                }

                Control quantityLabel = FindNamedControl(productCard, "addQuantityLabel");
                if (quantityLabel != null)
                {
                    quantityLabel.Left = addQuantityInput.Left;
                    quantityLabel.Top = 52;
                    quantityLabel.Width = 140;
                }
            }

            int mainTop = 158;
            int mainHeight = Math.Max(240, productHeight - mainTop - pad);
            int categoryWidth = Math.Min(170, Math.Max(118, productWidth / 4));
            categoryButtonsPanel.Left = pad;
            categoryButtonsPanel.Top = mainTop;
            categoryButtonsPanel.Width = categoryWidth;
            categoryButtonsPanel.Height = mainHeight;

            productScrollBar.Width = 44;
            productScrollBar.Left = Math.Max(categoryButtonsPanel.Right + 130, productWidth - pad - productScrollBar.Width);
            productScrollBar.Top = mainTop;
            productScrollBar.Height = mainHeight;

            productViewportPanel.Left = categoryButtonsPanel.Right + 10;
            productViewportPanel.Top = mainTop;
            productViewportPanel.Width = Math.Max(150, productScrollBar.Left - productViewportPanel.Left - 10);
            productViewportPanel.Height = mainHeight;
            productButtonsPanel.Width = productViewportPanel.Width;
            FitProductButtonsToViewport();

            int checkoutWidth = Math.Max(300, checkoutCard.ClientSize.Width - (pad * 2));
            int checkoutHeight = checkoutCard.ClientSize.Height;
            bool compactCheckout = checkoutHeight < 500;
            cartGrid.Left = pad;
            cartGrid.Top = 56;
            cartGrid.Width = checkoutWidth;
            cartGrid.Height = compactCheckout ? 92 : Math.Max(120, Math.Min(210, checkoutHeight - 380));
            FitCartGridColumns();

            int actionsTop = cartGrid.Bottom + (compactCheckout ? 8 : 18);
            LayoutCheckoutActions(checkoutCard, checkoutWidth, actionsTop);
            int inputTop = actionsTop + (checkoutWidth >= 420 ? 74 : 112);
            LayoutCheckoutInputs(checkoutWidth, inputTop, checkoutHeight);
        }

        private void LayoutCheckoutActions(Control checkoutCard, int contentWidth, int top)
        {
            Control removeButton = FindNamedControl(checkoutCard, "removeButton");
            Control[] buttons = new Control[] { removeButton, decreaseQuantityButton, increaseQuantityButton, clearCartButton };
            if (buttons.Any(button => button == null))
            {
                return;
            }

            int pad = 18;
            int gap = 8;
            if (contentWidth >= 420)
            {
                int buttonWidth = (contentWidth - (gap * 3)) / 4;
                for (int index = 0; index < buttons.Length; index++)
                {
                    buttons[index].Left = pad + (index * (buttonWidth + gap));
                    buttons[index].Top = top;
                    buttons[index].Width = buttonWidth;
                    buttons[index].Height = 46;
                }
                return;
            }

            int smallWidth = (contentWidth - gap) / 2;
            for (int index = 0; index < buttons.Length; index++)
            {
                buttons[index].Left = pad + ((index % 2) * (smallWidth + gap));
                buttons[index].Top = top + ((index / 2) * 52);
                buttons[index].Width = smallWidth;
                buttons[index].Height = 46;
            }
        }

        private void LayoutCheckoutInputs(int contentWidth, int inputTop, int checkoutHeight)
        {
            int pad = 18;
            int gap = 10;
            int printTop = Math.Max(inputTop + 178, checkoutHeight - 62);
            int checkoutTop = printTop - 56;
            int totalTop = checkoutTop - 42;
            int fieldsTop = Math.Min(inputTop, totalTop - 68);
            if (checkoutHeight < 500)
            {
                fieldsTop = inputTop;
                totalTop = fieldsTop + 48;
                checkoutTop = totalTop + 36;
                printTop = checkoutTop + 50;
            }
            if (fieldsTop < cartGrid.Bottom + 66)
            {
                fieldsTop = cartGrid.Bottom + 66;
                totalTop = fieldsTop + 62;
                checkoutTop = totalTop + 42;
                printTop = checkoutTop + 56;
            }

            employeeDiscountCheckBox.Left = pad;
            employeeDiscountCheckBox.Top = fieldsTop - 58;
            employeeDiscountCheckBox.Width = Math.Min(160, contentWidth / 2);
            if (contentWidth < 380)
            {
                returnCheckBox.Left = pad + employeeDiscountCheckBox.Width + gap;
                returnCheckBox.Top = fieldsTop - 58;
                returnCheckBox.Width = Math.Max(90, contentWidth - employeeDiscountCheckBox.Width - gap);
                debtCheckBox.Left = pad;
                debtCheckBox.Top = fieldsTop - 32;
                debtCheckBox.Width = 100;
            }
            else
            {
                returnCheckBox.Left = pad + employeeDiscountCheckBox.Width + gap;
                returnCheckBox.Top = fieldsTop - 58;
                returnCheckBox.Width = 96;
                debtCheckBox.Left = returnCheckBox.Right + gap;
                debtCheckBox.Top = fieldsTop - 58;
                debtCheckBox.Width = 90;
            }

            int columnWidth = Math.Max(92, (contentWidth - (gap * 2)) / 3);
            MoveLabel("discountLabel", pad, fieldsTop - 26, columnWidth);
            saleDiscountInput.Left = pad;
            saleDiscountInput.Top = fieldsTop;
            saleDiscountInput.Width = columnWidth;

            MoveLabel("paymentLabel", pad + columnWidth + gap, fieldsTop - 26, columnWidth);
            paymentComboBox.Left = pad + columnWidth + gap;
            paymentComboBox.Top = fieldsTop;
            paymentComboBox.Width = columnWidth;

            MoveLabel("customerLabel", pad + (columnWidth + gap) * 2, fieldsTop - 26, columnWidth);
            customerTextBox.Left = pad + (columnWidth + gap) * 2;
            customerTextBox.Top = fieldsTop;
            customerTextBox.Width = contentWidth - (columnWidth + gap) * 2;

            totalLabel.Left = pad;
            totalLabel.Top = totalTop;
            totalLabel.Width = contentWidth;
            printReceiptButton.Left = pad;
            printReceiptButton.Top = printTop;
            printReceiptButton.Width = contentWidth;
            Control checkoutButton = FindNamedControl(printReceiptButton.Parent, "checkoutButton");
            if (checkoutButton != null)
            {
                checkoutButton.Left = pad;
                checkoutButton.Top = checkoutTop;
                checkoutButton.Width = contentWidth;
            }
        }

        private void FitProductButtonsToViewport()
        {
            if (productButtonsPanel == null || productViewportPanel == null)
            {
                return;
            }

            int available = Math.Max(140, productViewportPanel.ClientSize.Width - 18);
            int tileWidth = available >= 360 ? (available / 2) - 18 : available - 14;
            tileWidth = Math.Max(132, Math.Min(190, tileWidth));
            foreach (Control control in productButtonsPanel.Controls)
            {
                control.Width = tileWidth;
            }

            productButtonsPanel.Height = Math.Max(productViewportPanel.Height, productButtonsPanel.PreferredSize.Height + 12);
            UpdateProductScrollBar();
        }

        private void LayoutProductsScreen()
        {
            if (stockGrid == null || productNameInput == null)
            {
                return;
            }

            Control listCard = stockGrid.Parent;
            Control form = productNameInput.Parent;
            if (listCard == null || form == null)
            {
                return;
            }

            int pad = 18;
            stockGrid.Left = pad;
            stockGrid.Top = 58;
            stockGrid.Width = Math.Max(300, listCard.ClientSize.Width - (pad * 2));
            stockGrid.Height = Math.Max(250, listCard.ClientSize.Height - 76);
            FitProductsGridColumns();

            int formPad = 22;
            int contentWidth = Math.Max(260, form.ClientSize.Width - (formPad * 2));
            productNameInput.Left = formPad;
            productNameInput.Width = contentWidth;
            productCategoryInput.Left = formPad;
            productCategoryInput.Width = contentWidth;
            barcodeInput.Left = formPad;
            barcodeInput.Width = contentWidth;
            MoveLabel("productNameLabel", formPad, 76, contentWidth);
            MoveLabel("productCategoryLabel", formPad, 142, contentWidth);
            MoveLabel("barcodeLabel", formPad, 208, contentWidth);

            int twoColumnWidth = Math.Max(118, (contentWidth - 16) / 2);
            LayoutField(form, "purchaseLabel", purchasePriceInput, formPad, 274, twoColumnWidth);
            LayoutField(form, "salePriceLabel", salePriceInput, formPad + twoColumnWidth + 16, 274, twoColumnWidth);

            int threeColumnWidth = Math.Max(82, (contentWidth - 24) / 3);
            LayoutField(form, "taxLabel", taxInput, formPad, 340, threeColumnWidth);
            LayoutField(form, "quantityLabel", quantityInput, formPad + threeColumnWidth + 12, 340, threeColumnWidth);
            LayoutField(form, "minimumLabel", minimumInput, formPad + ((threeColumnWidth + 12) * 2), 340, threeColumnWidth);

            int bottomWidth = Math.Max(120, (contentWidth - 16) / 2);
            MoveLabel("expiryLabel", formPad, 406, bottomWidth);
            expiryInput.Left = formPad;
            expiryInput.Width = bottomWidth;
            MoveLabel("storeFieldLabel", formPad + bottomWidth + 16, 406, bottomWidth);
            productStoreInput.Left = formPad + bottomWidth + 16;
            productStoreInput.Width = bottomWidth;

            Control saveButton = FindNamedControl(form, "saveProductButton");
            Control deleteButton = FindNamedControl(form, "deleteProductButton");
            if (saveButton != null && deleteButton != null)
            {
                int buttonWidth = Math.Max(120, (contentWidth - 16) / 2);
                saveButton.Left = formPad;
                saveButton.Width = buttonWidth;
                deleteButton.Left = formPad + buttonWidth + 16;
                deleteButton.Width = buttonWidth;
            }
        }

        private void LayoutField(Control parent, string labelName, Control input, int left, int top, int width)
        {
            MoveLabel(labelName, left, top, width);
            input.Left = left;
            input.Top = top + 26;
            input.Width = width;
        }

        private void MoveLabel(string name, int left, int top, int width)
        {
            Control label = FindNamedControl(this, name);
            if (label == null)
            {
                return;
            }

            label.Left = left;
            label.Top = top;
            label.Width = width;
        }

        private Control FindNamedControl(Control parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Control[] matches = parent.Controls.Find(name, true);
            return matches.Length == 0 ? null : matches[0];
        }

        private void HandleSearchChanged()
        {
            if (suppressSearchAutoAdd)
            {
                return;
            }

            string query = searchTextBox.Text.Trim();
            if (query.Length >= 6)
            {
                Product exact = store.Database.Products.FirstOrDefault(p =>
                    p.StoreId == activeStoreId &&
                    !string.IsNullOrEmpty(p.Barcode) &&
                    p.Barcode == query);

                if (exact != null)
                {
                    AddProductToCart(query);
                    return;
                }
            }

            RefreshProducts();
        }

        private void AddProductToCart(string query)
        {
            Product product = store.FindProduct(query);
            if (product == null || product.StoreId != activeStoreId)
            {
                MessageBox.Show("Product not found");
                return;
            }

            int quantityToAdd = addQuantityInput == null ? 1 : (int)addQuantityInput.Value;
            SaleItem existing = cart.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null) existing.Quantity += quantityToAdd;
            else
            {
                cart.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = quantityToAdd,
                    UnitPrice = product.SalePrice,
                    Discount = 0,
                    TaxRate = product.TaxRate
                });
            }

            suppressSearchAutoAdd = true;
            searchTextBox.Text = "";
            suppressSearchAutoAdd = false;
            if (addQuantityInput != null) addQuantityInput.Value = 1;
            RefreshProducts();
            RefreshCart();
        }

        private void ChangeAddQuantity(int change)
        {
            if (addQuantityInput == null)
            {
                return;
            }

            decimal next = addQuantityInput.Value + change;
            if (next < addQuantityInput.Minimum) next = addQuantityInput.Minimum;
            if (next > addQuantityInput.Maximum) next = addQuantityInput.Maximum;
            addQuantityInput.Value = next;
        }

        private void RemoveSelectedCartItem()
        {
            if (cartGrid.CurrentRow == null) return;
            SaleItem item = cartGrid.CurrentRow.DataBoundItem as SaleItem;
            if (item != null) cart.Remove(item);
            RefreshCart();
        }

        private void ChangeSelectedQuantity(int change)
        {
            if (cartGrid.CurrentRow == null) return;
            SaleItem item = cartGrid.CurrentRow.DataBoundItem as SaleItem;
            if (item == null) return;

            item.Quantity += change;
            if (item.Quantity <= 0)
            {
                cart.Remove(item);
            }
            RefreshCart();
        }

        private void ApplyEmployeeDiscount()
        {
            if (employeeDiscountCheckBox.Checked)
            {
                decimal subtotal = cart.Sum(i => i.LineTotal);
                saleDiscountInput.Value = Math.Min(saleDiscountInput.Maximum, subtotal * 0.10m);
            }
            RefreshCart();
        }

        private void RefreshCart()
        {
            if (cartGrid != null)
            {
                SetRedraw(cartGrid, false);
                try
                {
                    cartGrid.DataSource = null;
                    cartGrid.DataSource = cart.ToList();
                    FormatCartGrid();
                }
                finally
                {
                    SetRedraw(cartGrid, true);
                }
            }

            decimal discount = saleDiscountInput != null ? saleDiscountInput.Value : 0;
            decimal total = Math.Max(0, cart.Sum(i => i.LineTotal) - discount);
            if (totalLabel != null) totalLabel.Text = Localization.T("Total") + ": " + total.ToString("0.000") + " DT";
        }

        private void Checkout()
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Cart is empty");
                return;
            }

            if (store.GetOpenSession(user.Username) == null)
            {
                MessageBox.Show("Open a cash session first");
                tabs.SelectedTab = tabs.TabPages["cashTab"];
                return;
            }

            List<SaleItem> items = cart.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                TaxRate = i.TaxRate
            }).ToList();

            lastCompletedSale = store.SaveSale(user, activeStoreId, items, paymentComboBox.SelectedItem.ToString(), saleDiscountInput.Value, returnCheckBox.Checked, debtCheckBox.Checked, customerTextBox.Text);

            cart.Clear();
            saleDiscountInput.Value = 0;
            customerTextBox.Text = "";
            returnCheckBox.Checked = false;
            debtCheckBox.Checked = false;
            employeeDiscountCheckBox.Checked = false;
            RefreshAll();
        }

        private void PrintLastReceipt()
        {
            Sale sale = lastCompletedSale;
            if (sale == null)
            {
                sale = store.Database.Sales
                    .Where(s => s.CashierUsername == user.Username && s.StoreId == activeStoreId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefault();
            }

            if (sale == null)
            {
                MessageBox.Show("No completed sale to print");
                return;
            }

            PrintReceipt(sale);
        }

        private void LoadSelectedProduct()
        {
            if (stockGrid.CurrentRow == null) return;
            selectedProduct = stockGrid.CurrentRow.DataBoundItem as Product;
            if (selectedProduct == null) return;

            productNameInput.Text = selectedProduct.Name;
            productCategoryInput.Text = selectedProduct.Category;
            barcodeInput.Text = selectedProduct.Barcode;
            purchasePriceInput.Value = selectedProduct.PurchasePrice;
            salePriceInput.Value = selectedProduct.SalePrice;
            taxInput.Value = selectedProduct.TaxRate;
            quantityInput.Value = selectedProduct.Quantity;
            minimumInput.Value = selectedProduct.MinimumQuantity;
            expiryInput.Value = selectedProduct.ExpiryDate < expiryInput.MinDate ? DateTime.Today : selectedProduct.ExpiryDate;
            productStoreInput.SelectedItem = selectedProduct.StoreId;
        }

        private void SaveProduct()
        {
            Product product = selectedProduct ?? new Product();
            product.Name = productNameInput.Text.Trim();
            product.Category = productCategoryInput.Text.Trim();
            product.Barcode = barcodeInput.Text.Trim();
            product.PurchasePrice = purchasePriceInput.Value;
            product.SalePrice = salePriceInput.Value;
            product.TaxRate = taxInput.Value;
            product.Quantity = (int)quantityInput.Value;
            product.MinimumQuantity = (int)minimumInput.Value;
            product.ExpiryDate = expiryInput.Value.Date;
            product.StoreId = productStoreInput.SelectedItem.ToString();
            store.SaveProduct(product);

            selectedProduct = null;
            ClearProductForm();
            RefreshAll();
        }

        private void DeleteSelectedProduct()
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Select a product first");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Delete product: " + selectedProduct.Name + "?",
                Localization.T("DeleteProduct"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            store.DeleteProduct(selectedProduct);
            selectedProduct = null;
            ClearProductForm();
            RefreshAll();
        }

        private void ClearProductForm()
        {
            productNameInput.Text = "";
            productCategoryInput.Text = "";
            barcodeInput.Text = "";
            purchasePriceInput.Value = 0;
            salePriceInput.Value = 0;
            taxInput.Value = 0;
            quantityInput.Value = 0;
            minimumInput.Value = 0;
            expiryInput.Value = DateTime.Today;
        }

        private void AddMovement()
        {
            CashSession session = store.GetOpenSession(user.Username);
            if (session == null)
            {
                MessageBox.Show("Open a cash session first");
                return;
            }

            store.AddMovement(session, movementTypeInput.SelectedItem.ToString(), movementAmountInput.Value, movementReasonInput.Text, user.Username);
            movementAmountInput.Value = 0;
            movementReasonInput.Text = "";
            RefreshAll();
        }

        private void CloseShift()
        {
            CashSession session = store.GetOpenSession(user.Username);
            if (session == null)
            {
                MessageBox.Show("No open session");
                return;
            }

            store.CloseSession(session, countedCashInput.Value);
            MessageBox.Show(Localization.T("BackupDone"));
            RefreshAll();
        }

        private void RefreshCash()
        {
            if (cashStatusLabel == null || cashKpiLabel == null)
            {
                if (sessionSummaryLabel != null) sessionSummaryLabel.Text = user.Role == UserRole.Admin ? "Gestion produits" : "";
                return;
            }

            CashSession session = store.GetOpenSession(user.Username);
            if (session == null)
            {
                cashStatusLabel.Text = "Caisse fermee";
                cashKpiLabel.Text = "Fond de caisse: 200.000 DT\nOuvrir une session avant de vendre.";
                sessionSummaryLabel.Text = "Caisse fermee";
                return;
            }

            decimal cashSales = store.CashSalesForSession(session);
            decimal deposits = session.Movements.Where(m => m.Type == "Deposit").Sum(m => m.Amount);
            decimal withdrawals = session.Movements.Where(m => m.Type == "Withdrawal").Sum(m => m.Amount);
            decimal expected = session.OpeningFund + cashSales + deposits - withdrawals;

            cashStatusLabel.Text = "Session ouverte depuis " + session.OpenedAt.ToString("g");
            cashKpiLabel.Text =
                "Fond: " + session.OpeningFund.ToString("0.000") + " DT\n" +
                "Ventes cash: " + cashSales.ToString("0.000") + " DT\n" +
                "Entrees: " + deposits.ToString("0.000") + " DT | Sorties: " + withdrawals.ToString("0.000") + " DT\n" +
                "Cash attendu: " + expected.ToString("0.000") + " DT";
            sessionSummaryLabel.Text = "Attendu: " + expected.ToString("0.000") + " DT";
        }

        private void FormatProductsGrid(DataGridView grid, bool adminView)
        {
            HideColumn(grid, "Id");
            HideColumn(grid, "StoreId");
            if (adminView)
            {
                HideColumn(grid, "Barcode");
                HideColumn(grid, "TaxRate");
                HideColumn(grid, "ExpiryDate");
            }
            if (!adminView)
            {
                HideColumn(grid, "PurchasePrice");
                HideColumn(grid, "TaxRate");
                HideColumn(grid, "MinimumQuantity");
                HideColumn(grid, "ExpiryDate");
            }

            RenameColumn(grid, "Name", Localization.T("Name"));
            RenameColumn(grid, "Category", Localization.T("Category"));
            RenameColumn(grid, "Barcode", Localization.T("Barcode"));
            RenameColumn(grid, "PurchasePrice", adminView ? "Achat" : Localization.T("PurchasePrice"));
            RenameColumn(grid, "SalePrice", adminView ? "Vente" : Localization.T("SalePrice"));
            RenameColumn(grid, "TaxRate", Localization.T("Tax"));
            RenameColumn(grid, "Quantity", adminView ? "Qty" : Localization.T("Quantity"));
            RenameColumn(grid, "MinimumQuantity", adminView ? "Min" : Localization.T("Minimum"));
            RenameColumn(grid, "ExpiryDate", Localization.T("Expiry"));

            AlignColumn(grid, "PurchasePrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "SalePrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "TaxRate", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "Quantity", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(grid, "MinimumQuantity", DataGridViewContentAlignment.MiddleCenter);

            SetColumnWidth(grid, "Name", 160);
            SetColumnWidth(grid, "Category", 92);
            SetColumnWidth(grid, "Barcode", 100);
            SetColumnWidth(grid, "PurchasePrice", 84);
            SetColumnWidth(grid, "SalePrice", 84);
            SetColumnWidth(grid, "TaxRate", 70);
            SetColumnWidth(grid, "Quantity", 74);
            SetColumnWidth(grid, "MinimumQuantity", 74);
            SetColumnWidth(grid, "ExpiryDate", 110);
            if (adminView)
            {
                FitProductsGridColumns();
            }
        }

        private void FitProductsGridColumns()
        {
            if (stockGrid == null || stockGrid.Columns.Count == 0)
            {
                return;
            }

            int visibleWidth = Math.Max(320, stockGrid.ClientSize.Width - 24);
            SetColumnWidth(stockGrid, "Name", Math.Max(95, visibleWidth * 32 / 100));
            SetColumnWidth(stockGrid, "Category", Math.Max(62, visibleWidth * 18 / 100));
            SetColumnWidth(stockGrid, "PurchasePrice", Math.Max(52, visibleWidth * 15 / 100));
            SetColumnWidth(stockGrid, "SalePrice", Math.Max(52, visibleWidth * 15 / 100));
            SetColumnWidth(stockGrid, "Quantity", Math.Max(38, visibleWidth * 10 / 100));
            SetColumnWidth(stockGrid, "MinimumQuantity", Math.Max(38, visibleWidth * 10 / 100));
        }

        private void FormatCartGrid()
        {
            HideColumn(cartGrid, "ProductId");
            HideColumn(cartGrid, "TaxRate");
            HideColumn(cartGrid, "Discount");
            RenameColumn(cartGrid, "ProductName", Localization.T("Name"));
            RenameColumn(cartGrid, "Quantity", "Qty");
            RenameColumn(cartGrid, "UnitPrice", "Prix");
            RenameColumn(cartGrid, "LineTotal", Localization.T("Total"));
            AlignColumn(cartGrid, "Quantity", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(cartGrid, "UnitPrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(cartGrid, "LineTotal", DataGridViewContentAlignment.MiddleRight);
            SetColumnWidth(cartGrid, "ProductName", 170);
            SetColumnWidth(cartGrid, "Quantity", 70);
            SetColumnWidth(cartGrid, "UnitPrice", 80);
            SetColumnWidth(cartGrid, "LineTotal", 90);
            FitCartGridColumns();
        }

        private void FitCartGridColumns()
        {
            if (cartGrid == null || cartGrid.Columns.Count == 0)
            {
                return;
            }

            int visibleWidth = Math.Max(300, cartGrid.ClientSize.Width - 24);
            SetColumnWidth(cartGrid, "ProductName", Math.Max(120, visibleWidth * 42 / 100));
            SetColumnWidth(cartGrid, "Quantity", Math.Max(58, visibleWidth * 17 / 100));
            SetColumnWidth(cartGrid, "UnitPrice", Math.Max(64, visibleWidth * 19 / 100));
            SetColumnWidth(cartGrid, "LineTotal", Math.Max(70, visibleWidth * 22 / 100));
        }

        private void HideColumn(DataGridView grid, string columnName)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].Visible = false;
            }
        }

        private void RenameColumn(DataGridView grid, string columnName, string title)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].HeaderText = title;
            }
        }

        private void AlignColumn(DataGridView grid, string columnName, DataGridViewContentAlignment alignment)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].DefaultCellStyle.Alignment = alignment;
            }
        }

        private void SetColumnWidth(DataGridView grid, string columnName, int width)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[columnName].Width = width;
            }
        }

        private void RefreshReports()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("OLY DRUGSTORE POS REPORT");
            builder.AppendLine("Store: " + activeStoreId);
            builder.AppendLine("Generated: " + DateTime.Now.ToString("g"));
            builder.AppendLine();
            builder.AppendLine("Sales count: " + store.Database.Sales.Count);
            builder.AppendLine("Total sales: " + store.Database.Sales.Sum(s => s.IsReturn ? -s.Total : s.Total).ToString("0.000") + " DT");
            builder.AppendLine("Debt total: " + store.Database.Sales.Where(s => s.IsDebt).Sum(s => s.Total).ToString("0.000") + " DT");
            builder.AppendLine();
            builder.AppendLine("LOW STOCK");
            foreach (Product product in store.Database.Products.Where(p => p.StoreId == activeStoreId && p.Quantity <= p.MinimumQuantity))
            {
                builder.AppendLine("- " + product.Name + " | Qty: " + product.Quantity);
            }
            builder.AppendLine();
            builder.AppendLine("EXPIRING SOON");
            foreach (Product product in store.Database.Products.Where(p => p.StoreId == activeStoreId && p.ExpiryDate <= DateTime.Today.AddDays(30)))
            {
                builder.AppendLine("- " + product.Name + " | Expiry: " + product.ExpiryDate.ToShortDateString());
            }
            if (reportTextBox != null) reportTextBox.Text = builder.ToString();
        }

        private void PrintReceipt(Sale sale)
        {
            string receipt = BuildReceipt(sale);
            try
            {
                PrintDocument document = new PrintDocument();
                document.PrintPage += delegate(object sender, PrintPageEventArgs e)
                {
                    e.Graphics.DrawString(receipt, new Font("Consolas", 9), Brushes.Black, 5, 5);
                };
                document.Print();
            }
            catch
            {
                MessageBox.Show(receipt, Localization.T("PrintTicket"));
            }
        }

        private string BuildReceipt(Sale sale)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("OLY DRUGSTORE");
            builder.AppendLine("Ticket: " + sale.TicketNumber);
            builder.AppendLine("Date: " + sale.CreatedAt.ToString("g"));
            builder.AppendLine("Cashier: " + sale.CashierUsername);
            builder.AppendLine("--------------------------------");
            foreach (SaleItem item in sale.Items)
            {
                builder.AppendLine(item.ProductName);
                builder.AppendLine(item.Quantity + " x " + item.UnitPrice.ToString("0.000") + " = " + item.LineTotal.ToString("0.000"));
            }
            builder.AppendLine("--------------------------------");
            builder.AppendLine("Discount: " + sale.Discount.ToString("0.000"));
            builder.AppendLine("TOTAL: " + sale.Total.ToString("0.000") + " DT");
            builder.AppendLine("Payment: " + sale.PaymentMethod);
            if (sale.IsDebt) builder.AppendLine("Customer debt: " + sale.CustomerName);
            builder.AppendLine("Thank you");
            return builder.ToString();
        }
    }
}
