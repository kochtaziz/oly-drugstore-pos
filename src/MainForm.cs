using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
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
        private Panel categoryViewportPanel;
        private Panel productViewportPanel;
        private FlowLayoutPanel productButtonsPanel;
        private VScrollBar categoryScrollBar;
        private VScrollBar productScrollBar;
        private FlowLayoutPanel categoryButtonsPanel;
        private DataGridView cartGrid;
        private Label totalLabel;
        private Label sessionSummaryLabel;
        private Button alertNotificationButton;
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
        private int selectedCartProductId;
        private string activeCategory = "All";
        private bool suppressSearchAutoAdd;
        private bool refreshingCategories;
        private bool scannerSubmitInProgress;
        private bool productDragScrolling;
        private bool productDragMoved;
        private int productDragStartY;
        private int productDragStartTop;
        private int productDragLastScrollSyncY;
        private string lastCategoryRenderKey = "";
        private string lastProductRenderKey = "";
        private Timer responsiveLayoutTimer;
        private Timer scannerAutoAddTimer;

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
        private DataGridView salesHistoryGrid;
        private TextBox saleDetailsTextBox;
        private Sale selectedHistorySale;
        private DataGridView stockStrategyGrid;
        private TextBox categoryStrategyTextBox;
        private TabControl adminTabs;
        private DataGridView debtGrid;
        private DataGridView alertGrid;
        private DataGridView stockMovementGrid;
        private DataGridView usersGrid;
        private DataGridView purchasesGrid;
        private DataGridView productProfitGrid;
        private DataGridView cashierPerformanceGrid;
        private DataGridView paymentSummaryGrid;
        private DataGridView categorySummaryGrid;
        private Label advancedReportKpiLabel;
        private TextBox userUsernameInput;
        private TextBox userPasswordInput;
        private TextBox userFullNameInput;
        private ComboBox userRoleInput;
        private ComboBox restockProductInput;
        private NumericUpDown restockQuantityInput;
        private NumericUpDown restockCostInput;
        private TextBox restockSupplierInput;
        private User selectedManagedUser;
        private Sale selectedDebtSale;

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
                BuildStockStrategyTab();
                BuildAdminToolsTab();
            }
            else
            {
                BuildSalesTab();
                BuildCashTab();
                BuildSalesHistoryTab();
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

            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.BackColor = UiTheme.Primary;
            top.Controls.Add(statusPanel, 3, 0);

            alertNotificationButton = UiTheme.SecondaryButton("");
            alertNotificationButton.Name = "alertNotificationButton";
            alertNotificationButton.Left = 0;
            alertNotificationButton.Top = 0;
            alertNotificationButton.Width = 255;
            alertNotificationButton.Height = 34;
            alertNotificationButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ThemedButton themedAlertButton = alertNotificationButton as ThemedButton;
            if (themedAlertButton != null)
            {
                themedAlertButton.NormalBackColor = Color.FromArgb(254, 243, 199);
                themedAlertButton.HoverBackColor = Color.FromArgb(253, 230, 138);
                themedAlertButton.PressedBackColor = Color.FromArgb(252, 211, 77);
            }
            alertNotificationButton.BackColor = Color.FromArgb(254, 243, 199);
            alertNotificationButton.ForeColor = Color.FromArgb(146, 64, 14);
            alertNotificationButton.FlatAppearance.BorderColor = Color.FromArgb(251, 191, 36);
            alertNotificationButton.Font = UiTheme.FontBold;
            alertNotificationButton.Visible = false;
            alertNotificationButton.Click += delegate { ShowUrgentAlerts(); };
            statusPanel.Controls.Add(alertNotificationButton);

            statusPanel.Resize += delegate
            {
                int statusWidth = Math.Min(280, Math.Max(150, statusPanel.Width));
                alertNotificationButton.Width = statusWidth;
                alertNotificationButton.Left = Math.Max(0, statusPanel.Width - alertNotificationButton.Width);
                if (sessionSummaryLabel != null)
                {
                    sessionSummaryLabel.Width = statusWidth;
                    sessionSummaryLabel.Left = Math.Max(0, statusPanel.Width - sessionSummaryLabel.Width);
                }
            };

            sessionSummaryLabel = new Label();
            sessionSummaryLabel.ForeColor = Color.FromArgb(240, 253, 244);
            sessionSummaryLabel.Font = UiTheme.FontBold;
            sessionSummaryLabel.TextAlign = ContentAlignment.MiddleRight;
            sessionSummaryLabel.Left = 0;
            sessionSummaryLabel.Top = 38;
            sessionSummaryLabel.Width = 255;
            sessionSummaryLabel.Height = 32;
            sessionSummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            statusPanel.Controls.Add(sessionSummaryLabel);

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

            Label productTitle = CardTitle(Localization.T("CatalogScanner"), 18, 14);
            productTitle.Name = "catalogScannerTitle";
            productCard.Controls.Add(productTitle);

            Label scannerLabel = UiTheme.FieldLabel(Localization.T("ScannerBarcode"), 18, 52);
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
                    SubmitScannerText(true);
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

            categoryViewportPanel = new SmoothPanel();
            categoryViewportPanel.Left = 18;
            categoryViewportPanel.Top = 158;
            categoryViewportPanel.Width = 145;
            categoryViewportPanel.Height = 345;
            categoryViewportPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            categoryViewportPanel.BackColor = UiTheme.CardAlt;
            categoryViewportPanel.BorderStyle = BorderStyle.None;
            productCard.Controls.Add(categoryViewportPanel);

            categoryButtonsPanel = new SmoothFlowLayoutPanel();
            categoryButtonsPanel.Left = 0;
            categoryButtonsPanel.Top = 0;
            categoryButtonsPanel.Width = 145;
            categoryButtonsPanel.Height = 345;
            categoryButtonsPanel.AutoScroll = false;
            categoryButtonsPanel.WrapContents = false;
            categoryButtonsPanel.FlowDirection = FlowDirection.TopDown;
            categoryButtonsPanel.BackColor = UiTheme.CardAlt;
            categoryButtonsPanel.Padding = new Padding(0);
            categoryViewportPanel.Controls.Add(categoryButtonsPanel);

            categoryScrollBar = new VScrollBar();
            categoryScrollBar.Left = 18;
            categoryScrollBar.Top = 158;
            categoryScrollBar.Width = 32;
            categoryScrollBar.Height = 345;
            categoryScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            categoryScrollBar.SmallChange = 48;
            categoryScrollBar.LargeChange = 144;
            categoryScrollBar.Scroll += delegate(object sender, ScrollEventArgs e)
            {
                ScrollCategoriesTo(e.NewValue);
            };
            productCard.Controls.Add(categoryScrollBar);

            productViewportPanel = new SmoothPanel();
            productViewportPanel.Left = 172;
            productViewportPanel.Top = 158;
            productViewportPanel.Width = 390;
            productViewportPanel.Height = 345;
            productViewportPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            productViewportPanel.BackColor = UiTheme.CardAlt;
            productViewportPanel.BorderStyle = BorderStyle.None;
            productViewportPanel.MouseWheel += delegate(object sender, MouseEventArgs e)
            {
                ScrollProductsTo(-productButtonsPanel.Top - Math.Sign(e.Delta) * 96);
                SyncProductScrollBarFromPanel();
            };
            productViewportPanel.MouseDown += ProductDragMouseDown;
            productViewportPanel.MouseMove += ProductDragMouseMove;
            productViewportPanel.MouseUp += ProductDragMouseUp;
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
            productButtonsPanel.MouseDown += ProductDragMouseDown;
            productButtonsPanel.MouseMove += ProductDragMouseMove;
            productButtonsPanel.MouseUp += ProductDragMouseUp;
            productViewportPanel.Controls.Add(productButtonsPanel);

            productScrollBar = new VScrollBar();
            productScrollBar.Left = 570;
            productScrollBar.Top = 158;
            productScrollBar.Width = 44;
            productScrollBar.Height = 345;
            productScrollBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            productScrollBar.SmallChange = 48;
            productScrollBar.LargeChange = 144;
            productScrollBar.Visible = true;
            productScrollBar.Scroll += delegate(object sender, ScrollEventArgs e)
            {
                ScrollProductsTo(e.NewValue);
            };
            productCard.Controls.Add(productScrollBar);

            scannerAutoAddTimer = new Timer();
            scannerAutoAddTimer.Interval = 120;
            scannerAutoAddTimer.Tick += delegate
            {
                scannerAutoAddTimer.Stop();
                SubmitScannerText(false);
            };

            Panel checkoutCard = UiTheme.CardPanel();
            checkoutCard.Dock = DockStyle.Fill;
            checkoutCard.Padding = new Padding(18);
            checkoutCard.AutoScroll = true;
            shell.Controls.Add(checkoutCard, 1, 0);

            Label currentTicketTitle = CardTitle(Localization.T("CurrentTicket"), 18, 14);
            currentTicketTitle.Name = "currentTicketTitle";
            checkoutCard.Controls.Add(currentTicketTitle);

            cartGrid = UiTheme.Grid();
            cartGrid.Left = 18;
            cartGrid.Top = 56;
            cartGrid.Width = 410;
            cartGrid.Height = 160;
            cartGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cartGrid.CellClick += delegate { RememberSelectedCartItem(); };
            cartGrid.SelectionChanged += delegate { RememberSelectedCartItem(); };
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
            Label storeStockTitle = CardTitle(Localization.T("StoreStock"), 18, 14);
            storeStockTitle.Name = "storeStockTitle";
            listCard.Controls.Add(storeStockTitle);

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
            Label productFormTitle = CardTitle(Localization.T("ProductForm"), 22, 18);
            productFormTitle.Name = "productFormTitle";
            form.Controls.Add(productFormTitle);

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

            AddLabel(form, "storeFieldLabel", Localization.T("Store"), 245, 406);
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
            Label cashClosingTitle = CardTitle(Localization.T("CashClosing"), 24, 20);
            cashClosingTitle.Name = "cashClosingTitle";
            sessionCard.Controls.Add(cashClosingTitle);

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
            Label cashMovementsTitle = CardTitle(Localization.T("CashMovements"), 24, 20);
            cashMovementsTitle.Name = "cashMovementsTitle";
            movementCard.Controls.Add(cashMovementsTitle);

            AddLabel(movementCard, "movementTypeLabel", Localization.T("Type"), 24, 84);
            movementTypeInput = new ComboBox();
            movementTypeInput.DropDownStyle = ComboBoxStyle.DropDownList;
            FillMovementTypeInput("Withdrawal");
            movementTypeInput.Left = 24;
            movementTypeInput.Top = 110;
            movementTypeInput.Width = 200;
            movementCard.Controls.Add(movementTypeInput);

            AddLabel(movementCard, "movementAmountLabel", Localization.T("Amount"), 250, 84);
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

        private void BuildSalesHistoryTab()
        {
            TabPage tab = NewTab("historyTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            tab.Controls.Add(shell);

            Panel listCard = UiTheme.CardPanel();
            listCard.Dock = DockStyle.Fill;
            listCard.Padding = new Padding(18);
            shell.Controls.Add(listCard, 0, 0);
            listCard.Controls.Add(CardTitle(Localization.T("SalesHistory"), 18, 14));

            Button refreshButton = UiTheme.SecondaryButton(Localization.T("Refresh"));
            refreshButton.Name = "refreshHistoryButton";
            refreshButton.Width = 150;
            refreshButton.Height = 46;
            refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refreshButton.Left = listCard.Width - refreshButton.Width - 18;
            refreshButton.Top = 18;
            refreshButton.Click += delegate { RefreshSalesHistory(); };
            listCard.Controls.Add(refreshButton);

            salesHistoryGrid = UiTheme.Grid();
            salesHistoryGrid.Left = 18;
            salesHistoryGrid.Top = 78;
            salesHistoryGrid.Width = 660;
            salesHistoryGrid.Height = 560;
            salesHistoryGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            salesHistoryGrid.CellClick += delegate { LoadSelectedHistorySale(); };
            salesHistoryGrid.SelectionChanged += delegate { LoadSelectedHistorySale(); };
            listCard.Controls.Add(salesHistoryGrid);

            Panel detailCard = UiTheme.CardPanel();
            detailCard.Dock = DockStyle.Fill;
            detailCard.Padding = new Padding(18);
            shell.Controls.Add(detailCard, 1, 0);
            detailCard.Controls.Add(CardTitle(Localization.T("TicketDetails"), 18, 14));

            saleDetailsTextBox = new TextBox();
            saleDetailsTextBox.Multiline = true;
            saleDetailsTextBox.ReadOnly = true;
            saleDetailsTextBox.ScrollBars = ScrollBars.Vertical;
            saleDetailsTextBox.Font = new Font("Consolas", 11);
            saleDetailsTextBox.BackColor = Color.White;
            saleDetailsTextBox.ForeColor = UiTheme.Text;
            saleDetailsTextBox.Left = 18;
            saleDetailsTextBox.Top = 78;
            saleDetailsTextBox.Width = 390;
            saleDetailsTextBox.Height = 470;
            saleDetailsTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            detailCard.Controls.Add(saleDetailsTextBox);

            Button reprintButton = UiTheme.PrimaryButton(Localization.T("Reprint"));
            reprintButton.Name = "reprintHistoryButton";
            reprintButton.Left = 18;
            reprintButton.Top = 565;
            reprintButton.Width = 390;
            reprintButton.Height = 54;
            reprintButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            reprintButton.Click += delegate { ReprintSelectedHistorySale(); };
            detailCard.Controls.Add(reprintButton);
        }

        private void BuildStockStrategyTab()
        {
            TabPage tab = NewTab("stockStrategyTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            tab.Controls.Add(shell);

            Panel strategyCard = UiTheme.CardPanel();
            strategyCard.Dock = DockStyle.Fill;
            strategyCard.Padding = new Padding(18);
            shell.Controls.Add(strategyCard, 0, 0);
            strategyCard.Controls.Add(CardTitle(Localization.T("StockStrategy"), 18, 14));

            if (user.Role == UserRole.Admin)
            {
                Button applyButton = UiTheme.PrimaryButton(Localization.T("ApplyABC"));
                applyButton.Name = "applyAbcButton";
                applyButton.Width = 230;
                applyButton.Height = 48;
                applyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                applyButton.Left = strategyCard.Width - applyButton.Width - 18;
                applyButton.Top = 18;
                applyButton.Click += delegate { ApplyAbcStockStrategy(); };
                strategyCard.Controls.Add(applyButton);
            }

            stockStrategyGrid = UiTheme.Grid();
            stockStrategyGrid.Left = 18;
            stockStrategyGrid.Top = 78;
            stockStrategyGrid.Width = 700;
            stockStrategyGrid.Height = 560;
            stockStrategyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            strategyCard.Controls.Add(stockStrategyGrid);

            Panel categoryCard = UiTheme.CardPanel();
            categoryCard.Dock = DockStyle.Fill;
            categoryCard.Padding = new Padding(18);
            shell.Controls.Add(categoryCard, 1, 0);
            categoryCard.Controls.Add(CardTitle(Localization.T("CategoryPerformance"), 18, 14));

            categoryStrategyTextBox = new TextBox();
            categoryStrategyTextBox.Multiline = true;
            categoryStrategyTextBox.ReadOnly = true;
            categoryStrategyTextBox.ScrollBars = ScrollBars.Vertical;
            categoryStrategyTextBox.Font = new Font("Consolas", 11);
            categoryStrategyTextBox.BackColor = Color.White;
            categoryStrategyTextBox.ForeColor = UiTheme.Text;
            categoryStrategyTextBox.Left = 18;
            categoryStrategyTextBox.Top = 78;
            categoryStrategyTextBox.Width = 390;
            categoryStrategyTextBox.Height = 560;
            categoryStrategyTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            categoryCard.Controls.Add(categoryStrategyTextBox);
        }

        private void BuildAdminToolsTab()
        {
            TabPage tab = NewTab("adminToolsTab");
            adminTabs = new TabControl();
            adminTabs.Dock = DockStyle.Fill;
            adminTabs.Font = UiTheme.FontBold;
            adminTabs.Padding = new Point(10, 6);
            adminTabs.ItemSize = new Size(124, 32);
            adminTabs.SizeMode = TabSizeMode.Fixed;
            tab.Controls.Add(adminTabs);

            BuildDebtAdminPage(adminTabs);
            BuildAlertsAdminPage(adminTabs);
            BuildStockLogAdminPage(adminTabs);
            BuildUsersAdminPage(adminTabs);
            BuildRestockAdminPage(adminTabs);
            BuildAdvancedReportsAdminPage(adminTabs);
            BuildFilesAdminPage(adminTabs);
        }

        private TabPage InnerTab(TabControl owner, string title)
        {
            TabPage page = new TabPage(title);
            page.BackColor = UiTheme.Background;
            page.Padding = new Padding(14);
            owner.TabPages.Add(page);
            return page;
        }

        private void BuildDebtAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("Debts"));
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            page.Controls.Add(card);
            card.Controls.Add(CardTitle(Localization.T("Debts"), 18, 14));

            debtGrid = UiTheme.Grid();
            debtGrid.Left = 18;
            debtGrid.Top = 78;
            debtGrid.Width = 760;
            debtGrid.Height = 390;
            debtGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            debtGrid.CellClick += delegate { LoadSelectedDebt(); };
            card.Controls.Add(debtGrid);

            Button paid = UiTheme.PrimaryButton(Localization.T("MarkPaid"));
            paid.Name = "markDebtPaidButton";
            paid.Left = 680;
            paid.Top = 18;
            paid.Width = 220;
            paid.Height = 48;
            paid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            paid.Click += delegate { MarkSelectedDebtPaid(); };
            card.Controls.Add(paid);
        }

        private void BuildAlertsAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("Alerts"));
            page.Name = "alertsAdminPage";
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            page.Controls.Add(card);
            card.Controls.Add(CardTitle(Localization.T("Alerts"), 18, 14));

            alertGrid = UiTheme.Grid();
            alertGrid.Left = 18;
            alertGrid.Top = 78;
            alertGrid.Width = 840;
            alertGrid.Height = 540;
            alertGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(alertGrid);
        }

        private void BuildStockLogAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("StockLog"));
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            page.Controls.Add(card);
            card.Controls.Add(CardTitle(Localization.T("StockLog"), 18, 14));

            stockMovementGrid = UiTheme.Grid();
            stockMovementGrid.Left = 18;
            stockMovementGrid.Top = 78;
            stockMovementGrid.Width = 840;
            stockMovementGrid.Height = 540;
            stockMovementGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(stockMovementGrid);
        }

        private void BuildUsersAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("Users"));
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            page.Controls.Add(shell);

            Panel list = UiTheme.CardPanel();
            list.Dock = DockStyle.Fill;
            list.Padding = new Padding(18);
            shell.Controls.Add(list, 0, 0);
            list.Controls.Add(CardTitle(Localization.T("Users"), 18, 14));
            usersGrid = UiTheme.Grid();
            usersGrid.Left = 18;
            usersGrid.Top = 78;
            usersGrid.Width = 520;
            usersGrid.Height = 520;
            usersGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            usersGrid.CellClick += delegate { LoadSelectedManagedUser(); };
            list.Controls.Add(usersGrid);

            Panel form = UiTheme.CardPanel();
            form.Dock = DockStyle.Fill;
            form.Padding = new Padding(22);
            shell.Controls.Add(form, 1, 0);
            form.Controls.Add(CardTitle(Localization.T("UserForm"), 22, 18));
            userUsernameInput = AddTextField(form, "managedUsernameLabel", 22, 76, 320);
            userPasswordInput = AddTextField(form, "managedPasswordLabel", 22, 142, 320);
            userFullNameInput = AddTextField(form, "managedFullNameLabel", 22, 208, 320);
            AddLabel(form, "managedRoleLabel", Localization.T("Role"), 22, 274);
            userRoleInput = new ComboBox();
            userRoleInput.DropDownStyle = ComboBoxStyle.DropDownList;
            userRoleInput.Items.AddRange(new object[] { "Admin", "Cashier" });
            userRoleInput.Left = 22;
            userRoleInput.Top = 300;
            userRoleInput.Width = 180;
            userRoleInput.SelectedIndex = 1;
            form.Controls.Add(userRoleInput);

            Button save = UiTheme.PrimaryButton(Localization.T("SaveUser"));
            save.Name = "saveUserButton";
            save.Left = 22;
            save.Top = 370;
            save.Width = 160;
            save.Height = 50;
            save.Click += delegate { SaveManagedUser(); };
            form.Controls.Add(save);

            Button delete = UiTheme.SecondaryButton(Localization.T("DeleteUser"));
            delete.Name = "deleteUserButton";
            delete.Left = 198;
            delete.Top = 370;
            delete.Width = 160;
            delete.Height = 50;
            delete.Click += delegate { DeleteManagedUser(); };
            form.Controls.Add(delete);
        }

        private void BuildRestockAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("Restock"));
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            page.Controls.Add(shell);

            Panel form = UiTheme.CardPanel();
            form.Dock = DockStyle.Fill;
            form.Padding = new Padding(22);
            shell.Controls.Add(form, 0, 0);
            form.Controls.Add(CardTitle(Localization.T("Restock"), 22, 18));

            AddLabel(form, "restockProductLabel", Localization.T("Products"), 22, 76);
            restockProductInput = new ComboBox();
            restockProductInput.DropDownStyle = ComboBoxStyle.DropDownList;
            restockProductInput.Left = 22;
            restockProductInput.Top = 102;
            restockProductInput.Width = 330;
            form.Controls.Add(restockProductInput);

            restockQuantityInput = AddNumberField(form, "restockQuantityLabel", 22, 166, 150);
            restockCostInput = AddMoneyField(form, "restockCostLabel", 202, 166, 150);
            restockSupplierInput = AddTextField(form, "restockSupplierLabel", 22, 238, 330);
            Button save = UiTheme.PrimaryButton(Localization.T("SaveRestock"));
            save.Name = "saveRestockButton";
            save.Left = 22;
            save.Top = 320;
            save.Width = 330;
            save.Height = 54;
            save.Click += delegate { SaveRestock(); };
            form.Controls.Add(save);

            Panel list = UiTheme.CardPanel();
            list.Dock = DockStyle.Fill;
            list.Padding = new Padding(18);
            shell.Controls.Add(list, 1, 0);
            list.Controls.Add(CardTitle(Localization.T("PurchaseHistory"), 18, 14));
            purchasesGrid = UiTheme.Grid();
            purchasesGrid.Left = 18;
            purchasesGrid.Top = 78;
            purchasesGrid.Width = 600;
            purchasesGrid.Height = 520;
            purchasesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            list.Controls.Add(purchasesGrid);
        }

        private void BuildAdvancedReportsAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("AdvancedReports"));
            TableLayoutPanel shell = PageGrid(2, 3);
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            page.Controls.Add(shell);

            Panel kpiCard = UiTheme.CardPanel();
            kpiCard.Dock = DockStyle.Fill;
            kpiCard.Padding = new Padding(18);
            shell.Controls.Add(kpiCard, 0, 0);
            shell.SetColumnSpan(kpiCard, 2);
            kpiCard.Controls.Add(CardTitle(Localization.T("AdvancedReports"), 18, 14));
            advancedReportKpiLabel = new Label();
            advancedReportKpiLabel.Left = 18;
            advancedReportKpiLabel.Top = 58;
            advancedReportKpiLabel.Width = 980;
            advancedReportKpiLabel.Height = 56;
            advancedReportKpiLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            advancedReportKpiLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            advancedReportKpiLabel.ForeColor = UiTheme.Text;
            kpiCard.Controls.Add(advancedReportKpiLabel);

            productProfitGrid = AddReportGrid(shell, Localization.T("ProductProfit"), 0, 1);
            cashierPerformanceGrid = AddReportGrid(shell, Localization.T("CashierPerformance"), 1, 1);
            paymentSummaryGrid = AddReportGrid(shell, Localization.T("PaymentSummary"), 0, 2);
            categorySummaryGrid = AddReportGrid(shell, Localization.T("CategorySummary"), 1, 2);
        }

        private DataGridView AddReportGrid(TableLayoutPanel shell, string title, int column, int row)
        {
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            shell.Controls.Add(card, column, row);
            card.Controls.Add(CardTitle(title, 18, 14));
            DataGridView grid = UiTheme.Grid();
            grid.Left = 18;
            grid.Top = 58;
            grid.Width = 420;
            grid.Height = 180;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            card.Controls.Add(grid);
            return grid;
        }

        private void BuildFilesAdminPage(TabControl owner)
        {
            TabPage page = InnerTab(owner, Localization.T("Files"));
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(28);
            page.Controls.Add(card);
            card.Controls.Add(CardTitle(Localization.T("Files"), 28, 24));

            Button backup = UiTheme.PrimaryButton(Localization.T("BackupNow"));
            backup.Name = "backupNowButton";
            backup.Left = 28;
            backup.Top = 90;
            backup.Width = 260;
            backup.Height = 56;
            backup.Click += delegate { ManualBackup(); };
            card.Controls.Add(backup);

            Button restore = UiTheme.SecondaryButton(Localization.T("RestoreBackup"));
            restore.Name = "restoreBackupButton";
            restore.Left = 310;
            restore.Top = 90;
            restore.Width = 260;
            restore.Height = 56;
            restore.Click += delegate { RestoreBackup(); };
            card.Controls.Add(restore);

            Button exportProducts = UiTheme.SecondaryButton(Localization.T("ExportProducts"));
            exportProducts.Name = "exportProductsButton";
            exportProducts.Left = 28;
            exportProducts.Top = 175;
            exportProducts.Width = 260;
            exportProducts.Height = 56;
            exportProducts.Click += delegate { ExportProductsCsv(); };
            card.Controls.Add(exportProducts);

            Button exportSales = UiTheme.SecondaryButton(Localization.T("ExportSales"));
            exportSales.Name = "exportSalesButton";
            exportSales.Left = 310;
            exportSales.Top = 175;
            exportSales.Width = 260;
            exportSales.Height = 56;
            exportSales.Click += delegate { ExportSalesCsv(); };
            card.Controls.Add(exportSales);

            Button importProducts = UiTheme.SecondaryButton(Localization.T("ImportProducts"));
            importProducts.Name = "importProductsButton";
            importProducts.Left = 592;
            importProducts.Top = 175;
            importProducts.Width = 260;
            importProducts.Height = 56;
            importProducts.Click += delegate { ImportProductsCsv(); };
            card.Controls.Add(importProducts);
        }

        private void BuildReportsTab()
        {
            TabPage tab = NewTab("reportsTab");
            Panel card = UiTheme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(18);
            tab.Controls.Add(card);

            Label reportTitle = CardTitle(Localization.T("ReportTitle"), 18, 14);
            reportTitle.Name = "reportTitle";
            card.Controls.Add(reportTitle);
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

            Label title = CardTitle(Localization.T("System"), 28, 24);
            title.Name = "systemTitle";
            card.Controls.Add(title);
            Label body = new Label();
            body.Name = "settingsBody";
            body.Text = Localization.T("SettingsBody");
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
            if (tabs.TabPages.ContainsKey("historyTab")) tabs.TabPages["historyTab"].Text = Localization.T("History");
            if (tabs.TabPages.ContainsKey("stockStrategyTab")) tabs.TabPages["stockStrategyTab"].Text = Localization.T("Stock");
            if (tabs.TabPages.ContainsKey("adminToolsTab")) tabs.TabPages["adminToolsTab"].Text = Localization.T("Admin");
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
            SetText("refreshHistoryButton", Localization.T("Refresh"));
            SetText("reprintHistoryButton", Localization.T("Reprint"));
            SetText("applyAbcButton", Localization.T("ApplyABC"));
            SetText("markDebtPaidButton", Localization.T("MarkPaid"));
            SetText("saveUserButton", Localization.T("SaveUser"));
            SetText("deleteUserButton", Localization.T("DeleteUser"));
            SetText("saveRestockButton", Localization.T("SaveRestock"));
            SetText("backupNowButton", Localization.T("BackupNow"));
            SetText("restoreBackupButton", Localization.T("RestoreBackup"));
            SetText("exportProductsButton", Localization.T("ExportProducts"));
            SetText("exportSalesButton", Localization.T("ExportSales"));
            SetText("importProductsButton", Localization.T("ImportProducts"));
            SetText("countedLabel", Localization.T("CountedCash"));
            SetText("managedUsernameLabel", Localization.T("Username"));
            SetText("managedPasswordLabel", Localization.T("Password"));
            SetText("managedFullNameLabel", Localization.T("FullName"));
            SetText("managedRoleLabel", Localization.T("Role"));
            SetText("restockProductLabel", Localization.T("Products"));
            SetText("restockQuantityLabel", Localization.T("Quantity"));
            SetText("restockCostLabel", Localization.T("PurchasePrice"));
            SetText("restockSupplierLabel", Localization.T("Supplier"));
            SetText("movementReasonLabel", Localization.T("Reason"));
            SetText("catalogScannerTitle", Localization.T("CatalogScanner"));
            SetText("scannerLabel", Localization.T("ScannerBarcode"));
            SetText("currentTicketTitle", Localization.T("CurrentTicket"));
            SetText("storeStockTitle", Localization.T("StoreStock"));
            SetText("productFormTitle", Localization.T("ProductForm"));
            SetText("storeFieldLabel", Localization.T("Store"));
            SetText("cashClosingTitle", Localization.T("CashClosing"));
            SetText("cashMovementsTitle", Localization.T("CashMovements"));
            SetText("movementTypeLabel", Localization.T("Type"));
            SetText("movementAmountLabel", Localization.T("Amount"));
            SetText("reportTitle", Localization.T("ReportTitle"));
            SetText("systemTitle", Localization.T("System"));
            SetText("settingsBody", Localization.T("SettingsBody"));
            FillMovementTypeInput(GetSelectedMovementType());
            RefreshAll();
        }

        private void SetText(string name, string value)
        {
            Control[] matches = Controls.Find(name, true);
            if (matches.Length > 0) matches[0].Text = value;
        }

        private void RefreshAll()
        {
            store.ReloadIfChanged();
            RefreshProducts();
            RefreshCart();
            RefreshCash();
            RefreshUrgentNotification();
            RefreshSalesHistory();
            RefreshStockStrategy();
            RefreshAdminTools();
            RefreshReports();
        }

        private void RefreshUrgentNotification()
        {
            if (alertNotificationButton == null) return;

            List<Product> lowStockProducts = store.Database.Products
                .Where(p => p.StoreId == activeStoreId && p.Quantity <= p.MinimumQuantity)
                .ToList();
            List<Product> nextWeekExpiryProducts = store.Database.Products
                .Where(p => p.StoreId == activeStoreId && p.ExpiryDate <= DateTime.Today.AddDays(7))
                .ToList();

            int urgentCount = lowStockProducts
                .Select(p => p.Id)
                .Union(nextWeekExpiryProducts.Select(p => p.Id))
                .Count();

            alertNotificationButton.Visible = urgentCount > 0;
            if (urgentCount == 0)
            {
                alertNotificationButton.Text = "";
                return;
            }

            int availableWidth = alertNotificationButton.Parent == null
                ? alertNotificationButton.Width
                : Math.Min(alertNotificationButton.Width, alertNotificationButton.Parent.Width);
            if (availableWidth < 230)
            {
                alertNotificationButton.Text = Localization.T("Alerts") + ": " + urgentCount;
            }
            else
            {
                alertNotificationButton.Text =
                    Localization.T("UrgentAlerts") + ": " + urgentCount +
                    " | " + Localization.T("LowStockShort") + " " + lowStockProducts.Count +
                    " | " + Localization.T("ExpireNextWeek") + " " + nextWeekExpiryProducts.Count;
            }
        }

        private void ShowUrgentAlerts()
        {
            if (user.Role == UserRole.Admin && tabs.TabPages.ContainsKey("adminToolsTab"))
            {
                tabs.SelectedTab = tabs.TabPages["adminToolsTab"];
                if (adminTabs != null && adminTabs.TabPages.ContainsKey("alertsAdminPage"))
                {
                    adminTabs.SelectedTab = adminTabs.TabPages["alertsAdminPage"];
                }
                return;
            }

            List<Product> urgentProducts = store.Database.Products
                .Where(p => p.StoreId == activeStoreId && (p.Quantity <= p.MinimumQuantity || p.ExpiryDate <= DateTime.Today.AddDays(7)))
                .OrderBy(p => p.Quantity <= p.MinimumQuantity ? 0 : 1)
                .ThenBy(p => p.ExpiryDate)
                .Take(12)
                .ToList();

            if (urgentProducts.Count == 0)
            {
                MessageBox.Show(Localization.T("NoUrgentAlerts"));
                return;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Localization.T("UrgentAlerts"));
            builder.AppendLine("--------------------------------");
            foreach (Product product in urgentProducts)
            {
                string reason = product.Quantity <= product.MinimumQuantity
                    ? Localization.T("LowStock")
                    : Localization.T("ExpireNextWeek");
                builder.AppendLine(reason + " | " + product.Name);
                builder.AppendLine(Localization.T("Quantity") + ": " + product.Quantity + " | " + Localization.T("Expiry") + ": " + product.ExpiryDate.ToShortDateString());
            }
            MessageBox.Show(builder.ToString(), Localization.T("UrgentAlerts"));
        }

        private void RefreshProducts()
        {
            RefreshCategories();

            string query = searchTextBox == null ? "" : searchTextBox.Text.Trim();
            string normalizedQuery = query.ToLowerInvariant();
            List<Product> storeProducts = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .OrderBy(p => p.Name)
                .ToList();
            List<Product> products = storeProducts
                .Where(p => activeCategory == "All" || p.Category == activeCategory)
                .Where(p => searchTextBox == null ||
                            string.IsNullOrEmpty(normalizedQuery) ||
                            p.Name.ToLowerInvariant().Contains(normalizedQuery) ||
                            p.Category.ToLowerInvariant().Contains(normalizedQuery) ||
                            (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(query)))
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
                    stockGrid.DataSource = storeProducts.ToList();
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
                FitCategoryButtonsToViewport();
                SetRedraw(categoryButtonsPanel, true);
                refreshingCategories = false;
            }
        }

        private void RenderCategoryButton(string category)
        {
            Button button = category == activeCategory
                ? UiTheme.PrimaryButton(category)
                : UiTheme.SecondaryButton(category);
            button.Width = Math.Max(100, categoryButtonsPanel.Width - 4);
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
                    button.MouseDown += ProductDragMouseDown;
                    button.MouseMove += ProductDragMouseMove;
                    button.MouseUp += ProductDragMouseUp;
                    button.Click += delegate(object sender, EventArgs e)
                    {
                        if (productDragMoved)
                        {
                            productDragMoved = false;
                            return;
                        }

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
            productScrollBar.Visible = true;
            productScrollBar.Enabled = true;
            productScrollBar.Minimum = 0;
            productScrollBar.LargeChange = 144;
            productScrollBar.SmallChange = 48;
            productScrollBar.Maximum = Math.Max(productScrollBar.LargeChange, scrollableHeight + productScrollBar.LargeChange - 1);
            int current = Math.Min(scrollableHeight, Math.Max(0, -productButtonsPanel.Top));
            productScrollBar.Value = Math.Min(productScrollBar.Maximum - productScrollBar.LargeChange + 1, current);
        }

        private void UpdateCategoryScrollBar()
        {
            if (categoryButtonsPanel == null || categoryViewportPanel == null || categoryScrollBar == null)
            {
                return;
            }

            int scrollableHeight = Math.Max(0, categoryButtonsPanel.Height - categoryViewportPanel.Height);
            categoryScrollBar.Enabled = scrollableHeight > 0;
            categoryScrollBar.Minimum = 0;
            categoryScrollBar.LargeChange = 144;
            categoryScrollBar.SmallChange = 48;
            categoryScrollBar.Maximum = scrollableHeight + categoryScrollBar.LargeChange - 1;
            int current = Math.Min(scrollableHeight, Math.Max(0, -categoryButtonsPanel.Top));
            categoryScrollBar.Value = Math.Min(categoryScrollBar.Maximum - categoryScrollBar.LargeChange + 1, current);
        }

        private void ScrollCategoriesTo(int value)
        {
            if (categoryButtonsPanel == null || categoryViewportPanel == null || categoryScrollBar == null)
            {
                return;
            }

            int scrollableHeight = Math.Max(0, categoryButtonsPanel.Height - categoryViewportPanel.Height);
            int target = Math.Min(scrollableHeight, Math.Max(0, value));
            categoryButtonsPanel.Top = -target;
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

        private void ProductDragMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || productButtonsPanel == null)
            {
                return;
            }

            productDragScrolling = true;
            productDragMoved = false;
            productDragStartY = Cursor.Position.Y;
            productDragStartTop = productButtonsPanel.Top;
            productDragLastScrollSyncY = productDragStartY;
            Control source = sender as Control;
            if (source != null)
            {
                source.Capture = true;
            }
        }

        private void ProductDragMouseMove(object sender, MouseEventArgs e)
        {
            if (!productDragScrolling || productButtonsPanel == null || productViewportPanel == null)
            {
                return;
            }

            int delta = Cursor.Position.Y - productDragStartY;
            if (Math.Abs(delta) > 6)
            {
                productDragMoved = true;
            }

            int scrollableHeight = Math.Max(0, productButtonsPanel.Height - productViewportPanel.Height);
            int targetTop = Math.Min(0, Math.Max(-scrollableHeight, productDragStartTop + delta));
            productButtonsPanel.Top = targetTop;
            if (Math.Abs(Cursor.Position.Y - productDragLastScrollSyncY) >= 24)
            {
                productDragLastScrollSyncY = Cursor.Position.Y;
                SyncProductScrollBarFromPanel();
            }
        }

        private void ProductDragMouseUp(object sender, MouseEventArgs e)
        {
            productDragScrolling = false;
            SyncProductScrollBarFromPanel();
            Control source = sender as Control;
            if (source != null)
            {
                source.Capture = false;
            }
        }

        private void SyncProductScrollBarFromPanel()
        {
            if (productButtonsPanel == null || productViewportPanel == null || productScrollBar == null)
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
            LayoutSalesHistoryScreen();
            LayoutStockStrategyScreen();
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
            if (categoryViewportPanel == null || productViewportPanel == null || productButtonsPanel == null || productScrollBar == null || cartGrid == null)
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
            int railGap = 8;
            int categoryScrollWidth = 32;
            int productScrollWidth = 44;
            int categoryAreaWidth = Math.Min(172, Math.Max(146, productWidth / 4));
            int categoryViewportWidth = Math.Max(104, categoryAreaWidth - categoryScrollWidth - railGap);
            categoryViewportPanel.Left = pad;
            categoryViewportPanel.Top = mainTop;
            categoryViewportPanel.Width = categoryViewportWidth;
            categoryViewportPanel.Height = mainHeight;
            categoryScrollBar.Width = categoryScrollWidth;
            categoryScrollBar.Left = categoryViewportPanel.Right + railGap;
            categoryScrollBar.Top = mainTop;
            categoryScrollBar.Height = mainHeight;
            categoryButtonsPanel.Left = 0;
            categoryButtonsPanel.Width = categoryViewportPanel.Width;
            FitCategoryButtonsToViewport();

            productScrollBar.Width = productScrollWidth;
            productScrollBar.Left = Math.Max(categoryScrollBar.Right + 140, productWidth - pad - productScrollBar.Width);
            productScrollBar.Top = mainTop;
            productScrollBar.Height = mainHeight;

            productViewportPanel.Left = categoryScrollBar.Right + 12;
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

            int columns = Math.Max(1, (productViewportPanel.ClientSize.Width - 8) / Math.Max(1, tileWidth + 14));
            int rows = productButtonsPanel.Controls.Count == 0
                ? 1
                : (int)Math.Ceiling(productButtonsPanel.Controls.Count / (decimal)columns);
            int tileHeightWithMargin = 142;
            productButtonsPanel.Height = Math.Max(productViewportPanel.Height, (rows * tileHeightWithMargin) + 12);
            UpdateProductScrollBar();
        }

        private void FitCategoryButtonsToViewport()
        {
            if (categoryButtonsPanel == null || categoryViewportPanel == null)
            {
                return;
            }

            categoryButtonsPanel.Width = categoryViewportPanel.Width;
            foreach (Control control in categoryButtonsPanel.Controls)
            {
                control.Width = Math.Max(96, categoryViewportPanel.Width - 4);
            }

            categoryButtonsPanel.Height = Math.Max(categoryViewportPanel.Height, categoryButtonsPanel.PreferredSize.Height + 12);
            UpdateCategoryScrollBar();
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

        private void LayoutSalesHistoryScreen()
        {
            if (salesHistoryGrid == null || saleDetailsTextBox == null)
            {
                return;
            }

            Control listCard = salesHistoryGrid.Parent;
            Control detailCard = saleDetailsTextBox.Parent;
            if (listCard == null || detailCard == null)
            {
                return;
            }

            int pad = 18;
            Control refreshButton = FindNamedControl(listCard, "refreshHistoryButton");
            if (refreshButton != null)
            {
                refreshButton.Left = Math.Max(pad, listCard.ClientSize.Width - refreshButton.Width - pad);
                refreshButton.Top = 18;
            }

            salesHistoryGrid.Left = pad;
            salesHistoryGrid.Top = 78;
            salesHistoryGrid.Width = Math.Max(260, listCard.ClientSize.Width - (pad * 2));
            salesHistoryGrid.Height = Math.Max(220, listCard.ClientSize.Height - 96);
            FitSalesHistoryGridColumns();

            Control reprintButton = FindNamedControl(detailCard, "reprintHistoryButton");
            int reprintHeight = 54;
            int reprintTop = Math.Max(88, detailCard.ClientSize.Height - reprintHeight - pad);
            saleDetailsTextBox.Left = pad;
            saleDetailsTextBox.Top = 78;
            saleDetailsTextBox.Width = Math.Max(220, detailCard.ClientSize.Width - (pad * 2));
            saleDetailsTextBox.Height = Math.Max(180, reprintTop - saleDetailsTextBox.Top - 14);

            if (reprintButton != null)
            {
                reprintButton.Left = pad;
                reprintButton.Top = reprintTop;
                reprintButton.Width = saleDetailsTextBox.Width;
                reprintButton.Height = reprintHeight;
            }
        }

        private void LayoutStockStrategyScreen()
        {
            if (stockStrategyGrid == null || categoryStrategyTextBox == null)
            {
                return;
            }

            Control strategyCard = stockStrategyGrid.Parent;
            Control categoryCard = categoryStrategyTextBox.Parent;
            if (strategyCard == null || categoryCard == null)
            {
                return;
            }

            int pad = 18;
            Control applyButton = FindNamedControl(strategyCard, "applyAbcButton");
            if (applyButton != null)
            {
                applyButton.Left = Math.Max(pad, strategyCard.ClientSize.Width - applyButton.Width - pad);
                applyButton.Top = 18;
            }

            stockStrategyGrid.Left = pad;
            stockStrategyGrid.Top = 78;
            stockStrategyGrid.Width = Math.Max(320, strategyCard.ClientSize.Width - (pad * 2));
            stockStrategyGrid.Height = Math.Max(220, strategyCard.ClientSize.Height - 96);
            FitStockStrategyGridColumns();

            categoryStrategyTextBox.Left = pad;
            categoryStrategyTextBox.Top = 78;
            categoryStrategyTextBox.Width = Math.Max(220, categoryCard.ClientSize.Width - (pad * 2));
            categoryStrategyTextBox.Height = Math.Max(220, categoryCard.ClientSize.Height - 96);
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
            if (scannerAutoAddTimer != null)
            {
                scannerAutoAddTimer.Stop();
            }

            if (query.Length == 0 || query.Length < 4)
            {
                RefreshProducts();
            }

            if (query.Length >= 4 && scannerAutoAddTimer != null)
            {
                scannerAutoAddTimer.Start();
            }
        }

        private void SubmitScannerText(bool showMessageIfMissing)
        {
            if (scannerSubmitInProgress || searchTextBox == null)
            {
                return;
            }

            string query = searchTextBox.Text.Trim();
            if (query.Length == 0)
            {
                return;
            }

            Product exactBarcode = FindActiveStoreProduct(query, true);
            if (exactBarcode != null)
            {
                scannerSubmitInProgress = true;
                try
                {
                    AddProductToCart(exactBarcode);
                }
                finally
                {
                    scannerSubmitInProgress = false;
                }
                return;
            }

            if (showMessageIfMissing)
            {
                Product product = FindActiveStoreProduct(query, false);
                if (product != null)
                {
                    AddProductToCart(product);
                    return;
                }

                MessageBox.Show("Product not found");
            }
        }

        private Product FindActiveStoreProduct(string query, bool barcodeOnly)
        {
            query = (query ?? "").Trim();
            if (query.Length == 0)
            {
                return null;
            }

            string normalized = query.ToLowerInvariant();
            Product exactBarcode = store.Database.Products.FirstOrDefault(p =>
                p.StoreId == activeStoreId &&
                !string.IsNullOrEmpty(p.Barcode) &&
                string.Equals(p.Barcode.Trim(), query, StringComparison.OrdinalIgnoreCase));
            if (exactBarcode != null || barcodeOnly)
            {
                return exactBarcode;
            }

            return store.Database.Products.FirstOrDefault(p =>
                p.StoreId == activeStoreId &&
                p.Name.ToLowerInvariant().Contains(normalized));
        }

        private void AddProductToCart(string query)
        {
            Product product = FindActiveStoreProduct(query, false);
            if (product == null)
            {
                MessageBox.Show(Localization.T("ProductNotFound"));
                return;
            }

            AddProductToCart(product);
        }

        private void AddProductToCart(Product product)
        {
            int quantityToAdd = addQuantityInput == null ? 1 : (int)addQuantityInput.Value;
            SaleItem existing = cart.FirstOrDefault(i => i.ProductId == product.Id);
            int existingQuantity = existing == null ? 0 : existing.Quantity;
            if (!CanSellQuantity(product, existingQuantity + quantityToAdd))
            {
                ShowStockLimit(product, existingQuantity + quantityToAdd);
                return;
            }

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
            SaleItem item = GetSelectedCartItem();
            if (item != null) cart.Remove(item);
            selectedCartProductId = 0;
            RefreshCart();
        }

        private void ChangeSelectedQuantity(int change)
        {
            SaleItem item = GetSelectedCartItem();
            if (item == null) return;

            int nextQuantity = item.Quantity + change;
            if (nextQuantity <= 0)
            {
                cart.Remove(item);
                selectedCartProductId = 0;
            }
            else
            {
                Product product = store.Database.Products.FirstOrDefault(p => p.Id == item.ProductId && p.StoreId == activeStoreId);
                if (product != null && !CanSellQuantity(product, nextQuantity))
                {
                    ShowStockLimit(product, nextQuantity);
                    return;
                }

                item.Quantity = nextQuantity;
                selectedCartProductId = item.ProductId;
            }
            RefreshCart();
        }

        private bool CanSellQuantity(Product product, int requestedQuantity)
        {
            if (product == null) return false;
            if (returnCheckBox != null && returnCheckBox.Checked) return true;
            return requestedQuantity <= product.Quantity;
        }

        private void ShowStockLimit(Product product, int requestedQuantity)
        {
            MessageBox.Show(
                Localization.T("OutOfStock") + "\n" +
                product.Name + "\n" +
                Localization.T("AvailableStock") + ": " + product.Quantity + "\n" +
                Localization.T("RequestedQuantity") + ": " + requestedQuantity);
        }

        private bool ValidateCartStock()
        {
            if (returnCheckBox != null && returnCheckBox.Checked)
            {
                return true;
            }

            foreach (SaleItem item in cart)
            {
                Product product = store.Database.Products.FirstOrDefault(p => p.Id == item.ProductId && p.StoreId == activeStoreId);
                if (product == null)
                {
                    MessageBox.Show(Localization.T("ProductNotFound") + ": " + item.ProductName);
                    return false;
                }

                if (!CanSellQuantity(product, item.Quantity))
                {
                    ShowStockLimit(product, item.Quantity);
                    return false;
                }
            }

            return true;
        }

        private void RememberSelectedCartItem()
        {
            if (cartGrid == null || cartGrid.CurrentRow == null)
            {
                return;
            }

            SaleItem item = cartGrid.CurrentRow.DataBoundItem as SaleItem;
            if (item != null)
            {
                selectedCartProductId = item.ProductId;
            }
        }

        private SaleItem GetSelectedCartItem()
        {
            if (selectedCartProductId != 0)
            {
                SaleItem remembered = cart.FirstOrDefault(i => i.ProductId == selectedCartProductId);
                if (remembered != null)
                {
                    return remembered;
                }
            }

            if (cartGrid == null || cartGrid.CurrentRow == null)
            {
                return null;
            }

            SaleItem item = cartGrid.CurrentRow.DataBoundItem as SaleItem;
            if (item != null)
            {
                selectedCartProductId = item.ProductId;
            }
            return item;
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
                    RestoreCartSelection();
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

        private void RestoreCartSelection()
        {
            if (cartGrid == null || cartGrid.Rows.Count == 0 || selectedCartProductId == 0)
            {
                return;
            }

            for (int index = 0; index < cartGrid.Rows.Count; index++)
            {
                SaleItem item = cartGrid.Rows[index].DataBoundItem as SaleItem;
                if (item != null && item.ProductId == selectedCartProductId)
                {
                    cartGrid.ClearSelection();
                    cartGrid.Rows[index].Selected = true;
                    if (cartGrid.Columns.Contains("ProductName"))
                    {
                        cartGrid.CurrentCell = cartGrid.Rows[index].Cells["ProductName"];
                    }
                    return;
                }
            }
        }

        private void Checkout()
        {
            if (cart.Count == 0)
            {
                MessageBox.Show(Localization.T("CartEmpty"));
                return;
            }

            if (!ValidateCartStock())
            {
                RefreshProducts();
                RefreshCart();
                return;
            }

            if (store.GetOpenSession(user.Username) == null)
            {
                MessageBox.Show(Localization.T("OpenCashSessionFirst"));
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

            try
            {
                lastCompletedSale = store.SaveSale(user, activeStoreId, items, paymentComboBox.SelectedItem.ToString(), saleDiscountInput.Value, returnCheckBox.Checked, debtCheckBox.Checked, customerTextBox.Text);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(Localization.T("OutOfStock") + "\n" + ex.Message);
                RefreshProducts();
                RefreshCart();
                return;
            }

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
                MessageBox.Show(Localization.T("NoCompletedSaleToPrint"));
                return;
            }

            PrintReceipt(sale);
        }

        private void RefreshSalesHistory()
        {
            if (salesHistoryGrid == null)
            {
                return;
            }

            int selectedSaleId = selectedHistorySale == null ? 0 : selectedHistorySale.Id;
            List<SaleHistoryRow> rows = store.Database.Sales
                .Where(s => s.StoreId == activeStoreId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SaleHistoryRow
                {
                    SaleId = s.Id,
                    Ticket = s.TicketNumber,
                    Date = s.CreatedAt.ToString("g"),
                    Payment = s.PaymentMethod,
                    Total = Money(s.Total),
                    Status = SaleStatusText(s),
                    Customer = s.CustomerName
                })
                .ToList();

            SetRedraw(salesHistoryGrid, false);
            try
            {
                salesHistoryGrid.DataSource = null;
                salesHistoryGrid.DataSource = rows;
                FormatSalesHistoryGrid();
            }
            finally
            {
                SetRedraw(salesHistoryGrid, true);
            }

            if (rows.Count == 0)
            {
                selectedHistorySale = null;
                if (saleDetailsTextBox != null) saleDetailsTextBox.Text = Localization.T("NoSalesYetMessage");
                return;
            }

            int rowIndex = rows.FindIndex(r => r.SaleId == selectedSaleId);
            if (rowIndex < 0) rowIndex = 0;
            salesHistoryGrid.ClearSelection();
            salesHistoryGrid.Rows[rowIndex].Selected = true;
            salesHistoryGrid.CurrentCell = salesHistoryGrid.Rows[rowIndex].Cells["Ticket"];
            LoadSelectedHistorySale();
        }

        private void LoadSelectedHistorySale()
        {
            if (salesHistoryGrid == null || salesHistoryGrid.CurrentRow == null)
            {
                return;
            }

            SaleHistoryRow row = salesHistoryGrid.CurrentRow.DataBoundItem as SaleHistoryRow;
            if (row == null)
            {
                return;
            }

            selectedHistorySale = store.Database.Sales.FirstOrDefault(s => s.Id == row.SaleId);
            if (saleDetailsTextBox != null)
            {
                saleDetailsTextBox.Text = selectedHistorySale == null ? "" : BuildReceipt(selectedHistorySale);
            }
        }

        private void ReprintSelectedHistorySale()
        {
            if (selectedHistorySale == null)
            {
                MessageBox.Show("Select a ticket first");
                return;
            }

            PrintReceipt(selectedHistorySale);
        }

        private void RefreshStockStrategy()
        {
            if (stockStrategyGrid == null)
            {
                return;
            }

            List<StockStrategyRow> rows = BuildStockStrategyRows();
            SetRedraw(stockStrategyGrid, false);
            try
            {
                stockStrategyGrid.DataSource = null;
                stockStrategyGrid.DataSource = rows;
                FormatStockStrategyGrid();
            }
            finally
            {
                SetRedraw(stockStrategyGrid, true);
            }

            if (categoryStrategyTextBox != null)
            {
                categoryStrategyTextBox.Text = BuildCategoryPerformanceText(rows);
            }
        }

        private List<StockStrategyRow> BuildStockStrategyRows()
        {
            List<ProductSalesMetric> metrics = BuildProductSalesMetrics();
            decimal totalRevenue = metrics.Sum(m => m.Revenue);
            decimal cumulativeRevenue = 0;
            int totalSoldQuantity = metrics.Sum(m => m.QuantitySold);
            List<StockStrategyRow> rows = new List<StockStrategyRow>();

            foreach (ProductSalesMetric metric in metrics)
            {
                cumulativeRevenue += metric.Revenue;
                decimal cumulativePercent = totalRevenue <= 0 ? 100 : (cumulativeRevenue / totalRevenue) * 100;
                string abcClass = AbcClass(cumulativePercent, metric.QuantitySold);
                int suggestedMinimum = SuggestedMinimumStock(metric, abcClass);
                int reorderQuantity = Math.Max(0, suggestedMinimum - metric.Product.Quantity);
                string movement = totalSoldQuantity <= 0
                    ? Localization.T("NoSales")
                    : metric.QuantitySold == metrics.Max(m => m.QuantitySold)
                        ? Localization.T("TopSeller")
                        : metric.QuantitySold == 0
                            ? Localization.T("NoSales")
                            : Localization.T("Normal");

                rows.Add(new StockStrategyRow
                {
                    ProductId = metric.Product.Id,
                    Category = metric.Product.Category,
                    Product = metric.Product.Name,
                    SoldQty = metric.QuantitySold,
                    Revenue = Money(metric.Revenue),
                    Share = totalRevenue <= 0 ? "0%" : ((metric.Revenue / totalRevenue) * 100).ToString("0.0") + "%",
                    ABC = abcClass,
                    CurrentStock = metric.Product.Quantity,
                    CurrentMinimum = metric.Product.MinimumQuantity,
                    SuggestedMinimum = suggestedMinimum,
                    ReorderQty = reorderQuantity,
                    Movement = movement
                });
            }

            return rows
                .OrderBy(r => r.Category)
                .ThenBy(r => r.ABC)
                .ThenByDescending(r => r.SoldQty)
                .ToList();
        }

        private List<ProductSalesMetric> BuildProductSalesMetrics()
        {
            Dictionary<int, ProductSalesMetric> metrics = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .ToDictionary(
                    p => p.Id,
                    p => new ProductSalesMetric
                    {
                        Product = p,
                        QuantitySold = 0,
                        Revenue = 0,
                        FirstSaleAt = DateTime.Today,
                        LastSaleAt = DateTime.Today
                    });

            foreach (Sale sale in store.Database.Sales.Where(s => s.StoreId == activeStoreId))
            {
                foreach (SaleItem item in sale.Items)
                {
                    ProductSalesMetric metric;
                    if (!metrics.TryGetValue(item.ProductId, out metric))
                    {
                        continue;
                    }

                    int signedQuantity = sale.IsReturn ? -item.Quantity : item.Quantity;
                    decimal signedRevenue = sale.IsReturn ? -item.LineTotal : item.LineTotal;
                    metric.QuantitySold += signedQuantity;
                    metric.Revenue += signedRevenue;
                    if (!metric.HasSales || sale.CreatedAt < metric.FirstSaleAt) metric.FirstSaleAt = sale.CreatedAt;
                    if (!metric.HasSales || sale.CreatedAt > metric.LastSaleAt) metric.LastSaleAt = sale.CreatedAt;
                    metric.HasSales = true;
                }
            }

            foreach (ProductSalesMetric metric in metrics.Values)
            {
                if (metric.QuantitySold < 0) metric.QuantitySold = 0;
                if (metric.Revenue < 0) metric.Revenue = 0;
            }

            return metrics.Values
                .OrderByDescending(m => m.Revenue)
                .ThenByDescending(m => m.QuantitySold)
                .ThenBy(m => m.Product.Name)
                .ToList();
        }

        private string AbcClass(decimal cumulativePercent, int quantitySold)
        {
            if (quantitySold <= 0) return "C";
            if (cumulativePercent <= 80) return "A";
            if (cumulativePercent <= 95) return "B";
            return "C";
        }

        private int SuggestedMinimumStock(ProductSalesMetric metric, string abcClass)
        {
            int horizonDays = abcClass == "A" ? 21 : abcClass == "B" ? 14 : 7;
            int safetyFloor = abcClass == "A" ? 10 : abcClass == "B" ? 5 : 2;
            int days = 30;
            if (metric.HasSales)
            {
                days = Math.Max(7, (int)Math.Ceiling((metric.LastSaleAt.Date - metric.FirstSaleAt.Date).TotalDays) + 1);
            }

            decimal averageDailySales = days <= 0 ? 0 : metric.QuantitySold / (decimal)days;
            int demandBasedMinimum = (int)Math.Ceiling(averageDailySales * horizonDays);
            int suggested = Math.Max(safetyFloor, demandBasedMinimum);

            if (metric.QuantitySold == 0)
            {
                suggested = Math.Max(1, Math.Min(metric.Product.MinimumQuantity, 3));
            }

            return Math.Min(9999, suggested);
        }

        private string BuildCategoryPerformanceText(List<StockStrategyRow> rows)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Localization.T("ABCGuideTitle"));
            builder.AppendLine("--------------------------------");
            builder.AppendLine(Localization.T("ABCGuideA"));
            builder.AppendLine(Localization.T("ABCGuideB"));
            builder.AppendLine(Localization.T("ABCGuideC"));
            builder.AppendLine(Localization.T("ABCTableHint"));
            builder.AppendLine();
            builder.AppendLine(Localization.T("CategoryPerformance").ToUpperInvariant());
            builder.AppendLine(Localization.T("Store") + ": " + activeStoreId);
            builder.AppendLine();

            foreach (IGrouping<string, StockStrategyRow> category in rows.GroupBy(r => r.Category).OrderBy(g => g.Key))
            {
                StockStrategyRow mostSold = category.OrderByDescending(r => r.SoldQty).ThenBy(r => r.Product).FirstOrDefault();
                StockStrategyRow lessSold = category.OrderBy(r => r.SoldQty).ThenBy(r => r.Product).FirstOrDefault();
                int aCount = category.Count(r => r.ABC == "A");
                int bCount = category.Count(r => r.ABC == "B");
                int cCount = category.Count(r => r.ABC == "C");
                int reorder = category.Sum(r => r.ReorderQty);

                builder.AppendLine(category.Key);
                builder.AppendLine("--------------------------------");
                builder.AppendLine(Localization.T("MostSold") + ": " + (mostSold == null ? "-" : mostSold.Product + " (" + mostSold.SoldQty + ")"));
                builder.AppendLine(Localization.T("LessSold") + ": " + (lessSold == null ? "-" : lessSold.Product + " (" + lessSold.SoldQty + ")"));
                builder.AppendLine("ABC: A=" + aCount + " | B=" + bCount + " | C=" + cCount);
                builder.AppendLine(Localization.T("ActionNow") + ": " +
                    (reorder > 0 ? Localization.T("OrderNow") + " " + reorder : Localization.T("NoOrderNeeded")));
                builder.AppendLine();
            }

            builder.AppendLine(Localization.T("CategoryAdvice"));
            builder.AppendLine("--------------------------------");
            builder.AppendLine("A: " + Localization.T("AStockRule"));
            builder.AppendLine("B: " + Localization.T("BStockRule"));
            builder.AppendLine("C: " + Localization.T("CStockRule"));
            return builder.ToString();
        }

        private void ApplyAbcStockStrategy()
        {
            if (user.Role != UserRole.Admin)
            {
                return;
            }

            List<StockStrategyRow> rows = BuildStockStrategyRows();
            foreach (StockStrategyRow row in rows)
            {
                Product product = store.Database.Products.FirstOrDefault(p => p.Id == row.ProductId);
                if (product != null)
                {
                    product.MinimumQuantity = row.SuggestedMinimum;
                }
            }

            store.Save();
            RefreshAll();
            MessageBox.Show(Localization.T("ABCUpdated"));
        }

        private void RefreshAdminTools()
        {
            if (user.Role != UserRole.Admin)
            {
                return;
            }

            RefreshDebtGrid();
            RefreshAlertGrid();
            RefreshStockMovementGrid();
            RefreshUsersGrid();
            RefreshRestockProducts();
            RefreshPurchasesGrid();
            RefreshAdvancedReports();
        }

        private void RefreshDebtGrid()
        {
            if (debtGrid == null) return;
            var rows = store.Database.Sales
                .Where(s => s.StoreId == activeStoreId && s.IsDebt)
                .OrderBy(s => s.IsDebtPaid)
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new DebtRow
                {
                    SaleId = s.Id,
                    Ticket = s.TicketNumber,
                    Date = s.CreatedAt.ToString("g"),
                    Customer = s.CustomerName,
                    Total = Money(s.Total),
                    Status = s.IsDebtPaid ? Localization.T("Paid") : Localization.T("Unpaid"),
                    PaidAt = s.IsDebtPaid ? s.DebtPaidAt.ToString("g") : ""
                })
                .ToList();
            debtGrid.DataSource = null;
            debtGrid.DataSource = rows;
            if (debtGrid.Columns.Contains("SaleId")) debtGrid.Columns["SaleId"].Visible = false;
        }

        private void LoadSelectedDebt()
        {
            if (debtGrid == null || debtGrid.CurrentRow == null) return;
            DebtRow row = debtGrid.CurrentRow.DataBoundItem as DebtRow;
            selectedDebtSale = row == null ? null : store.Database.Sales.FirstOrDefault(s => s.Id == row.SaleId);
        }

        private void MarkSelectedDebtPaid()
        {
            LoadSelectedDebt();
            if (selectedDebtSale == null)
            {
                MessageBox.Show("Select a debt first");
                return;
            }

            store.MarkDebtPaid(selectedDebtSale, user.Username);
            RefreshAll();
        }

        private void RefreshAlertGrid()
        {
            if (alertGrid == null) return;
            var rows = store.Database.Products
                .Where(p => p.StoreId == activeStoreId && (p.Quantity <= p.MinimumQuantity || p.ExpiryDate <= DateTime.Today.AddDays(30)))
                .OrderBy(p => p.Quantity <= p.MinimumQuantity ? 0 : 1)
                .ThenBy(p => p.ExpiryDate)
                .Select(p => new AlertRow
                {
                    Product = p.Name,
                    Category = p.Category,
                    Quantity = p.Quantity,
                    Minimum = p.MinimumQuantity,
                    Expiry = p.ExpiryDate.ToShortDateString(),
                    Alert = p.Quantity <= p.MinimumQuantity ? Localization.T("LowStock") : Localization.T("Expiry")
                })
                .ToList();
            alertGrid.DataSource = null;
            alertGrid.DataSource = rows;
        }

        private void RefreshStockMovementGrid()
        {
            if (stockMovementGrid == null) return;
            var rows = store.Database.StockMovements
                .Where(m => m.StoreId == activeStoreId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(250)
                .Select(m => new StockMovementRow
                {
                    Date = m.CreatedAt.ToString("g"),
                    Product = m.ProductName,
                    Type = m.Type,
                    Old = m.OldQuantity,
                    New = m.NewQuantity,
                    Delta = m.Delta,
                    Reason = m.Reason,
                    User = m.Username
                })
                .ToList();
            stockMovementGrid.DataSource = null;
            stockMovementGrid.DataSource = rows;
        }

        private void RefreshUsersGrid()
        {
            if (usersGrid == null) return;
            usersGrid.DataSource = null;
            usersGrid.DataSource = store.Database.Users
                .Select(u => new UserRow { Username = u.Username, FullName = u.FullName, Role = u.Role.ToString() })
                .ToList();
        }

        private void LoadSelectedManagedUser()
        {
            if (usersGrid == null || usersGrid.CurrentRow == null) return;
            UserRow row = usersGrid.CurrentRow.DataBoundItem as UserRow;
            selectedManagedUser = row == null ? null : store.Database.Users.FirstOrDefault(u => u.Username == row.Username);
            if (selectedManagedUser == null) return;
            userUsernameInput.Text = selectedManagedUser.Username;
            userPasswordInput.Text = "";
            userFullNameInput.Text = selectedManagedUser.FullName;
            userRoleInput.SelectedItem = selectedManagedUser.Role.ToString();
        }

        private void SaveManagedUser()
        {
            string username = userUsernameInput.Text.Trim();
            if (username.Length == 0 || (selectedManagedUser == null && userPasswordInput.Text.Trim().Length == 0))
            {
                MessageBox.Show("Username is required. New users also need a password.");
                return;
            }

            User target = new User();
            target.Username = username;
            target.Password = userPasswordInput.Text.Trim();
            target.FullName = userFullNameInput.Text.Trim();
            target.Role = userRoleInput.SelectedItem.ToString() == "Admin" ? UserRole.Admin : UserRole.Cashier;
            store.SaveUser(target);
            RefreshAll();
        }

        private void DeleteManagedUser()
        {
            LoadSelectedManagedUser();
            if (selectedManagedUser == null) return;
            if (selectedManagedUser.Username == user.Username)
            {
                MessageBox.Show("You cannot delete the active user");
                return;
            }
            store.DeleteUser(selectedManagedUser.Username);
            selectedManagedUser = null;
            userUsernameInput.Text = "";
            userPasswordInput.Text = "";
            userFullNameInput.Text = "";
            RefreshAll();
        }

        private void RefreshRestockProducts()
        {
            if (restockProductInput == null) return;
            string selected = restockProductInput.SelectedItem == null ? "" : restockProductInput.SelectedItem.ToString();
            restockProductInput.Items.Clear();
            foreach (Product product in store.Database.Products.Where(p => p.StoreId == activeStoreId).OrderBy(p => p.Name))
            {
                restockProductInput.Items.Add(product.Id + " - " + product.Name);
            }
            if (restockProductInput.Items.Count > 0)
            {
                int index = restockProductInput.Items.IndexOf(selected);
                restockProductInput.SelectedIndex = index >= 0 ? index : 0;
            }
        }

        private void SaveRestock()
        {
            if (restockProductInput == null || restockProductInput.SelectedItem == null) return;
            int productId;
            string raw = restockProductInput.SelectedItem.ToString().Split('-')[0].Trim();
            if (!int.TryParse(raw, out productId)) return;
            Product product = store.Database.Products.FirstOrDefault(p => p.Id == productId);
            store.RecordSupplierPurchase(activeStoreId, product, (int)restockQuantityInput.Value, restockCostInput.Value, restockSupplierInput.Text.Trim(), user.Username);
            restockQuantityInput.Value = 0;
            restockCostInput.Value = 0;
            RefreshAll();
        }

        private void RefreshPurchasesGrid()
        {
            if (purchasesGrid == null) return;
            purchasesGrid.DataSource = null;
            purchasesGrid.DataSource = store.Database.SupplierPurchases
                .Where(p => p.StoreId == activeStoreId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(200)
                .Select(p => new PurchaseRow
                {
                    Date = p.CreatedAt.ToString("g"),
                    Supplier = p.SupplierName,
                    Product = p.ProductName,
                    Quantity = p.Quantity,
                    UnitCost = Money(p.UnitCost),
                    User = p.Username
                })
                .ToList();
        }

        private void RefreshAdvancedReports()
        {
            if (advancedReportKpiLabel == null) return;

            List<Sale> sales = store.Database.Sales.Where(s => s.StoreId == activeStoreId).ToList();
            List<ReportLineMetric> productMetrics = BuildReportLineMetrics(sales);
            decimal revenue = sales.Sum(s => s.IsReturn ? -s.Total : s.Total);
            decimal estimatedProfit = productMetrics.Sum(m => m.Profit);
            decimal margin = revenue <= 0 ? 0 : (estimatedProfit / revenue) * 100;
            decimal stockValue = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .Sum(p => p.Quantity * p.PurchasePrice);
            decimal debtOpen = sales.Where(s => s.IsDebt && !s.IsDebtPaid).Sum(s => s.Total);

            advancedReportKpiLabel.Text =
                Localization.T("Revenue") + ": " + Money(revenue) + "    " +
                Localization.T("Profit") + ": " + Money(estimatedProfit) + "    " +
                Localization.T("Margin") + ": " + margin.ToString("0.0") + "%\n" +
                Localization.T("StockValue") + ": " + Money(stockValue) + "    " +
                Localization.T("OpenDebt") + ": " + Money(debtOpen);

            if (productProfitGrid != null)
            {
                productProfitGrid.DataSource = null;
                productProfitGrid.DataSource = productMetrics
                    .GroupBy(m => m.Product)
                    .Select(g => new ProductProfitRow
                    {
                        Product = g.Key,
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = Money(g.Sum(x => x.Revenue)),
                        Profit = Money(g.Sum(x => x.Profit)),
                        Margin = g.Sum(x => x.Revenue) <= 0 ? "0%" : ((g.Sum(x => x.Profit) / g.Sum(x => x.Revenue)) * 100).ToString("0.0") + "%"
                    })
                    .OrderByDescending(r => ParseMoneyText(r.Profit))
                    .Take(50)
                    .ToList();
            }

            if (cashierPerformanceGrid != null)
            {
                cashierPerformanceGrid.DataSource = null;
                cashierPerformanceGrid.DataSource = sales
                    .GroupBy(s => s.CashierUsername)
                    .Select(g => new CashierReportRow
                    {
                        Cashier = g.Key,
                        Tickets = g.Count(),
                        Revenue = Money(g.Sum(s => s.IsReturn ? -s.Total : s.Total)),
                        Returns = g.Count(s => s.IsReturn),
                        Debts = g.Count(s => s.IsDebt && !s.IsDebtPaid)
                    })
                    .OrderByDescending(r => ParseMoneyText(r.Revenue))
                    .ToList();
            }

            if (paymentSummaryGrid != null)
            {
                paymentSummaryGrid.DataSource = null;
                paymentSummaryGrid.DataSource = sales
                    .GroupBy(s => s.PaymentMethod)
                    .Select(g => new PaymentReportRow
                    {
                        Payment = g.Key,
                        Tickets = g.Count(),
                        Total = Money(g.Sum(s => s.IsReturn ? -s.Total : s.Total))
                    })
                    .OrderByDescending(r => ParseMoneyText(r.Total))
                    .ToList();
            }

            if (categorySummaryGrid != null)
            {
                categorySummaryGrid.DataSource = null;
                categorySummaryGrid.DataSource = productMetrics
                    .GroupBy(m => m.Category)
                    .Select(g => new CategoryReportRow
                    {
                        Category = g.Key,
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = Money(g.Sum(x => x.Revenue)),
                        Profit = Money(g.Sum(x => x.Profit))
                    })
                    .OrderByDescending(r => ParseMoneyText(r.Revenue))
                    .ToList();
            }
        }

        private List<ReportLineMetric> BuildReportLineMetrics(List<Sale> sales)
        {
            List<ReportLineMetric> metrics = new List<ReportLineMetric>();
            foreach (Sale sale in sales)
            {
                foreach (SaleItem item in sale.Items)
                {
                    Product product = store.Database.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    decimal purchasePrice = product == null ? 0 : product.PurchasePrice;
                    string category = product == null ? "" : product.Category;
                    int sign = sale.IsReturn ? -1 : 1;
                    metrics.Add(new ReportLineMetric
                    {
                        Product = item.ProductName,
                        Category = category,
                        Quantity = item.Quantity * sign,
                        Revenue = item.LineTotal * sign,
                        Profit = ((item.UnitPrice - purchasePrice) * item.Quantity - item.Discount) * sign
                    });
                }
            }
            return metrics;
        }

        private decimal ParseMoneyText(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            decimal result;
            string normalized = value.Replace("DT", "").Trim();
            return decimal.TryParse(normalized, out result) ? result : 0;
        }

        private void ManualBackup()
        {
            string path = store.Backup();
            MessageBox.Show(path, Localization.T("BackupDone"));
        }

        private void RestoreBackup()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "XML backup (*.xml)|*.xml|All files (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            store.RestoreBackup(dialog.FileName);
            RefreshAll();
            MessageBox.Show(Localization.T("BackupDone"));
        }

        private void ExportProductsCsv()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Store,Category,Name,Barcode,PurchasePrice,SalePrice,Tax,Quantity,Minimum,Expiry");
            foreach (Product p in store.Database.Products.Where(p => p.StoreId == activeStoreId).OrderBy(p => p.Category).ThenBy(p => p.Name))
            {
                builder.AppendLine(activeStoreId + "," + Csv(p.Category) + "," + Csv(p.Name) + "," + Csv(p.Barcode) + "," + p.PurchasePrice + "," + p.SalePrice + "," + p.TaxRate + "," + p.Quantity + "," + p.MinimumQuantity + "," + p.ExpiryDate.ToShortDateString());
            }
            string path = store.ExportCsv("products-" + activeStoreId + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv", builder.ToString());
            MessageBox.Show(path);
        }

        private void ExportSalesCsv()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Store,Ticket,Date,Cashier,Payment,Total,Debt,Paid,Customer");
            foreach (Sale s in store.Database.Sales.Where(s => s.StoreId == activeStoreId).OrderByDescending(s => s.CreatedAt))
            {
                builder.AppendLine(activeStoreId + "," + s.TicketNumber + "," + Csv(s.CreatedAt.ToString("g")) + "," + Csv(s.CashierUsername) + "," + Csv(s.PaymentMethod) + "," + s.Total + "," + s.IsDebt + "," + s.IsDebtPaid + "," + Csv(s.CustomerName));
            }
            string path = store.ExportCsv("sales-" + activeStoreId + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv", builder.ToString());
            MessageBox.Show(path);
        }

        private void ImportProductsCsv()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Product files (*.csv;*.xlsx)|*.csv;*.xlsx|CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            if (dialog.ShowDialog() != DialogResult.OK) return;
            int count = store.ImportProductsCsv(dialog.FileName, activeStoreId, user.Username);
            RefreshAll();
            MessageBox.Show(count + " products imported/updated.");
        }

        private string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
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
            int oldQuantity = selectedProduct == null ? 0 : selectedProduct.Quantity;
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
            if (store.BarcodeExists(product))
            {
                MessageBox.Show("This barcode already exists in this store.");
                return;
            }

            store.SaveProduct(product, user.Username, "Product form update", oldQuantity);

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
                MessageBox.Show(Localization.T("OpenCashSessionFirst"));
                return;
            }

            store.AddMovement(session, GetSelectedMovementType(), movementAmountInput.Value, movementReasonInput.Text, user.Username);
            movementAmountInput.Value = 0;
            movementReasonInput.Text = "";
            RefreshAll();
        }

        private void FillMovementTypeInput(string selectedValue)
        {
            if (movementTypeInput == null) return;
            movementTypeInput.Items.Clear();
            movementTypeInput.Items.Add(new ComboOption("Withdrawal", Localization.T("Withdrawal")));
            movementTypeInput.Items.Add(new ComboOption("Deposit", Localization.T("Deposit")));
            movementTypeInput.SelectedIndex = selectedValue == "Deposit" ? 1 : 0;
        }

        private string GetSelectedMovementType()
        {
            if (movementTypeInput == null) return "Withdrawal";
            ComboOption option = movementTypeInput.SelectedItem as ComboOption;
            return option == null ? "Withdrawal" : option.Value;
        }

        private string LocalizeMovementType(string value)
        {
            if (value == "Deposit") return Localization.T("Deposit");
            if (value == "Withdrawal") return Localization.T("Withdrawal");
            return value;
        }

        private void CloseShift()
        {
            CashSession session = store.GetOpenSession(user.Username);
            if (session == null)
            {
                MessageBox.Show(Localization.T("NoOpenSession"));
                return;
            }

            decimal counted = countedCashInput.Value;
            decimal cashSales = store.CashSalesForSession(session);
            decimal deposits = session.Movements.Where(m => m.Type == "Deposit").Sum(m => m.Amount);
            decimal withdrawals = session.Movements.Where(m => m.Type == "Withdrawal").Sum(m => m.Amount);
            decimal expected = session.OpeningFund + cashSales + deposits - withdrawals;
            decimal difference = counted - expected;
            decimal bankDeposit = Math.Max(0, counted - 200m);
            string message =
                Localization.T("ExpectedCash") + ": " + Money(expected) + "\n" +
                Localization.T("CountedCash") + ": " + Money(counted) + "\n" +
                Localization.T("Difference") + ": " + Money(difference) + "\n" +
                Localization.T("LeaveInRegister") + ": " + Money(200m) + "\n" +
                Localization.T("BankDeposit") + ": " + Money(bankDeposit) + "\n\n" +
                Localization.T("CloseShiftQuestion");
            if (MessageBox.Show(message, Localization.T("CloseShift"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            store.CloseSession(session, counted);
            string report = BuildShiftClosingReport(session);
            string reportPath = SaveShiftClosingReport(session, report);
            PrintShiftClosingReport(report);
            MessageBox.Show(Localization.T("BackupDone") + "\n" + Localization.T("ClosingReportSaved") + ":\n" + reportPath);
            RefreshAll();
        }

        private void RefreshCash()
        {
            if (cashStatusLabel == null || cashKpiLabel == null)
            {
                if (sessionSummaryLabel != null) sessionSummaryLabel.Text = user.Role == UserRole.Admin ? Localization.T("ProductManagement") : "";
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

        private void FormatSalesHistoryGrid()
        {
            if (salesHistoryGrid == null || salesHistoryGrid.Columns.Count == 0)
            {
                return;
            }

            HideColumn(salesHistoryGrid, "SaleId");
            RenameColumn(salesHistoryGrid, "Ticket", "Ticket");
            RenameColumn(salesHistoryGrid, "Date", "Date");
            RenameColumn(salesHistoryGrid, "Payment", Localization.T("Payment"));
            RenameColumn(salesHistoryGrid, "Total", Localization.T("Total"));
            RenameColumn(salesHistoryGrid, "Status", Localization.T("Status"));
            RenameColumn(salesHistoryGrid, "Customer", Localization.T("Customer"));
            AlignColumn(salesHistoryGrid, "Total", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(salesHistoryGrid, "Status", DataGridViewContentAlignment.MiddleCenter);
            FitSalesHistoryGridColumns();
        }

        private void FormatStockStrategyGrid()
        {
            if (stockStrategyGrid == null || stockStrategyGrid.Columns.Count == 0)
            {
                return;
            }

            HideColumn(stockStrategyGrid, "ProductId");
            HideColumn(stockStrategyGrid, "Revenue");
            HideColumn(stockStrategyGrid, "Share");
            HideColumn(stockStrategyGrid, "Movement");
            RenameColumn(stockStrategyGrid, "Category", Localization.T("CategoryShort"));
            RenameColumn(stockStrategyGrid, "Product", Localization.T("Name"));
            RenameColumn(stockStrategyGrid, "SoldQty", Localization.T("SoldQtyShort"));
            RenameColumn(stockStrategyGrid, "Revenue", "Revenue");
            RenameColumn(stockStrategyGrid, "Share", "Share");
            RenameColumn(stockStrategyGrid, "ABC", Localization.T("ABCClass"));
            RenameColumn(stockStrategyGrid, "CurrentStock", Localization.T("CurrentStockShort"));
            RenameColumn(stockStrategyGrid, "CurrentMinimum", "Min");
            RenameColumn(stockStrategyGrid, "SuggestedMinimum", Localization.T("SuggestedMinimumShort"));
            RenameColumn(stockStrategyGrid, "ReorderQty", Localization.T("ReorderQtyShort"));
            RenameColumn(stockStrategyGrid, "Movement", Localization.T("Movement"));

            AlignColumn(stockStrategyGrid, "SoldQty", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "Revenue", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(stockStrategyGrid, "Share", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "ABC", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "CurrentStock", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "CurrentMinimum", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "SuggestedMinimum", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(stockStrategyGrid, "ReorderQty", DataGridViewContentAlignment.MiddleCenter);
            FitStockStrategyGridColumns();
        }

        private void FitSalesHistoryGridColumns()
        {
            if (salesHistoryGrid == null || salesHistoryGrid.Columns.Count == 0)
            {
                return;
            }

            int visibleWidth = Math.Max(520, salesHistoryGrid.ClientSize.Width - 24);
            SetColumnWidth(salesHistoryGrid, "Ticket", Math.Max(108, visibleWidth * 22 / 100));
            SetColumnWidth(salesHistoryGrid, "Date", Math.Max(126, visibleWidth * 25 / 100));
            SetColumnWidth(salesHistoryGrid, "Payment", Math.Max(82, visibleWidth * 16 / 100));
            SetColumnWidth(salesHistoryGrid, "Total", Math.Max(82, visibleWidth * 16 / 100));
            SetColumnWidth(salesHistoryGrid, "Status", Math.Max(78, visibleWidth * 14 / 100));
            SetColumnWidth(salesHistoryGrid, "Customer", Math.Max(84, visibleWidth * 14 / 100));
        }

        private void FitStockStrategyGridColumns()
        {
            if (stockStrategyGrid == null || stockStrategyGrid.Columns.Count == 0)
            {
                return;
            }

            int visibleWidth = Math.Max(460, stockStrategyGrid.ClientSize.Width - 24);
            SetColumnWidth(stockStrategyGrid, "Category", Math.Max(70, visibleWidth * 13 / 100));
            SetColumnWidth(stockStrategyGrid, "Product", Math.Max(132, visibleWidth * 30 / 100));
            SetColumnWidth(stockStrategyGrid, "SoldQty", Math.Max(48, visibleWidth * 7 / 100));
            SetColumnWidth(stockStrategyGrid, "ABC", Math.Max(42, visibleWidth * 6 / 100));
            SetColumnWidth(stockStrategyGrid, "CurrentStock", Math.Max(48, visibleWidth * 8 / 100));
            SetColumnWidth(stockStrategyGrid, "CurrentMinimum", Math.Max(44, visibleWidth * 7 / 100));
            SetColumnWidth(stockStrategyGrid, "SuggestedMinimum", Math.Max(58, visibleWidth * 9 / 100));
            SetColumnWidth(stockStrategyGrid, "ReorderQty", Math.Max(58, visibleWidth * 9 / 100));
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
            builder.AppendLine("OLY DRUGSTORE POS - " + Localization.T("ReportTitle"));
            builder.AppendLine(Localization.T("Store") + ": " + activeStoreId);
            builder.AppendLine(Localization.T("Generated") + ": " + DateTime.Now.ToString("g"));
            builder.AppendLine();
            builder.AppendLine(Localization.T("SalesCount") + ": " + store.Database.Sales.Count);
            builder.AppendLine(Localization.T("TotalSales") + ": " + store.Database.Sales.Sum(s => s.IsReturn ? -s.Total : s.Total).ToString("0.000") + " DT");
            builder.AppendLine(Localization.T("DebtTotal") + ": " + store.Database.Sales.Where(s => s.IsDebt).Sum(s => s.Total).ToString("0.000") + " DT");
            builder.AppendLine();
            builder.AppendLine(Localization.T("LowStock").ToUpperInvariant());
            foreach (Product product in store.Database.Products.Where(p => p.StoreId == activeStoreId && p.Quantity <= p.MinimumQuantity))
            {
                builder.AppendLine("- " + product.Name + " | " + Localization.T("Quantity") + ": " + product.Quantity);
            }
            builder.AppendLine();
            builder.AppendLine(Localization.T("ExpiringSoon").ToUpperInvariant());
            foreach (Product product in store.Database.Products.Where(p => p.StoreId == activeStoreId && p.ExpiryDate <= DateTime.Today.AddDays(30)))
            {
                builder.AppendLine("- " + product.Name + " | " + Localization.T("Expiry") + ": " + product.ExpiryDate.ToShortDateString());
            }
            if (reportTextBox != null) reportTextBox.Text = builder.ToString();
        }

        private void PrintReceipt(Sale sale)
        {
            string receipt = BuildReceipt(sale);
            try
            {
                PrintDocument document = new PrintDocument();
                document.DocumentName = "Oly receipt " + sale.TicketNumber;
                document.PrintPage += delegate(object sender, PrintPageEventArgs e)
                {
                    using (Font font = new Font("Consolas", 9))
                    {
                        float y = 8;
                        foreach (string line in receipt.Replace("\r\n", "\n").Split('\n'))
                        {
                            e.Graphics.DrawString(line, font, Brushes.Black, 8, y);
                            y += font.GetHeight(e.Graphics) + 2;
                        }
                    }
                };
                using (PrintDialog dialog = new PrintDialog())
                {
                    dialog.Document = document;
                    dialog.UseEXDialog = true;
                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
                document.Print();
            }
            catch
            {
                MessageBox.Show(receipt, Localization.T("PrintTicket"));
            }
        }

        private string BuildShiftClosingReport(CashSession session)
        {
            List<Sale> sales = store.Database.Sales
                .Where(s => s.CashierUsername == session.CashierUsername &&
                            s.StoreId == session.StoreId &&
                            s.CreatedAt >= session.OpenedAt &&
                            s.CreatedAt <= session.ClosedAt)
                .OrderBy(s => s.CreatedAt)
                .ToList();

            decimal cashSales = sales
                .Where(s => s.PaymentMethod == "Cash" && !s.IsDebt)
                .Sum(s => s.IsReturn ? -s.Total : s.Total);
            decimal cardAndOtherSales = sales
                .Where(s => s.PaymentMethod != "Cash" && !s.IsDebt)
                .Sum(s => s.IsReturn ? -s.Total : s.Total);
            decimal debtSales = sales
                .Where(s => s.IsDebt)
                .Sum(s => s.IsReturn ? -s.Total : s.Total);
            decimal returns = sales
                .Where(s => s.IsReturn)
                .Sum(s => s.Total);
            decimal deposits = session.Movements.Where(m => m.Type == "Deposit").Sum(m => m.Amount);
            decimal withdrawals = session.Movements.Where(m => m.Type == "Withdrawal").Sum(m => m.Amount);

            Store activeStore = store.Database.Stores.FirstOrDefault(s => s.Id == session.StoreId);
            string storeName = activeStore == null ? session.StoreId : activeStore.Name;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("OLY DRUGSTORE POS");
            builder.AppendLine(Localization.T("ShiftClosingReport"));
            builder.AppendLine("----------------------------------------");
            builder.AppendLine(Localization.T("ReportDate") + ": " + DateTime.Now.ToString("g"));
            builder.AppendLine(Localization.T("Store") + ": " + storeName + " (" + session.StoreId + ")");
            builder.AppendLine(Localization.T("Cashier") + ": " + session.CashierUsername);
            builder.AppendLine(Localization.T("Session") + ": #" + session.Id);
            builder.AppendLine(Localization.T("Opened") + ": " + session.OpenedAt.ToString("g"));
            builder.AppendLine(Localization.T("Closed") + ": " + session.ClosedAt.ToString("g"));
            builder.AppendLine("----------------------------------------");
            builder.AppendLine(Localization.T("OpeningFund") + ": " + Money(session.OpeningFund));
            builder.AppendLine(Localization.T("CashSales") + ": " + Money(cashSales));
            builder.AppendLine(Localization.T("OtherSales") + ": " + Money(cardAndOtherSales));
            builder.AppendLine(Localization.T("DebtSales") + ": " + Money(debtSales));
            builder.AppendLine(Localization.T("Returns") + ": " + Money(returns));
            builder.AppendLine(Localization.T("Deposits") + ": " + Money(deposits));
            builder.AppendLine(Localization.T("Withdrawals") + ": " + Money(withdrawals));
            builder.AppendLine("----------------------------------------");
            builder.AppendLine(Localization.T("ExpectedCash") + ": " + Money(session.ExpectedCash));
            builder.AppendLine(Localization.T("CountedCash") + ": " + Money(session.CountedCash));
            builder.AppendLine(Localization.T("Difference") + ": " + Money(session.Difference));
            builder.AppendLine(Localization.T("LeaveInRegister") + ": " + Money(200m));
            builder.AppendLine(Localization.T("BankDeposit") + ": " + Money(session.BankDeposit));
            builder.AppendLine("----------------------------------------");
            builder.AppendLine(Localization.T("Ticket") + "s: " + sales.Count);
            foreach (Sale sale in sales)
            {
                builder.AppendLine(
                    sale.TicketNumber + " | " +
                    sale.CreatedAt.ToString("HH:mm") + " | " +
                    sale.PaymentMethod + " | " +
                    (sale.IsDebt ? Localization.T("Debt").ToUpperInvariant() + " | " : "") +
                    (sale.IsReturn ? Localization.T("Return").ToUpperInvariant() + " | " : "") +
                    Money(sale.Total));
            }

            if (session.Movements.Count > 0)
            {
                builder.AppendLine("----------------------------------------");
                builder.AppendLine(Localization.T("CashMovements"));
                foreach (CashMovement movement in session.Movements.OrderBy(m => m.CreatedAt))
                {
                    builder.AppendLine(
                        movement.CreatedAt.ToString("HH:mm") + " | " +
                        LocalizeMovementType(movement.Type) + " | " +
                        Money(movement.Amount) + " | " +
                        movement.Reason);
                }
            }

            builder.AppendLine("----------------------------------------");
            builder.AppendLine(Localization.T("CashierSignature") + ":");
            builder.AppendLine();
            builder.AppendLine(Localization.T("ManagerSignature") + ":");
            return builder.ToString();
        }

        private string SaveShiftClosingReport(CashSession session, string report)
        {
            string reportsDirectory = Path.Combine(Application.StartupPath, "shift-reports");
            Directory.CreateDirectory(reportsDirectory);
            string fileName = "shift-" + session.Id.ToString("000000") + "-" + session.ClosedAt.ToString("yyyyMMdd-HHmmss") + ".txt";
            string reportPath = Path.Combine(reportsDirectory, fileName);
            File.WriteAllText(reportPath, report, Encoding.UTF8);
            return reportPath;
        }

        private void PrintShiftClosingReport(string report)
        {
            try
            {
                string[] lines = report.Replace("\r\n", "\n").Split('\n');
                int lineIndex = 0;
                PrintDocument document = new PrintDocument();
                document.DocumentName = "Oly shift closing report";
                document.PrintPage += delegate(object sender, PrintPageEventArgs e)
                {
                    using (Font font = new Font("Consolas", 9))
                    {
                        float lineHeight = font.GetHeight(e.Graphics) + 2;
                        float y = e.MarginBounds.Top;
                        while (lineIndex < lines.Length && y + lineHeight < e.MarginBounds.Bottom)
                        {
                            e.Graphics.DrawString(lines[lineIndex], font, Brushes.Black, e.MarginBounds.Left, y);
                            y += lineHeight;
                            lineIndex++;
                        }
                        e.HasMorePages = lineIndex < lines.Length;
                    }
                };
                document.Print();
            }
            catch
            {
                MessageBox.Show(report, Localization.T("ShiftClosingReport"));
            }
        }

        private string Money(decimal amount)
        {
            return amount.ToString("0.000") + " DT";
        }

        private string SaleStatusText(Sale sale)
        {
            if (sale.IsDebt && sale.IsReturn) return Localization.T("Debt") + " / " + Localization.T("Return");
            if (sale.IsDebt) return Localization.T("Debt");
            if (sale.IsReturn) return Localization.T("Return");
            return Localization.T("Paid");
        }

        private string BuildReceipt(Sale sale)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("OLY DRUGSTORE");
            builder.AppendLine(Localization.T("Ticket") + ": " + sale.TicketNumber);
            builder.AppendLine(Localization.T("Date") + ": " + sale.CreatedAt.ToString("g"));
            builder.AppendLine(Localization.T("Cashier") + ": " + sale.CashierUsername);
            builder.AppendLine("--------------------------------");
            foreach (SaleItem item in sale.Items)
            {
                builder.AppendLine(item.ProductName);
                builder.AppendLine(item.Quantity + " x " + item.UnitPrice.ToString("0.000") + " = " + item.LineTotal.ToString("0.000"));
            }
            builder.AppendLine("--------------------------------");
            builder.AppendLine(Localization.T("Discount") + ": " + sale.Discount.ToString("0.000"));
            builder.AppendLine(Localization.T("Total").ToUpperInvariant() + ": " + sale.Total.ToString("0.000") + " DT");
            builder.AppendLine(Localization.T("Payment") + ": " + sale.PaymentMethod);
            if (sale.IsDebt) builder.AppendLine(Localization.T("CustomerDebt") + ": " + sale.CustomerName);
            builder.AppendLine(Localization.T("ThankYou"));
            return builder.ToString();
        }

        private class SaleHistoryRow
        {
            public int SaleId { get; set; }
            public string Ticket { get; set; }
            public string Date { get; set; }
            public string Payment { get; set; }
            public string Total { get; set; }
            public string Status { get; set; }
            public string Customer { get; set; }
        }

        private class ComboOption
        {
            public ComboOption(string value, string label)
            {
                Value = value;
                Label = label;
            }

            public string Value { get; private set; }
            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }

        private class ProductSalesMetric
        {
            public Product Product { get; set; }
            public int QuantitySold { get; set; }
            public decimal Revenue { get; set; }
            public bool HasSales { get; set; }
            public DateTime FirstSaleAt { get; set; }
            public DateTime LastSaleAt { get; set; }
        }

        private class StockStrategyRow
        {
            public int ProductId { get; set; }
            public string Category { get; set; }
            public string Product { get; set; }
            public int SoldQty { get; set; }
            public string Revenue { get; set; }
            public string Share { get; set; }
            public string ABC { get; set; }
            public int CurrentStock { get; set; }
            public int CurrentMinimum { get; set; }
            public int SuggestedMinimum { get; set; }
            public int ReorderQty { get; set; }
            public string Movement { get; set; }
        }

        private class DebtRow
        {
            public int SaleId { get; set; }
            public string Ticket { get; set; }
            public string Date { get; set; }
            public string Customer { get; set; }
            public string Total { get; set; }
            public string Status { get; set; }
            public string PaidAt { get; set; }
        }

        private class AlertRow
        {
            public string Alert { get; set; }
            public string Category { get; set; }
            public string Product { get; set; }
            public int Quantity { get; set; }
            public int Minimum { get; set; }
            public string Expiry { get; set; }
        }

        private class StockMovementRow
        {
            public string Date { get; set; }
            public string Product { get; set; }
            public string Type { get; set; }
            public int Old { get; set; }
            public int New { get; set; }
            public int Delta { get; set; }
            public string Reason { get; set; }
            public string User { get; set; }
        }

        private class UserRow
        {
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Role { get; set; }
        }

        private class PurchaseRow
        {
            public string Date { get; set; }
            public string Supplier { get; set; }
            public string Product { get; set; }
            public int Quantity { get; set; }
            public string UnitCost { get; set; }
            public string User { get; set; }
        }

        private class ReportLineMetric
        {
            public string Product { get; set; }
            public string Category { get; set; }
            public int Quantity { get; set; }
            public decimal Revenue { get; set; }
            public decimal Profit { get; set; }
        }

        private class ProductProfitRow
        {
            public string Product { get; set; }
            public int Quantity { get; set; }
            public string Revenue { get; set; }
            public string Profit { get; set; }
            public string Margin { get; set; }
        }

        private class CashierReportRow
        {
            public string Cashier { get; set; }
            public int Tickets { get; set; }
            public string Revenue { get; set; }
            public int Returns { get; set; }
            public int Debts { get; set; }
        }

        private class PaymentReportRow
        {
            public string Payment { get; set; }
            public int Tickets { get; set; }
            public string Total { get; set; }
        }

        private class CategoryReportRow
        {
            public string Category { get; set; }
            public int Quantity { get; set; }
            public string Revenue { get; set; }
            public string Profit { get; set; }
        }
    }
}
