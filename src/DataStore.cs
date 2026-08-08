using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace OlyDrugstorePOS
{
    public class DataStore
    {
        private const string PasswordHashPrefix = "sha256:";
        private readonly string dataDirectory;
        private readonly string dataPath;
        private readonly string backupDirectory;
        private DateTime lastDataWriteUtc;

        public PosDatabase Database { get; private set; }

        public DataStore()
        {
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dataDirectory = Path.Combine(root, "data");
            backupDirectory = Path.Combine(root, "backups");
            dataPath = Path.Combine(dataDirectory, "oly-pos-data.xml");
            Load();
        }

        public void Load()
        {
            Directory.CreateDirectory(dataDirectory);
            Directory.CreateDirectory(backupDirectory);

            if (File.Exists(dataPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PosDatabase));
                using (FileStream stream = File.OpenRead(dataPath))
                {
                    Database = (PosDatabase)serializer.Deserialize(stream);
                }
                EnsureDatabaseShape();
                if (EnsureStartupCatalog())
                {
                    Save();
                }
                else
                {
                    UpdateDataTimestamp();
                }
            }
            else
            {
                Database = CreateSeedDatabase();
                EnsureDatabaseShape();
                EnsureStartupCatalog();
                Save();
            }
        }

        private void EnsureDatabaseShape()
        {
            if (Database.Users == null) Database.Users = new List<User>();
            if (Database.Stores == null) Database.Stores = new List<Store>();
            if (Database.Products == null) Database.Products = new List<Product>();
            if (Database.Sales == null) Database.Sales = new List<Sale>();
            if (Database.CashSessions == null) Database.CashSessions = new List<CashSession>();
            if (Database.StockMovements == null) Database.StockMovements = new List<StockMovement>();
            if (Database.SupplierPurchases == null) Database.SupplierPurchases = new List<SupplierPurchase>();
            if (Database.NextProductId <= 0) Database.NextProductId = NextId(Database.Products.Select(p => p.Id));
            if (Database.NextSaleId <= 0) Database.NextSaleId = NextId(Database.Sales.Select(s => s.Id));
            if (Database.NextCashSessionId <= 0) Database.NextCashSessionId = NextId(Database.CashSessions.Select(s => s.Id));
            if (Database.NextCashMovementId <= 0) Database.NextCashMovementId = NextId(Database.CashSessions.SelectMany(s => s.Movements ?? new List<CashMovement>()).Select(m => m.Id));
            if (Database.NextStockMovementId <= 0) Database.NextStockMovementId = NextId(Database.StockMovements.Select(m => m.Id));
            if (Database.NextSupplierPurchaseId <= 0) Database.NextSupplierPurchaseId = NextId(Database.SupplierPurchases.Select(p => p.Id));
            foreach (User user in Database.Users)
            {
                if (!IsHashedPassword(user.Password))
                {
                    user.Password = HashPassword(user.Password);
                }
            }
        }

        private int NextId(IEnumerable<int> ids)
        {
            return ids.Any() ? ids.Max() + 1 : 1;
        }

        public void Save()
        {
            Directory.CreateDirectory(dataDirectory);
            XmlSerializer serializer = new XmlSerializer(typeof(PosDatabase));
            using (FileStream stream = File.Create(dataPath))
            {
                serializer.Serialize(stream, Database);
            }
            UpdateDataTimestamp();
        }

        public bool ReloadIfChanged()
        {
            try
            {
                if (!File.Exists(dataPath))
                {
                    return false;
                }

                DateTime currentWriteUtc = File.GetLastWriteTimeUtc(dataPath);
                if (currentWriteUtc == lastDataWriteUtc)
                {
                    return false;
                }

                Load();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private void UpdateDataTimestamp()
        {
            lastDataWriteUtc = File.Exists(dataPath) ? File.GetLastWriteTimeUtc(dataPath) : DateTime.MinValue;
        }

        public string Backup()
        {
            Save();
            Directory.CreateDirectory(backupDirectory);
            string fileName = "oly-pos-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".xml";
            string target = Path.Combine(backupDirectory, fileName);
            File.Copy(dataPath, target, true);
            return target;
        }

        public void RestoreBackup(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Backup file not found", sourcePath);
            }

            Backup();
            File.Copy(sourcePath, dataPath, true);
            Load();
        }

        public User Authenticate(string username, string password)
        {
            return Database.Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                VerifyPassword(password, u.Password));
        }

        private bool IsHashedPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.StartsWith(PasswordHashPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private string HashPassword(string password)
        {
            password = password ?? "";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte item in bytes)
                {
                    builder.Append(item.ToString("x2"));
                }
                return PasswordHashPrefix + builder.ToString();
            }
        }

        private bool VerifyPassword(string input, string stored)
        {
            if (!IsHashedPassword(stored))
            {
                return stored == input;
            }

            return string.Equals(HashPassword(input), stored, StringComparison.OrdinalIgnoreCase);
        }

        public Product FindProduct(string codeOrName)
        {
            string query = (codeOrName ?? "").Trim().ToLowerInvariant();
            if (query.Length == 0)
            {
                return null;
            }

            return Database.Products.FirstOrDefault(p =>
                (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.ToLowerInvariant() == query) ||
                p.Name.ToLowerInvariant().Contains(query));
        }

        public IEnumerable<Product> SearchProducts(string query)
        {
            query = (query ?? "").Trim().ToLowerInvariant();
            if (query.Length == 0)
            {
                return Database.Products;
            }

            return Database.Products.Where(p =>
                p.Name.ToLowerInvariant().Contains(query) ||
                p.Category.ToLowerInvariant().Contains(query) ||
                (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.ToLowerInvariant().Contains(query)));
        }

        public CashSession GetOpenSession(string username)
        {
            return Database.CashSessions.FirstOrDefault(s => s.IsOpen && s.CashierUsername == username);
        }

        public CashSession OpenSession(string username, string storeId, decimal openingFund)
        {
            CashSession existing = GetOpenSession(username);
            if (existing != null)
            {
                return existing;
            }

            CashSession session = new CashSession();
            session.Id = Database.NextCashSessionId++;
            session.StoreId = storeId;
            session.CashierUsername = username;
            session.OpenedAt = DateTime.Now;
            session.IsOpen = true;
            session.OpeningFund = openingFund;
            Database.CashSessions.Add(session);
            Save();
            return session;
        }

        public void AddMovement(CashSession session, string type, decimal amount, string reason, string username)
        {
            CashMovement movement = new CashMovement();
            movement.Id = Database.NextCashMovementId++;
            movement.CreatedAt = DateTime.Now;
            movement.Type = type;
            movement.Amount = amount;
            movement.Reason = reason;
            movement.Username = username;
            session.Movements.Add(movement);
            Save();
        }

        public Sale SaveSale(User cashier, string storeId, List<SaleItem> items, string paymentMethod, decimal discount, bool isReturn, bool isDebt, string customerName)
        {
            Sale sale = new Sale();
            sale.Id = Database.NextSaleId++;
            sale.TicketNumber = "TCK-" + sale.Id.ToString("000000");
            sale.CashierUsername = cashier.Username;
            sale.StoreId = storeId;
            sale.CreatedAt = DateTime.Now;
            sale.PaymentMethod = paymentMethod;
            sale.Discount = discount;
            sale.IsReturn = isReturn;
            sale.IsDebt = isDebt;
            sale.CustomerName = customerName;
            sale.Items = items;

            if (!isReturn)
            {
                foreach (SaleItem item in items)
                {
                    Product product = Database.Products.FirstOrDefault(p => p.Id == item.ProductId && p.StoreId == storeId);
                    if (product == null)
                    {
                        throw new InvalidOperationException("Product not found: " + item.ProductName);
                    }
                    if (item.Quantity > product.Quantity)
                    {
                        throw new InvalidOperationException("Not enough stock for " + product.Name);
                    }
                }
            }

            foreach (SaleItem item in items)
            {
                Product product = Database.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    int oldQuantity = product.Quantity;
                    if (isReturn)
                    {
                        product.Quantity += item.Quantity;
                    }
                    else
                    {
                        product.Quantity -= item.Quantity;
                    }
                    RecordStockMovement(product, oldQuantity, product.Quantity, isReturn ? "Return" : "Sale", sale.TicketNumber, cashier.Username, false);
                }
            }

            Database.Sales.Add(sale);
            Save();
            return sale;
        }

        public void SaveProduct(Product product)
        {
            SaveProduct(product, "", "Product edit");
        }

        public void SaveProduct(Product product, string username, string reason)
        {
            SaveProduct(product, username, reason, product.Quantity);
        }

        public void SaveProduct(Product product, string username, string reason, int oldQuantity)
        {
            if (product.Id == 0)
            {
                product.Id = Database.NextProductId++;
                Database.Products.Add(product);
                RecordStockMovement(product, 0, product.Quantity, "Create", reason, username, false);
            }
            else
            {
                if (oldQuantity != product.Quantity)
                {
                    RecordStockMovement(product, oldQuantity, product.Quantity, "Edit", reason, username, false);
                }
            }
            Save();
        }

        public bool BarcodeExists(Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Barcode))
            {
                return false;
            }

            return Database.Products.Any(p =>
                p.Id != product.Id &&
                p.StoreId == product.StoreId &&
                string.Equals(p.Barcode, product.Barcode, StringComparison.OrdinalIgnoreCase));
        }

        public void RecordSupplierPurchase(string storeId, Product product, int quantity, decimal unitCost, string supplierName, string username)
        {
            if (product == null || quantity <= 0)
            {
                return;
            }

            int oldQuantity = product.Quantity;
            product.Quantity += quantity;
            SupplierPurchase purchase = new SupplierPurchase();
            purchase.Id = Database.NextSupplierPurchaseId++;
            purchase.CreatedAt = DateTime.Now;
            purchase.StoreId = storeId;
            purchase.SupplierName = supplierName;
            purchase.ProductId = product.Id;
            purchase.ProductName = product.Name;
            purchase.Quantity = quantity;
            purchase.UnitCost = unitCost;
            purchase.Username = username;
            Database.SupplierPurchases.Add(purchase);
            RecordStockMovement(product, oldQuantity, product.Quantity, "Purchase", supplierName, username, false);
            Save();
        }

        private void RecordStockMovement(Product product, int oldQuantity, int newQuantity, string type, string reason, string username, bool save)
        {
            StockMovement movement = new StockMovement();
            movement.Id = Database.NextStockMovementId++;
            movement.CreatedAt = DateTime.Now;
            movement.StoreId = product.StoreId;
            movement.ProductId = product.Id;
            movement.ProductName = product.Name;
            movement.Type = type;
            movement.OldQuantity = oldQuantity;
            movement.NewQuantity = newQuantity;
            movement.Delta = newQuantity - oldQuantity;
            movement.Reason = reason;
            movement.Username = username;
            Database.StockMovements.Add(movement);
            if (save) Save();
        }

        public void MarkDebtPaid(Sale sale, string username)
        {
            if (sale == null || !sale.IsDebt || sale.IsDebtPaid)
            {
                return;
            }

            sale.IsDebtPaid = true;
            sale.DebtPaidAt = DateTime.Now;
            sale.DebtPaidBy = username;
            Save();
        }

        public void SaveUser(User target)
        {
            User existing = Database.Users.FirstOrDefault(u => string.Equals(u.Username, target.Username, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                target.Password = HashPassword(target.Password);
                Database.Users.Add(target);
            }
            else
            {
                if (!string.IsNullOrEmpty(target.Password))
                {
                    existing.Password = HashPassword(target.Password);
                }
                existing.FullName = target.FullName;
                existing.Role = target.Role;
            }
            Save();
        }

        public void DeleteUser(string username)
        {
            Database.Users.RemoveAll(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.Username != "admin");
            Save();
        }

        public string ExportCsv(string fileName, string content)
        {
            string exportDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "exports");
            Directory.CreateDirectory(exportDirectory);
            string path = Path.Combine(exportDirectory, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        public int ImportProductsCsv(string sourcePath, string storeId, string username)
        {
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return 0;
            }

            if (string.Equals(Path.GetExtension(sourcePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return ImportProductsXlsx(sourcePath, storeId, username);
            }

            int count = 0;
            string[] lines = File.ReadAllLines(sourcePath);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = SplitCsvLine(lines[i]);
                if (parts.Length < 9) continue;
                if (ImportProductRow(parts, storeId, username, "CSV import")) count++;
            }
            return count;
        }

        private int ImportProductsXlsx(string sourcePath, string storeId, string username)
        {
            int count = 0;
            using (ZipArchive archive = ZipFile.OpenRead(sourcePath))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheetEntry == null)
                {
                    return 0;
                }

                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XDocument sheet;
                using (Stream stream = sheetEntry.Open())
                {
                    sheet = XDocument.Load(stream);
                }

                foreach (XElement row in sheet.Descendants(ns + "row").Skip(1))
                {
                    Dictionary<int, string> cells = new Dictionary<int, string>();
                    foreach (XElement cell in row.Elements(ns + "c"))
                    {
                        string reference = (string)cell.Attribute("r") ?? "";
                        int column = ColumnIndex(reference);
                        cells[column] = CellText(cell, sharedStrings, ns);
                    }

                    string[] parts = new string[10];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = cells.ContainsKey(i) ? cells[i] : "";
                    }

                    if (ImportProductRow(parts, storeId, username, "Excel import")) count++;
                }
            }
            return count;
        }

        private bool ImportProductRow(string[] parts, string storeId, string username, string reason)
        {
            if (parts.Length < 9) return false;
            string category = parts[1];
            string name = parts[2];
            string barcode = parts[3];
            if (string.IsNullOrWhiteSpace(name)) return false;

            Product product = Database.Products.FirstOrDefault(p =>
                p.StoreId == storeId &&
                ((!string.IsNullOrEmpty(barcode) && p.Barcode == barcode) ||
                 string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)));
            if (product == null)
            {
                product = new Product { StoreId = storeId };
            }

            int oldQuantity = product.Quantity;
            product.Category = category;
            product.Name = name;
            product.Barcode = barcode;
            product.PurchasePrice = ParseDecimal(parts[4]);
            product.SalePrice = ParseDecimal(parts[5]);
            product.TaxRate = ParseDecimal(parts[6]);
            product.Quantity = ParseInt(parts[7]);
            product.MinimumQuantity = ParseInt(parts[8]);
            DateTime expiry;
            product.ExpiryDate = parts.Length > 9 && DateTime.TryParse(parts[9], out expiry) ? expiry : DateTime.Today.AddMonths(12);
            SaveProduct(product, username, reason, oldQuantity);
            return true;
        }

        private List<string> ReadSharedStrings(ZipArchive archive)
        {
            List<string> values = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return values;

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument document;
            using (Stream stream = entry.Open())
            {
                document = XDocument.Load(stream);
            }

            foreach (XElement item in document.Descendants(ns + "si"))
            {
                values.Add(string.Concat(item.Descendants(ns + "t").Select(t => (string)t)));
            }
            return values;
        }

        private string CellText(XElement cell, List<string> sharedStrings, XNamespace ns)
        {
            string type = (string)cell.Attribute("t") ?? "";
            XElement value = cell.Element(ns + "v");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(ns + "t").Select(t => (string)t));
            }
            if (value == null) return "";

            string raw = value.Value;
            if (type == "s")
            {
                int index;
                return int.TryParse(raw, out index) && index >= 0 && index < sharedStrings.Count ? sharedStrings[index] : "";
            }
            return raw;
        }

        private int ColumnIndex(string cellReference)
        {
            int index = 0;
            foreach (char c in cellReference)
            {
                if (!char.IsLetter(c)) break;
                index = (index * 26) + (char.ToUpperInvariant(c) - 'A' + 1);
            }
            return Math.Max(0, index - 1);
        }

        private string[] SplitCsvLine(string line)
        {
            List<string> values = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }
            values.Add(current.ToString());
            return values.ToArray();
        }

        private decimal ParseDecimal(string value)
        {
            decimal result;
            value = (value ?? "").Trim().Replace("DT", "").Replace("dt", "").Replace(",", ".");
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0;
        }

        private int ParseInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        public void DeleteProduct(Product product)
        {
            if (product == null)
            {
                return;
            }

            Database.Products.RemoveAll(p => p.Id == product.Id);
            Save();
        }

        public decimal CashSalesForSession(CashSession session)
        {
            return Database.Sales
                .Where(s => s.CashierUsername == session.CashierUsername &&
                            s.CreatedAt >= session.OpenedAt &&
                            (!session.IsOpen ? s.CreatedAt <= session.ClosedAt : true) &&
                            s.PaymentMethod == "Cash" &&
                            !s.IsDebt)
                .Sum(s => s.IsReturn ? -s.Total : s.Total);
        }

        public decimal CardSalesForSession(CashSession session)
        {
            return Database.Sales
                .Where(s => s.CashierUsername == session.CashierUsername &&
                            s.CreatedAt >= session.OpenedAt &&
                            (!session.IsOpen ? s.CreatedAt <= session.ClosedAt : true) &&
                            s.PaymentMethod != "Cash" &&
                            !s.IsDebt)
                .Sum(s => s.IsReturn ? -s.Total : s.Total);
        }

        public void CloseSession(CashSession session, decimal countedCash)
        {
            decimal cashSales = CashSalesForSession(session);
            decimal deposits = session.Movements.Where(m => m.Type == "Deposit").Sum(m => m.Amount);
            decimal withdrawals = session.Movements.Where(m => m.Type == "Withdrawal").Sum(m => m.Amount);

            session.CountedCash = countedCash;
            session.ExpectedCash = session.OpeningFund + cashSales + deposits - withdrawals;
            session.BankDeposit = Math.Max(0, countedCash - 200m);
            session.Difference = countedCash - session.ExpectedCash;
            session.ClosedAt = DateTime.Now;
            session.IsOpen = false;
            Save();
            Backup();
        }

        private PosDatabase CreateSeedDatabase()
        {
            PosDatabase db = new PosDatabase();
            db.Users.Add(new User { Username = "admin", Password = "admin", Role = UserRole.Admin, FullName = "Administrator" });
            db.Users.Add(new User { Username = "cashier", Password = "cashier", Role = UserRole.Cashier, FullName = "Cashier Demo" });
            db.Stores.Add(new Store { Id = "STORE-1", Name = "Oly Drugstore 1", Address = "Tunisia" });
            db.Stores.Add(new Store { Id = "STORE-2", Name = "Oly Drugstore 2", Address = "Tunisia" });
            return db;
        }

        private bool EnsureStartupCatalog()
        {
            bool changed = false;

            changed |= EnsureStore("STORE-1", "Oly Drugstore 1", "Tunisia");
            changed |= EnsureStore("STORE-2", "Oly Drugstore 2", "Tunisia");

            changed |= AddSeedProduct("Safia Water 1.5L", "Drinks", "619100001", 0.650m, 1.000m, 72, 12, 12, "STORE-1");
            changed |= AddSeedProduct("Safia Water 0.5L", "Drinks", "619100002", 0.350m, 0.600m, 96, 18, 12, "STORE-1");
            changed |= AddSeedProduct("Marwa Water 1.5L", "Drinks", "619100003", 0.620m, 0.950m, 60, 12, 12, "STORE-1");
            changed |= AddSeedProduct("Boga Cidre 24cl", "Drinks", "619100004", 1.050m, 1.600m, 48, 10, 9, "STORE-1");
            changed |= AddSeedProduct("Boga Lim 24cl", "Drinks", "619100005", 1.050m, 1.600m, 48, 10, 9, "STORE-1");
            changed |= AddSeedProduct("Apla 24cl", "Drinks", "619100006", 1.000m, 1.500m, 48, 10, 9, "STORE-1");
            changed |= AddSeedProduct("Coca-Cola Can 24cl", "Drinks", "619100007", 1.300m, 2.000m, 36, 8, 9, "STORE-1");
            changed |= AddSeedProduct("Fanta Orange Can 24cl", "Drinks", "619100008", 1.250m, 1.900m, 36, 8, 9, "STORE-1");
            changed |= AddSeedProduct("Tango Juice 1L", "Drinks", "619100009", 2.200m, 3.400m, 24, 6, 8, "STORE-1");
            changed |= AddSeedProduct("Delice Milk 1L", "Dairy", "619100010", 1.250m, 1.700m, 30, 8, 2, "STORE-1");
            changed |= AddSeedProduct("Vitalait Yogurt Cup", "Dairy", "619100011", 0.450m, 0.700m, 60, 12, 1, "STORE-1");
            changed |= AddSeedProduct("Saida Biscuits", "Snacks", "619100012", 0.550m, 0.900m, 70, 15, 10, "STORE-1");
            changed |= AddSeedProduct("Tom Wafer", "Snacks", "619100013", 0.650m, 1.000m, 54, 12, 10, "STORE-1");
            changed |= AddSeedProduct("Maestro Chocolate Bar", "Snacks", "619100014", 1.200m, 1.900m, 45, 10, 8, "STORE-1");
            changed |= AddSeedProduct("Chips 40g", "Snacks", "619100015", 0.850m, 1.300m, 60, 12, 6, "STORE-1");
            changed |= AddSeedProduct("Chewing Gum Pack", "Snacks", "619100016", 0.700m, 1.200m, 40, 8, 14, "STORE-1");
            changed |= AddSeedProduct("Lilas Tissues Pack", "Hygiene", "619100017", 0.750m, 1.200m, 80, 15, 24, "STORE-1");
            changed |= AddSeedProduct("Lilas Toilet Paper 4 Rolls", "Hygiene", "619100018", 3.100m, 4.500m, 25, 6, 24, "STORE-1");
            changed |= AddSeedProduct("Lilas Kitchen Towels", "Household", "619100019", 3.600m, 5.200m, 20, 5, 24, "STORE-1");
            changed |= AddSeedProduct("Hand Sanitizer 100ml", "Hygiene", "619100020", 2.100m, 3.500m, 35, 8, 18, "STORE-1");
            changed |= AddSeedProduct("Liquid Soap 500ml", "Hygiene", "619100021", 2.400m, 3.900m, 30, 6, 18, "STORE-1");
            changed |= AddSeedProduct("Toothpaste 75ml", "Hygiene", "619100022", 2.700m, 4.200m, 32, 8, 20, "STORE-1");
            changed |= AddSeedProduct("Toothbrush Medium", "Hygiene", "619100023", 1.600m, 2.600m, 40, 8, 24, "STORE-1");
            changed |= AddSeedProduct("Baby Wipes 72pcs", "Baby", "619100024", 3.500m, 5.500m, 24, 6, 18, "STORE-1");
            changed |= AddSeedProduct("Peau Douce Diapers", "Baby", "619100025", 16.000m, 22.500m, 12, 3, 18, "STORE-1");
            changed |= AddSeedProduct("Laundry Detergent 1kg", "Household", "619100026", 4.500m, 6.500m, 18, 4, 24, "STORE-1");
            changed |= AddSeedProduct("Dishwashing Liquid 500ml", "Household", "619100027", 1.900m, 3.000m, 28, 6, 24, "STORE-1");
            changed |= AddSeedProduct("Briquet Lighter", "Tobacco", "619100028", 0.500m, 1.000m, 80, 20, 36, "STORE-1");
            changed |= AddSeedProduct("Cigarettes 20 Pack", "Tobacco", "619100029", 8.000m, 10.000m, 35, 8, 24, "STORE-1");
            changed |= AddSeedProduct("Phone Recharge Card 5 DT", "Services", "619100030", 4.800m, 5.000m, 50, 10, 36, "STORE-1");

            changed |= AddSeedProduct("Safia Water 1.5L", "Drinks", "619200001", 0.650m, 1.000m, 48, 10, 12, "STORE-2");
            changed |= AddSeedProduct("Boga Cidre 24cl", "Drinks", "619200002", 1.050m, 1.600m, 30, 8, 9, "STORE-2");
            changed |= AddSeedProduct("Apla 24cl", "Drinks", "619200003", 1.000m, 1.500m, 30, 8, 9, "STORE-2");
            changed |= AddSeedProduct("Saida Biscuits", "Snacks", "619200004", 0.550m, 0.900m, 50, 12, 10, "STORE-2");
            changed |= AddSeedProduct("Maestro Chocolate Bar", "Snacks", "619200005", 1.200m, 1.900m, 30, 8, 8, "STORE-2");
            changed |= AddSeedProduct("Lilas Tissues Pack", "Hygiene", "619200006", 0.750m, 1.200m, 60, 12, 24, "STORE-2");
            changed |= AddSeedProduct("Hand Sanitizer 100ml", "Hygiene", "619200007", 2.100m, 3.500m, 24, 6, 18, "STORE-2");
            changed |= AddSeedProduct("Baby Wipes 72pcs", "Baby", "619200008", 3.500m, 5.500m, 18, 5, 18, "STORE-2");
            changed |= AddSeedProduct("Briquet Lighter", "Tobacco", "619200009", 0.500m, 1.000m, 60, 15, 36, "STORE-2");
            changed |= AddSeedProduct("Phone Recharge Card 5 DT", "Services", "619200010", 4.800m, 5.000m, 40, 10, 36, "STORE-2");

            return changed;
        }

        private bool EnsureStore(string id, string name, string address)
        {
            if (Database.Stores.Any(s => s.Id == id))
            {
                return false;
            }

            Database.Stores.Add(new Store { Id = id, Name = name, Address = address });
            return true;
        }

        private bool AddSeedProduct(string name, string category, string barcode, decimal purchasePrice, decimal salePrice, int quantity, int minimumQuantity, int expiryMonths, string storeId)
        {
            bool exists = Database.Products.Any(p =>
                p.StoreId == storeId &&
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return false;
            }

            Database.Products.Add(new Product
            {
                Id = Database.NextProductId++,
                Name = name,
                Category = category,
                Barcode = barcode,
                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                TaxRate = 0,
                Quantity = quantity,
                MinimumQuantity = minimumQuantity,
                ExpiryDate = DateTime.Today.AddMonths(expiryMonths),
                StoreId = storeId
            });

            return true;
        }
    }
}
