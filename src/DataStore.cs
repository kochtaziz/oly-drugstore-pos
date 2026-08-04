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
                if (EnsureStartupCatalog())
                {
                    Save();
                }
            }
            else
            {
                Database = CreateSeedDatabase();
                EnsureStartupCatalog();
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
