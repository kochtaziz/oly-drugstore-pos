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
        private readonly List<SaleItem> cart;
        private string activeStoreId = "STORE-1";

        private TabControl tabs;
        private TextBox searchTextBox;
        private DataGridView productsGrid;
        private DataGridView cartGrid;
        private Label totalLabel;
        private NumericUpDown saleDiscountInput;
        private ComboBox paymentComboBox;
        private CheckBox returnCheckBox;
        private CheckBox debtCheckBox;
        private CheckBox employeeDiscountCheckBox;
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

        private Label sessionLabel;
        private NumericUpDown movementAmountInput;
        private TextBox movementReasonInput;
        private ComboBox movementTypeInput;
        private NumericUpDown countedCashInput;
        private TextBox reportTextBox;

        public MainForm(DataStore store, User user)
        {
            this.store = store;
            this.user = user;
            cart = new List<SaleItem>();
            BuildUi();
            RefreshAll();
        }

        private void BuildUi()
        {
            Text = "Oly Drugstore POS - " + user.FullName;
            Width = 1180;
            Height = 780;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 56;
            top.BackColor = Color.FromArgb(19, 28, 42);
            Controls.Add(top);

            Label title = new Label();
            title.Text = "Oly Drugstore POS";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.Left = 16;
            title.Top = 12;
            title.Width = 320;
            top.Controls.Add(title);

            ComboBox storeComboBox = new ComboBox();
            storeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            storeComboBox.Left = 360;
            storeComboBox.Top = 13;
            storeComboBox.Width = 190;
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

            ComboBox language = new ComboBox();
            language.DropDownStyle = ComboBoxStyle.DropDownList;
            language.Items.AddRange(new object[] { "FR", "EN" });
            language.SelectedItem = Localization.Language;
            language.Left = 570;
            language.Top = 13;
            language.Width = 80;
            language.SelectedIndexChanged += delegate
            {
                Localization.Language = language.SelectedItem.ToString();
                RefreshText();
            };
            top.Controls.Add(language);

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            Controls.Add(tabs);

            BuildSalesTab();
            BuildProductsTab();
            BuildCashTab();
            BuildReportsTab();
            BuildSettingsTab();
            RefreshText();
        }

        private void BuildSalesTab()
        {
            TabPage tab = new TabPage();
            tab.Name = "salesTab";
            tabs.TabPages.Add(tab);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterDistance = 570;
            tab.Controls.Add(split);

            Panel leftTop = new Panel();
            leftTop.Dock = DockStyle.Top;
            leftTop.Height = 62;
            split.Panel1.Controls.Add(leftTop);

            searchTextBox = new TextBox();
            searchTextBox.Left = 12;
            searchTextBox.Top = 14;
            searchTextBox.Width = 390;
            searchTextBox.Height = 34;
            searchTextBox.Font = new Font("Segoe UI", 14);
            searchTextBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    AddProductToCart(searchTextBox.Text);
                    e.SuppressKeyPress = true;
                }
            };
            leftTop.Controls.Add(searchTextBox);

            Button addButton = new Button();
            addButton.Name = "addButton";
            addButton.Left = 415;
            addButton.Top = 12;
            addButton.Width = 130;
            addButton.Height = 38;
            addButton.Click += delegate { AddProductToCart(searchTextBox.Text); };
            leftTop.Controls.Add(addButton);

            productsGrid = new DataGridView();
            productsGrid.Dock = DockStyle.Fill;
            productsGrid.ReadOnly = true;
            productsGrid.AllowUserToAddRows = false;
            productsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            productsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            productsGrid.CellDoubleClick += delegate
            {
                if (productsGrid.CurrentRow != null)
                {
                    Product product = productsGrid.CurrentRow.DataBoundItem as Product;
                    if (product != null) AddProductToCart(product.Barcode.Length > 0 ? product.Barcode : product.Name);
                }
            };
            split.Panel1.Controls.Add(productsGrid);

            cartGrid = new DataGridView();
            cartGrid.Dock = DockStyle.Top;
            cartGrid.Height = 360;
            cartGrid.ReadOnly = true;
            cartGrid.AllowUserToAddRows = false;
            cartGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            split.Panel2.Controls.Add(cartGrid);

            Panel controls = new Panel();
            controls.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(controls);

            Button removeButton = new Button();
            removeButton.Name = "removeButton";
            removeButton.Left = 12;
            removeButton.Top = 16;
            removeButton.Width = 150;
            removeButton.Height = 44;
            removeButton.Click += delegate { RemoveSelectedCartItem(); };
            controls.Controls.Add(removeButton);

            Label discountLabel = new Label();
            discountLabel.Name = "discountLabel";
            discountLabel.Left = 12;
            discountLabel.Top = 78;
            discountLabel.Width = 160;
            controls.Controls.Add(discountLabel);

            saleDiscountInput = MoneyInput();
            saleDiscountInput.Left = 170;
            saleDiscountInput.Top = 74;
            controls.Controls.Add(saleDiscountInput);
            saleDiscountInput.ValueChanged += delegate { RefreshCart(); };

            employeeDiscountCheckBox = new CheckBox();
            employeeDiscountCheckBox.Name = "employeeDiscountCheckBox";
            employeeDiscountCheckBox.Left = 315;
            employeeDiscountCheckBox.Top = 76;
            employeeDiscountCheckBox.Width = 220;
            employeeDiscountCheckBox.CheckedChanged += delegate { ApplyEmployeeDiscount(); };
            controls.Controls.Add(employeeDiscountCheckBox);

            Label paymentLabel = new Label();
            paymentLabel.Name = "paymentLabel";
            paymentLabel.Left = 12;
            paymentLabel.Top = 124;
            paymentLabel.Width = 160;
            controls.Controls.Add(paymentLabel);

            paymentComboBox = new ComboBox();
            paymentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            paymentComboBox.Items.AddRange(new object[] { "Cash", "Card", "Online", "In store" });
            paymentComboBox.SelectedIndex = 0;
            paymentComboBox.Left = 170;
            paymentComboBox.Top = 120;
            paymentComboBox.Width = 180;
            controls.Controls.Add(paymentComboBox);

            returnCheckBox = new CheckBox();
            returnCheckBox.Name = "returnCheckBox";
            returnCheckBox.Left = 12;
            returnCheckBox.Top = 164;
            returnCheckBox.Width = 160;
            controls.Controls.Add(returnCheckBox);

            debtCheckBox = new CheckBox();
            debtCheckBox.Name = "debtCheckBox";
            debtCheckBox.Left = 170;
            debtCheckBox.Top = 164;
            debtCheckBox.Width = 180;
            controls.Controls.Add(debtCheckBox);

            Label customerLabel = new Label();
            customerLabel.Name = "customerLabel";
            customerLabel.Left = 12;
            customerLabel.Top = 205;
            customerLabel.Width = 160;
            controls.Controls.Add(customerLabel);

            customerTextBox = new TextBox();
            customerTextBox.Left = 170;
            customerTextBox.Top = 200;
            customerTextBox.Width = 300;
            controls.Controls.Add(customerTextBox);

            totalLabel = new Label();
            totalLabel.Left = 12;
            totalLabel.Top = 250;
            totalLabel.Width = 480;
            totalLabel.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            controls.Controls.Add(totalLabel);

            Button checkoutButton = new Button();
            checkoutButton.Name = "checkoutButton";
            checkoutButton.Left = 12;
            checkoutButton.Top = 310;
            checkoutButton.Width = 480;
            checkoutButton.Height = 60;
            checkoutButton.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            checkoutButton.Click += delegate { Checkout(); };
            controls.Controls.Add(checkoutButton);
        }

        private void BuildProductsTab()
        {
            TabPage tab = new TabPage();
            tab.Name = "productsTab";
            tabs.TabPages.Add(tab);

            stockGrid = new DataGridView();
            stockGrid.Dock = DockStyle.Left;
            stockGrid.Width = 650;
            stockGrid.ReadOnly = true;
            stockGrid.AllowUserToAddRows = false;
            stockGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            stockGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            stockGrid.CellClick += delegate { LoadSelectedProduct(); };
            tab.Controls.Add(stockGrid);

            Panel form = new Panel();
            form.Dock = DockStyle.Fill;
            form.Padding = new Padding(20);
            tab.Controls.Add(form);

            productNameInput = AddTextField(form, "productNameLabel", 20, 40);
            productCategoryInput = AddTextField(form, "productCategoryLabel", 20, 100);
            barcodeInput = AddTextField(form, "barcodeLabel", 20, 160);
            purchasePriceInput = AddMoneyField(form, "purchaseLabel", 20, 220);
            salePriceInput = AddMoneyField(form, "salePriceLabel", 20, 280);
            taxInput = AddMoneyField(form, "taxLabel", 20, 340);
            quantityInput = AddNumberField(form, "quantityLabel", 260, 220);
            minimumInput = AddNumberField(form, "minimumLabel", 260, 280);

            Label expiryLabel = new Label();
            expiryLabel.Name = "expiryLabel";
            expiryLabel.Left = 260;
            expiryLabel.Top = 340;
            expiryLabel.Width = 180;
            form.Controls.Add(expiryLabel);

            expiryInput = new DateTimePicker();
            expiryInput.Left = 260;
            expiryInput.Top = 366;
            expiryInput.Width = 200;
            form.Controls.Add(expiryInput);

            productStoreInput = new ComboBox();
            productStoreInput.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (Store item in store.Database.Stores)
            {
                productStoreInput.Items.Add(item.Id);
            }
            productStoreInput.SelectedIndex = 0;
            productStoreInput.Left = 20;
            productStoreInput.Top = 400;
            productStoreInput.Width = 200;
            form.Controls.Add(productStoreInput);

            Button save = new Button();
            save.Name = "saveProductButton";
            save.Left = 20;
            save.Top = 455;
            save.Width = 440;
            save.Height = 50;
            save.Click += delegate { SaveProduct(); };
            form.Controls.Add(save);
        }

        private void BuildCashTab()
        {
            TabPage tab = new TabPage();
            tab.Name = "cashTab";
            tabs.TabPages.Add(tab);

            sessionLabel = new Label();
            sessionLabel.Left = 20;
            sessionLabel.Top = 20;
            sessionLabel.Width = 760;
            sessionLabel.Height = 44;
            sessionLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            tab.Controls.Add(sessionLabel);

            Button open = new Button();
            open.Name = "openShiftButton";
            open.Left = 20;
            open.Top = 85;
            open.Width = 220;
            open.Height = 48;
            open.Click += delegate { store.OpenSession(user.Username, activeStoreId, 200m); RefreshCash(); };
            tab.Controls.Add(open);

            movementTypeInput = new ComboBox();
            movementTypeInput.DropDownStyle = ComboBoxStyle.DropDownList;
            movementTypeInput.Items.AddRange(new object[] { "Withdrawal", "Deposit" });
            movementTypeInput.SelectedIndex = 0;
            movementTypeInput.Left = 20;
            movementTypeInput.Top = 175;
            movementTypeInput.Width = 180;
            tab.Controls.Add(movementTypeInput);

            movementAmountInput = MoneyInput();
            movementAmountInput.Left = 220;
            movementAmountInput.Top = 175;
            tab.Controls.Add(movementAmountInput);

            movementReasonInput = new TextBox();
            movementReasonInput.Left = 370;
            movementReasonInput.Top = 175;
            movementReasonInput.Width = 300;
            tab.Controls.Add(movementReasonInput);

            Button addMovement = new Button();
            addMovement.Name = "addMovementButton";
            addMovement.Left = 690;
            addMovement.Top = 172;
            addMovement.Width = 160;
            addMovement.Height = 34;
            addMovement.Click += delegate { AddMovement(); };
            tab.Controls.Add(addMovement);

            Label countedLabel = new Label();
            countedLabel.Name = "countedLabel";
            countedLabel.Left = 20;
            countedLabel.Top = 255;
            countedLabel.Width = 240;
            tab.Controls.Add(countedLabel);

            countedCashInput = MoneyInput();
            countedCashInput.Left = 260;
            countedCashInput.Top = 250;
            countedCashInput.Width = 160;
            tab.Controls.Add(countedCashInput);

            Button close = new Button();
            close.Name = "closeShiftButton";
            close.Left = 440;
            close.Top = 246;
            close.Width = 220;
            close.Height = 42;
            close.Click += delegate { CloseShift(); };
            tab.Controls.Add(close);
        }

        private void BuildReportsTab()
        {
            TabPage tab = new TabPage();
            tab.Name = "reportsTab";
            tabs.TabPages.Add(tab);

            reportTextBox = new TextBox();
            reportTextBox.Multiline = true;
            reportTextBox.ScrollBars = ScrollBars.Vertical;
            reportTextBox.Dock = DockStyle.Fill;
            reportTextBox.Font = new Font("Consolas", 11);
            reportTextBox.ReadOnly = true;
            tab.Controls.Add(reportTextBox);
        }

        private void BuildSettingsTab()
        {
            TabPage tab = new TabPage();
            tab.Name = "settingsTab";
            tabs.TabPages.Add(tab);

            Label info = new Label();
            info.Left = 20;
            info.Top = 20;
            info.Width = 900;
            info.Height = 180;
            info.Text = "Oly Drugstore POS\n\nData is stored locally and backups are created when shifts close.\nFuture versions will add SQLite, printer profiles, and central online-store sync.";
            tab.Controls.Add(info);
        }

        private TextBox AddTextField(Control parent, string labelName, int left, int top)
        {
            Label label = new Label();
            label.Name = labelName;
            label.Left = left;
            label.Top = top;
            label.Width = 220;
            parent.Controls.Add(label);

            TextBox input = new TextBox();
            input.Left = left;
            input.Top = top + 26;
            input.Width = 200;
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown AddMoneyField(Control parent, string labelName, int left, int top)
        {
            Label label = new Label();
            label.Name = labelName;
            label.Left = left;
            label.Top = top;
            label.Width = 220;
            parent.Controls.Add(label);

            NumericUpDown input = MoneyInput();
            input.Left = left;
            input.Top = top + 26;
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown AddNumberField(Control parent, string labelName, int left, int top)
        {
            Label label = new Label();
            label.Name = labelName;
            label.Left = left;
            label.Top = top;
            label.Width = 220;
            parent.Controls.Add(label);

            NumericUpDown input = new NumericUpDown();
            input.Minimum = 0;
            input.Maximum = 1000000;
            input.Left = left;
            input.Top = top + 26;
            input.Width = 160;
            parent.Controls.Add(input);
            return input;
        }

        private NumericUpDown MoneyInput()
        {
            NumericUpDown input = new NumericUpDown();
            input.DecimalPlaces = 3;
            input.Minimum = 0;
            input.Maximum = 1000000;
            input.Width = 130;
            return input;
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
            searchTextBox.PlaceholderTextCompat(Localization.T("Search"));
            RefreshCart();
            RefreshCash();
        }

        private void SetText(string name, string value)
        {
            Control[] controls = Controls.Find(name, true);
            if (controls.Length > 0)
            {
                controls[0].Text = value;
            }
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
            if (existing != null)
            {
                existing.Quantity++;
            }
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
            if (item != null)
            {
                cart.Remove(item);
                RefreshCart();
            }
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
            cartGrid.DataSource = null;
            cartGrid.DataSource = cart.ToList();
            decimal total = Math.Max(0, cart.Sum(i => i.LineTotal) - saleDiscountInput.Value);
            totalLabel.Text = Localization.T("Total") + ": " + total.ToString("0.000") + " DT";
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

            Sale sale = store.SaveSale(
                user,
                activeStoreId,
                items,
                paymentComboBox.SelectedItem.ToString(),
                saleDiscountInput.Value,
                returnCheckBox.Checked,
                debtCheckBox.Checked,
                customerTextBox.Text);

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
            RefreshProducts();
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
            RefreshCash();
            RefreshReports();
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
            RefreshCash();
            RefreshReports();
        }

        private void RefreshCash()
        {
            CashSession session = store.GetOpenSession(user.Username);
            if (session == null)
            {
                sessionLabel.Text = "No open cash session - opening fund: 200 DT";
                return;
            }

            decimal cashSales = store.CashSalesForSession(session);
            decimal deposits = session.Movements.Where(m => m.Type == "Deposit").Sum(m => m.Amount);
            decimal withdrawals = session.Movements.Where(m => m.Type == "Withdrawal").Sum(m => m.Amount);
            decimal expected = session.OpeningFund + cashSales + deposits - withdrawals;
            sessionLabel.Text = "Open: " + session.OpenedAt.ToString("g") + " | Expected cash: " + expected.ToString("0.000") + " DT";
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
            reportTextBox.Text = builder.ToString();
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

    public static class TextBoxCompatibilityExtensions
    {
        public static void PlaceholderTextCompat(this TextBox textBox, string text)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "";
            }
        }
    }
}
