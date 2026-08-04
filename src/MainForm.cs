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
        private DataGridView productsGrid;
        private DataGridView cartGrid;
        private Label totalLabel;
        private Label sessionSummaryLabel;
        private NumericUpDown saleDiscountInput;
        private ComboBox paymentComboBox;
        private CheckBox employeeDiscountCheckBox;
        private CheckBox returnCheckBox;
        private CheckBox debtCheckBox;
        private TextBox customerTextBox;

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

            Controls.Add(BuildTopBar());

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.Font = UiTheme.FontBold;
            tabs.Padding = new Point(18, 8);
            Controls.Add(tabs);

            BuildSalesTab();
            BuildProductsTab();
            BuildCashTab();
            BuildReportsTab();
            BuildSettingsTab();
            RefreshText();
        }

        private Control BuildTopBar()
        {
            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 74;
            top.BackColor = UiTheme.Primary;

            Label logo = new Label();
            logo.Text = "OLY Drugstore";
            logo.ForeColor = Color.White;
            logo.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            logo.Left = 22;
            logo.Top = 16;
            logo.Width = 260;
            top.Controls.Add(logo);

            Label userLabel = new Label();
            userLabel.Text = user.FullName + "  |  " + user.Role;
            userLabel.ForeColor = Color.FromArgb(203, 213, 225);
            userLabel.Font = UiTheme.FontSmall;
            userLabel.Left = 24;
            userLabel.Top = 48;
            userLabel.Width = 360;
            top.Controls.Add(userLabel);

            storeComboBox = new ComboBox();
            storeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            storeComboBox.Font = UiTheme.FontBold;
            storeComboBox.Left = 430;
            storeComboBox.Top = 20;
            storeComboBox.Width = 260;
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
            top.Controls.Add(storeComboBox);

            languageComboBox = new ComboBox();
            languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageComboBox.Font = UiTheme.FontBold;
            languageComboBox.Items.AddRange(new object[] { "FR", "EN" });
            languageComboBox.SelectedItem = Localization.Language;
            languageComboBox.Left = 710;
            languageComboBox.Top = 20;
            languageComboBox.Width = 90;
            languageComboBox.SelectedIndexChanged += delegate
            {
                Localization.Language = languageComboBox.SelectedItem.ToString();
                RefreshText();
            };
            top.Controls.Add(languageComboBox);

            sessionSummaryLabel = new Label();
            sessionSummaryLabel.ForeColor = Color.White;
            sessionSummaryLabel.Font = UiTheme.FontBold;
            sessionSummaryLabel.TextAlign = ContentAlignment.MiddleRight;
            sessionSummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sessionSummaryLabel.Left = 830;
            sessionSummaryLabel.Top = 20;
            sessionSummaryLabel.Width = 320;
            sessionSummaryLabel.Height = 30;
            top.Controls.Add(sessionSummaryLabel);

            return top;
        }

        private void BuildSalesTab()
        {
            TabPage tab = NewTab("salesTab");

            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            tab.Controls.Add(shell);

            Panel productCard = UiTheme.CardPanel();
            productCard.Dock = DockStyle.Fill;
            productCard.Padding = new Padding(18);
            shell.Controls.Add(productCard, 0, 0);

            Label productTitle = CardTitle("Catalogue / Scanner", 18, 14);
            productCard.Controls.Add(productTitle);

            searchTextBox = UiTheme.TextInput(18, 56, 420);
            searchTextBox.Font = new Font("Segoe UI", 16, FontStyle.Bold);
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

            productsGrid = UiTheme.Grid();
            productsGrid.Left = 18;
            productsGrid.Top = 122;
            productsGrid.Width = 640;
            productsGrid.Height = 505;
            productsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            productsGrid.CellDoubleClick += delegate
            {
                if (productsGrid.CurrentRow == null) return;
                Product product = productsGrid.CurrentRow.DataBoundItem as Product;
                if (product != null) AddProductToCart(!string.IsNullOrEmpty(product.Barcode) ? product.Barcode : product.Name);
            };
            productCard.Controls.Add(productsGrid);

            Panel checkoutCard = UiTheme.CardPanel();
            checkoutCard.Dock = DockStyle.Fill;
            checkoutCard.Padding = new Padding(18);
            shell.Controls.Add(checkoutCard, 1, 0);

            checkoutCard.Controls.Add(CardTitle("Ticket en cours", 18, 14));

            cartGrid = UiTheme.Grid();
            cartGrid.Left = 18;
            cartGrid.Top = 56;
            cartGrid.Width = 450;
            cartGrid.Height = 300;
            cartGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            checkoutCard.Controls.Add(cartGrid);

            Button removeButton = UiTheme.SecondaryButton(Localization.T("Remove"));
            removeButton.Name = "removeButton";
            removeButton.Left = 18;
            removeButton.Top = 372;
            removeButton.Width = 145;
            removeButton.Height = 42;
            removeButton.Click += delegate { RemoveSelectedCartItem(); };
            checkoutCard.Controls.Add(removeButton);

            employeeDiscountCheckBox = new CheckBox();
            employeeDiscountCheckBox.Name = "employeeDiscountCheckBox";
            employeeDiscountCheckBox.Left = 180;
            employeeDiscountCheckBox.Top = 382;
            employeeDiscountCheckBox.Width = 230;
            employeeDiscountCheckBox.Font = UiTheme.FontBold;
            employeeDiscountCheckBox.CheckedChanged += delegate { ApplyEmployeeDiscount(); };
            checkoutCard.Controls.Add(employeeDiscountCheckBox);

            AddLabel(checkoutCard, "discountLabel", Localization.T("Discount"), 18, 435);
            saleDiscountInput = MoneyInput();
            saleDiscountInput.Left = 18;
            saleDiscountInput.Top = 462;
            saleDiscountInput.Width = 130;
            saleDiscountInput.ValueChanged += delegate { RefreshCart(); };
            checkoutCard.Controls.Add(saleDiscountInput);

            AddLabel(checkoutCard, "paymentLabel", Localization.T("Payment"), 170, 435);
            paymentComboBox = new ComboBox();
            paymentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentComboBox.Items.AddRange(new object[] { "Cash", "Card", "Online", "In store" });
            paymentComboBox.SelectedIndex = 0;
            paymentComboBox.Left = 170;
            paymentComboBox.Top = 462;
            paymentComboBox.Width = 150;
            checkoutCard.Controls.Add(paymentComboBox);

            returnCheckBox = Check(Localization.T("Return"), "returnCheckBox", 340, 462);
            checkoutCard.Controls.Add(returnCheckBox);

            debtCheckBox = Check(Localization.T("Debt"), "debtCheckBox", 18, 512);
            checkoutCard.Controls.Add(debtCheckBox);

            AddLabel(checkoutCard, "customerLabel", Localization.T("Customer"), 170, 500);
            customerTextBox = UiTheme.TextInput(170, 525, 230);
            checkoutCard.Controls.Add(customerTextBox);

            totalLabel = new Label();
            totalLabel.Left = 18;
            totalLabel.Top = 585;
            totalLabel.Width = 450;
            totalLabel.Height = 48;
            totalLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            totalLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            totalLabel.ForeColor = UiTheme.Accent;
            checkoutCard.Controls.Add(totalLabel);

            Button checkoutButton = UiTheme.PrimaryButton(Localization.T("Checkout"));
            checkoutButton.Name = "checkoutButton";
            checkoutButton.Left = 18;
            checkoutButton.Top = 645;
            checkoutButton.Width = 450;
            checkoutButton.Height = 62;
            checkoutButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            checkoutButton.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            checkoutButton.Click += delegate { Checkout(); };
            checkoutCard.Controls.Add(checkoutButton);
        }

        private void BuildProductsTab()
        {
            TabPage tab = NewTab("productsTab");
            TableLayoutPanel shell = PageGrid(2, 1);
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
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
            stockGrid.Height = 610;
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
            salePriceInput = AddMoneyField(form, "salePriceLabel", 204, 274);
            taxInput = AddMoneyField(form, "taxLabel", 22, 340);
            quantityInput = AddNumberField(form, "quantityLabel", 204, 340);
            minimumInput = AddNumberField(form, "minimumLabel", 22, 406);

            AddLabel(form, "expiryLabel", Localization.T("Expiry"), 204, 406);
            expiryInput = new DateTimePicker();
            expiryInput.Left = 204;
            expiryInput.Top = 432;
            expiryInput.Width = 180;
            form.Controls.Add(expiryInput);

            AddLabel(form, "storeFieldLabel", "Store", 22, 472);
            productStoreInput = new ComboBox();
            productStoreInput.DropDownStyle = ComboBoxStyle.DropDownList;
            productStoreInput.Left = 22;
            productStoreInput.Top = 498;
            productStoreInput.Width = 180;
            foreach (Store item in store.Database.Stores) productStoreInput.Items.Add(item.Id);
            productStoreInput.SelectedIndex = 0;
            form.Controls.Add(productStoreInput);

            Button save = UiTheme.PrimaryButton(Localization.T("SaveProduct"));
            save.Name = "saveProductButton";
            save.Left = 22;
            save.Top = 568;
            save.Width = 362;
            save.Height = 54;
            save.Click += delegate { SaveProduct(); };
            form.Controls.Add(save);
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
            tabs.TabPages["salesTab"].Text = Localization.T("Sales");
            tabs.TabPages["productsTab"].Text = Localization.T("Products");
            tabs.TabPages["cashTab"].Text = Localization.T("Cash");
            tabs.TabPages["reportsTab"].Text = Localization.T("Reports");
            tabs.TabPages["settingsTab"].Text = Localization.T("Settings");

            SetText("addButton", Localization.T("Add"));
            SetText("removeButton", Localization.T("Remove"));
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
            List<Product> products = store.Database.Products
                .Where(p => p.StoreId == activeStoreId)
                .OrderBy(p => p.Name)
                .ToList();

            productsGrid.DataSource = null;
            productsGrid.DataSource = products;
            stockGrid.DataSource = null;
            stockGrid.DataSource = products.ToList();
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

            searchTextBox.Text = "";
            RefreshCart();
        }

        private void RemoveSelectedCartItem()
        {
            if (cartGrid.CurrentRow == null) return;
            SaleItem item = cartGrid.CurrentRow.DataBoundItem as SaleItem;
            if (item != null) cart.Remove(item);
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
            }

            decimal total = Math.Max(0, cart.Sum(i => i.LineTotal) - saleDiscountInput.Value);
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
