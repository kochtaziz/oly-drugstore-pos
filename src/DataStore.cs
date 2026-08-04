using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace OlyDrugstorePOS
{
    public class DataStore
    {
        private readonly string dataDirectory;
        private readonly string dataPath;
        private readonly string backupDirectory;

        public PosDatabase Database { get; private set; }

        public DataStore()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
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
            }
            else
            {
                Database = CreateSeedDatabase();
                Save();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(dataDirectory);
            XmlSerializer serializer = new XmlSerializer(typeof(PosDatabase));
            using (FileStream stream = File.Create(dataPath))
            {
                serializer.Serialize(stream, Database);
            }
        }

        public string Backup()
        {
            Save();
            Directory.CreateDirectory(backupDirectory);
            string fileName = "oly-pos-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml";
            string target = Path.Combine(backupDirectory, fileName);
            File.Copy(dataPath, target, true);
            return target;
        }

        public User Authenticate(string username, string password)
        {
            return Database.Users.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);
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

            foreach (SaleItem item in items)
            {
                Product product = Database.Products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    if (isReturn)
                    {
                        product.Quantity += item.Quantity;
                    }
                    else
                    {
                        product.Quantity -= item.Quantity;
                    }
                }
            }

            Database.Sales.Add(sale);
            Save();
            return sale;
        }

        public void SaveProduct(Product product)
        {
            if (product.Id == 0)
            {
                product.Id = Database.NextProductId++;
                Database.Products.Add(product);
            }
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
            db.Products.Add(new Product { Id = db.NextProductId++, Name = "Water 1L", Category = "Drinks", Barcode = "619000001", PurchasePrice = 0.6m, SalePrice = 1.0m, TaxRate = 0, Quantity = 48, MinimumQuantity = 10, ExpiryDate = DateTime.Today.AddYears(1), StoreId = "STORE-1" });
            db.Products.Add(new Product { Id = db.NextProductId++, Name = "Chocolate Bar", Category = "Snacks", Barcode = "619000002", PurchasePrice = 1.2m, SalePrice = 2.0m, TaxRate = 0, Quantity = 30, MinimumQuantity = 8, ExpiryDate = DateTime.Today.AddMonths(8), StoreId = "STORE-1" });
            db.Products.Add(new Product { Id = db.NextProductId++, Name = "Tobacco Pack", Category = "Tobacco", Barcode = "", PurchasePrice = 8.0m, SalePrice = 10.0m, TaxRate = 0, Quantity = 20, MinimumQuantity = 5, ExpiryDate = DateTime.Today.AddYears(2), StoreId = "STORE-1" });
            return db;
        }
    }
}
