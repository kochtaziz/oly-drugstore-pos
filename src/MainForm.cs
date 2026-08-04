using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OlyDrugstorePOS
{
    public class MainForm : Form
    {
        private readonly DataStore store;
        private readonly User user;
        private readonly List<SaleItem> cart = new List<SaleItem>();
        private string activeStoreId = "STORE-1";

        private TabControl tabs;
        private ComboBox storeComboBox;
        private ComboBox languageComboBox;
        private TextBox searchTextBox;
        private FlowLayoutPanel productButtonsPanel;
        private ComboBox categoryFilterComboBox;
        private DataGridView cartGrid;
        private Label totalLabel;
        private Label sessionSummaryLabel;
        private NumericUpDown saleDiscountInput;
        private ComboBox paymentComboBox;
        private CheckBox employeeDiscountCheckBox;
        private CheckBox returnCheckBox;
        private CheckBox debtCheckBox;
        private TextBox customerTextBox;
        private Button increaseQuantityButton;
        private Button decreaseQuantityButton;
        private Button clearCartButton;
        private bool suppressSearchAutoAdd;
        private bool refreshingCategories;

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
            Text = "Oly Drugstore POS - " + user.FullName;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1180, 760);
            BackColor = UiTheme.Background;
            Font = UiTheme.FontNormal;

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = UiTheme.FontBold;
            tabs.Padding = new Point(18, 8);
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
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
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
            logo.Width = 350;
            logo.Height = 36;
            logo.TextAlign = ContentAlignment.MiddleLeft;
            brand.Controls.Add(logo);

            Label userLabel = new Label();
            userLabel.Text = user.FullName + "  |  " + user.Role;
            userLabel.ForeColor = Color.FromArgb(203, 213, 225);
            userLabel.Font = UiTheme.FontSmall;
            userLabel.Left = 2;
            userLabel.Top = 46;
            userLabel.Width = 350;
            userLabel.Height = 22;
            userLabel.TextAlign = ContentAlignment.MiddleLeft;
            brand.Controls.Add(userLabel);

            storeComboBox = new ComboBox();
            storeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            storeComboBox.Font = UiTheme.FontBold;
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
            sessionSummaryLabel.ForeColor = Color.White;
            sessionSummaryLabel.Font = UiTheme.FontBold;
            sessionSummaryLabel.TextAlign = ContentAlignment.MiddleRight;
            sessionSummaryLabel.Dock = DockStyle.Fill;
            sessionSummaryLabel.Margin = new Padding(10, 18, 0, 16);
            top.Controls.Add(sessionSummaryLabel, 3, 0);

            return top;
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
            shell.Controls.Add(productCard, 0, 0);

            Label productTitle = CardTitle("Catalogue / Scanner", 18, 14);
            productCard.Controls.Add(productTitle);

            searchTextBox = UiTheme.TextInput(18, 56, 420);
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

            Button addButton = UiTheme.PrimaryButton(Localization.T("Add"));
            addButton.Name = "addButton";
            addButton.Left = 455;
            addButton.Top = 56;
            addButton.Width = 150;
            addButton.Height = 42;
            addButton.Click += delegate { AddProductToCart(searchTextBox.Text); };
            productCard.Controls.Add(addButton);

            categoryFilterComboBox = new ComboBox();
            categoryFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryFilterComboBox.Font = UiTheme.FontBold;
            categoryFilterComboBox.Left = 18;
            categoryFilterComboBox.Top = 112;
            categoryFilterComboBox.Width = 240;
            categoryFilterComboBox.SelectedIndexChanged += delegate
            {
                if (!refreshingCategories)
                {
                    RefreshProducts();
                }
            };
            productCard.Controls.Add(categoryFilterComboBox);

            productButtonsPanel = new FlowLayoutPanel();
            productButtonsPanel.Left = 18;
            productButtonsPanel.Top = 160;
            productButtonsPanel.Width = 640;
            productButtonsPanel.Height = 345;
            productButtonsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            productButtonsPanel.AutoScroll = true;
            productButtonsPanel.BackColor = Color.White;
            productButtonsPanel.Padding = new Padding(2);
            productCard.Controls.Add(productButtonsPanel);

            Panel checkoutCard = UiTheme.CardPanel();
            checkoutCard.Dock = DockStyle.Fill;
            checkoutCard.Padding = new Padding(18);
            shell.Controls.Add(checkoutCard, 1, 0);

            checkoutCard.Controls.Add(CardTitle("Ticket en cours", 18, 14));

            cartGrid = UiTheme.Grid();
            cartGrid.Left = 18;
            cartGrid.Top = 56;
            cartGrid.Width = 410;
            cartGrid.Height = 220;
            cartGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkoutCard.Controls.Add(cartGrid);

            Button removeButton = UiTheme.SecondaryButton(Localization.T("Remove"));
            removeButton.Name = "removeButton";
            removeButton.Left = 18;
            removeButton.Top = 292;
            removeButton.Width = 96;
            removeButton.Height = 42;
            removeButton.Click += delegate { RemoveSelectedCartItem(); };
            checkoutCard.Controls.Add(removeButton);

            decreaseQuantityButton = UiTheme.SecondaryButton(Localization.T("Decrease"));
            decreaseQuantityButton.Name = "decreaseQuantityButton";
            decreaseQuantityButton.Left = 122;
            decreaseQuantityButton.Top = 292;
            decreaseQuantityButton.Width = 92;
            decreaseQuantityButton.Height = 42;
            decreaseQuantityButton.Click += delegate { ChangeSelectedQuantity(-1); };
            checkoutCard.Controls.Add(decreaseQuantityButton);

            increaseQuantityButton = UiTheme.SecondaryButton(Localization.T("Increase"));
            increaseQuantityButton.Name = "increaseQuantityButton";
            increaseQuantityButton.Left = 222;
            increaseQuantityButton.Top = 292;
            increaseQuantityButton.Width = 92;
            increaseQuantityButton.Height = 42;
            increaseQuantityButton.Click += delegate { ChangeSelectedQuantity(1); };
            checkoutCard.Controls.Add(increaseQuantityButton);

            clearCartButton = UiTheme.SecondaryButton(Localization.T("ClearCart"));
            clearCartButton.Name = "clearCartButton";
            clearCartButton.Left = 322;
            clearCartButton.Top = 292;
            clearCartButton.Width = 116;
            clearCartButton.Height = 42;
            clearCartButton.Click += delegate { cart.Clear(); RefreshCart(); };
            checkoutCard.Controls.Add(clearCartButton);

            employeeDiscountCheckBox = new CheckBox();
            employeeDiscountCheckBox.Name = "employeeDiscountCheckBox";
            employeeDiscountCheckBox.Left = 18;
            employeeDiscountCheckBox.Top = 342;
            employeeDiscountCheckBox.Width = 230;
            employeeDiscountCheckBox.Font = UiTheme.FontBold;
            employeeDiscountCheckBox.CheckedChanged += delegate { ApplyEmployeeDiscount(); };
            checkoutCard.Controls.Add(employeeDiscountCheckBox);

            AddLabel(checkoutCard, "discountLabel", Localization.T("Discount"), 18, 374);
            saleDiscountInput = MoneyInput();
            saleDiscountInput.Left = 18;
            saleDiscountInput.Top = 400;
            saleDiscountInput.Width = 130;
            saleDiscountInput.ValueChanged += delegate { RefreshCart(); };
            checkoutCard.Controls.Add(saleDiscountInput);

            AddLabel(checkoutCard, "paymentLabel", Localization.T("Payment"), 150, 374);
            paymentComboBox = new ComboBox();
            paymentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentComboBox.Items.AddRange(new object[] { "Cash", "Card", "Online", "In store" });
            paymentComboBox.SelectedIndex = 0;
            paymentComboBox.Left = 150;
            paymentComboBox.Top = 400;
            paymentComboBox.Width = 130;
            checkoutCard.Controls.Add(paymentComboBox);

            returnCheckBox = Check(Localization.T("Return"), "returnCheckBox", 250, 342);
            checkoutCard.Controls.Add(returnCheckBox);

            debtCheckBox = Check(Localization.T("Debt"), "debtCheckBox", 340, 342);
            checkoutCard.Controls.Add(debtCheckBox);

            AddLabel(checkoutCard, "customerLabel", Localization.T("Customer"), 300, 374);
            customerTextBox = UiTheme.TextInput(300, 400, 135);
            checkoutCard.Controls.Add(customerTextBox);

            totalLabel = new Label();
            totalLabel.Left = 18;
            totalLabel.Top = 428;
            totalLabel.Width = 410;
            totalLabel.Height = 36;
            totalLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            totalLabel.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            totalLabel.ForeColor = UiTheme.Accent;
            checkoutCard.Controls.Add(totalLabel);

            Button checkoutButton = UiTheme.PrimaryButton(Localization.T("Checkout"));
            checkoutButton.Name = "checkoutButton";
            checkoutButton.Left = 18;
            checkoutButton.Top = 465;
            checkoutButton.Width = 410;
            checkoutButton.Height = 48;
            checkoutButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkoutButton.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            checkoutButton.Click += delegate { Checkout(); };
            checkoutCard.Controls.Add(checkoutButton);
        }

        private void BuildProductsTab()
        {
            TabPage tab = NewTab("productsTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            tab.Controls.Add(shell);

            Panel listCard = UiTheme.CardPanel();
            listCard.Dock = DockStyle.Fill;
            listCard.Padding = new Padding(18);
            shell.Controls.Add(listCard, 0, 0);
            listCard.Controls.Add(CardTitle("Stock par magasin", 18, 14));

            stockGrid = UiTheme.Grid();
            stockGrid.Left = 18;
            stockGrid.Top = 58;
            stockGrid.Width = 710;
            stockGrid.Height = 450;
            stockGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            stockGrid.CellClick += delegate { LoadSelectedProduct(); };
            listCard.Controls.Add(stockGrid);

            Panel form = UiTheme.CardPanel();
            form.Dock = DockStyle.Fill;
            form.Padding = new Padding(22);
            shell.Controls.Add(form, 1, 0);
            form.Controls.Add(CardTitle("Fiche produit", 22, 18));

            productNameInput = AddTextField(form, "productNameLabel", 22, 76, 360);
            productCategoryInput = AddTextField(form, "productCategoryLabel", 22, 142, 360);
            barcodeInput = AddTextField(form, "barcodeLabel", 22, 208, 360);
            purchasePriceInput = AddMoneyField(form, "purchaseLabel", 22, 274);
            salePriceInput = AddMoneyField(form, "salePriceLabel", 245, 274);
            taxInput = AddMoneyField(form, "taxLabel", 22, 340);
            quantityInput = AddNumberField(form, "quantityLabel", 167, 340);
            minimumInput = AddNumberField(form, "minimumLabel", 312, 340);

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
            productStoreInput.Width = 180;
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
            delete.Left = 210;
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
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = 360;
            label.Height = 32;
            label.Font = UiTheme.FontLarge;
            label.ForeColor = UiTheme.Text;
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
            AddLabel(parent, labelName, "", left, top);
            NumericUpDown input = MoneyInput();
            input.Left = left;
            input.Top = top + 26;
            input.Width = 150;
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown AddNumberField(Control parent, string labelName, int left, int top)
        {
            AddLabel(parent, labelName, "", left, top);
            NumericUpDown input = new NumericUpDown();
            input.Minimum = 0;
            input.Maximum = 1000000;
            input.Left = left;
            input.Top = top + 26;
            input.Width = 150;
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
            check.Height = 28;
            check.Font = UiTheme.FontBold;
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
            SetText("removeButton", Localization.T("Remove"));
            SetText("increaseQuantityButton", Localization.T("Increase"));
            SetText("decreaseQuantityButton", Localization.T("Decrease"));
            SetText("clearCartButton", Localization.T("ClearCart"));
            SetText("checkoutButton", Localization.T("Checkout"));
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

            List<Product> products = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .Where(p => categoryFilterComboBox == null ||
                            categoryFilterComboBox.SelectedItem == null ||
                            categoryFilterComboBox.SelectedItem.ToString() == "All" ||
                            p.Category == categoryFilterComboBox.SelectedItem.ToString())
                .Where(p => searchTextBox == null ||
                            string.IsNullOrEmpty(searchTextBox.Text) ||
                            p.Name.ToLowerInvariant().Contains(searchTextBox.Text.ToLowerInvariant()) ||
                            p.Category.ToLowerInvariant().Contains(searchTextBox.Text.ToLowerInvariant()) ||
                            (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(searchTextBox.Text)))
                .OrderBy(p => p.Name)
                .ToList();

            if (productButtonsPanel != null)
            {
                RenderProductButtons(products);
            }

            if (stockGrid != null)
            {
                stockGrid.DataSource = null;
                stockGrid.DataSource = products.ToList();
                FormatProductsGrid(stockGrid, true);
            }
        }

        private void RefreshCategories()
        {
            if (categoryFilterComboBox == null)
            {
                return;
            }

            string current = categoryFilterComboBox.SelectedItem == null ? "All" : categoryFilterComboBox.SelectedItem.ToString();
            List<string> categories = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            refreshingCategories = true;
            try
            {
                categoryFilterComboBox.Items.Clear();
                categoryFilterComboBox.Items.Add("All");
                foreach (string category in categories)
                {
                    categoryFilterComboBox.Items.Add(category);
                }

                if (categoryFilterComboBox.Items.Contains(current))
                {
                    categoryFilterComboBox.SelectedItem = current;
                }
                else
                {
                    categoryFilterComboBox.SelectedIndex = 0;
                }
            }
            finally
            {
                refreshingCategories = false;
            }
        }

        private void RenderProductButtons(List<Product> products)
        {
            productButtonsPanel.SuspendLayout();
            productButtonsPanel.Controls.Clear();

            foreach (Product product in products)
            {
                Button button = new Button();
                button.Width = 190;
                button.Height = 112;
                button.Margin = new Padding(8);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = UiTheme.Border;
                button.BackColor = product.Quantity <= product.MinimumQuantity
                    ? Color.FromArgb(255, 247, 237)
                    : Color.FromArgb(248, 250, 252);
                button.ForeColor = UiTheme.Text;
                button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.Text =
                    product.Category + "\n" +
                    product.Name + "\n" +
                    product.SalePrice.ToString("0.000") + " DT  | Qty " + product.Quantity;
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

            productButtonsPanel.ResumeLayout();
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

            SaleItem existing = cart.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null) existing.Quantity++;
            else
            {
                cart.Add(new SaleItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitPrice = product.SalePrice,
                    Discount = 0,
                    TaxRate = product.TaxRate
                });
            }

            suppressSearchAutoAdd = true;
            searchTextBox.Text = "";
            suppressSearchAutoAdd = false;
            RefreshProducts();
            RefreshCart();
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
                cartGrid.DataSource = null;
                cartGrid.DataSource = cart.ToList();
                FormatCartGrid();
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

            Sale sale = store.SaveSale(user, activeStoreId, items, paymentComboBox.SelectedItem.ToString(), saleDiscountInput.Value, returnCheckBox.Checked, debtCheckBox.Checked, customerTextBox.Text);
            PrintReceipt(sale);

            cart.Clear();
            saleDiscountInput.Value = 0;
            customerTextBox.Text = "";
            returnCheckBox.Checked = false;
            debtCheckBox.Checked = false;
            employeeDiscountCheckBox.Checked = false;
            RefreshAll();
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
            RenameColumn(grid, "Quantity", Localization.T("Quantity"));
            RenameColumn(grid, "MinimumQuantity", Localization.T("Minimum"));
            RenameColumn(grid, "ExpiryDate", Localization.T("Expiry"));

            AlignColumn(grid, "PurchasePrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "SalePrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "TaxRate", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(grid, "Quantity", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(grid, "MinimumQuantity", DataGridViewContentAlignment.MiddleCenter);

            SetColumnWidth(grid, "Name", 160);
            SetColumnWidth(grid, "Category", 105);
            SetColumnWidth(grid, "Barcode", 120);
            SetColumnWidth(grid, "PurchasePrice", 105);
            SetColumnWidth(grid, "SalePrice", 105);
            SetColumnWidth(grid, "TaxRate", 70);
            SetColumnWidth(grid, "Quantity", 80);
            SetColumnWidth(grid, "MinimumQuantity", 80);
            SetColumnWidth(grid, "ExpiryDate", 110);
        }

        private void FormatCartGrid()
        {
            HideColumn(cartGrid, "ProductId");
            HideColumn(cartGrid, "TaxRate");
            HideColumn(cartGrid, "Discount");
            RenameColumn(cartGrid, "ProductName", Localization.T("Name"));
            RenameColumn(cartGrid, "Quantity", Localization.T("Quantity"));
            RenameColumn(cartGrid, "UnitPrice", "Prix");
            RenameColumn(cartGrid, "LineTotal", Localization.T("Total"));
            AlignColumn(cartGrid, "Quantity", DataGridViewContentAlignment.MiddleCenter);
            AlignColumn(cartGrid, "UnitPrice", DataGridViewContentAlignment.MiddleRight);
            AlignColumn(cartGrid, "LineTotal", DataGridViewContentAlignment.MiddleRight);
            SetColumnWidth(cartGrid, "ProductName", 170);
            SetColumnWidth(cartGrid, "Quantity", 70);
            SetColumnWidth(cartGrid, "UnitPrice", 80);
            SetColumnWidth(cartGrid, "LineTotal", 90);
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
