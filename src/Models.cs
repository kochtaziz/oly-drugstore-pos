using System;
using System.Collections.Generic;

namespace OlyDrugstorePOS
{
    public enum UserRole
    {
        Admin,
        Cashier
    }

    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
    }

    public class Store
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Barcode { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TaxRate { get; set; }
        public int Quantity { get; set; }
        public int MinimumQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string StoreId { get; set; }
    }

    public class SaleItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxRate { get; set; }

        public decimal LineTotal
        {
            get { return Math.Max(0, (UnitPrice * Quantity) - Discount); }
            set { }
        }
    }

    public class Sale
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public string CashierUsername { get; set; }
        public string StoreId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Discount { get; set; }
        public bool IsReturn { get; set; }
        public bool IsDebt { get; set; }
        public bool IsDebtPaid { get; set; }
        public DateTime DebtPaidAt { get; set; }
        public string DebtPaidBy { get; set; }
        public string CustomerName { get; set; }
        public List<SaleItem> Items { get; set; }

        public Sale()
        {
            Items = new List<SaleItem>();
        }

        public decimal Total
        {
            get
            {
                decimal total = 0;
                foreach (SaleItem item in Items)
                {
                    total += item.LineTotal;
                }
                return Math.Max(0, total - Discount);
            }
            set { }
        }
    }

    public class CashMovement
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public string Username { get; set; }
    }

    public class CashSession
    {
        public int Id { get; set; }
        public string StoreId { get; set; }
        public string CashierUsername { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public bool IsOpen { get; set; }
        public decimal OpeningFund { get; set; }
        public decimal CountedCash { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal BankDeposit { get; set; }
        public decimal Difference { get; set; }
        public List<CashMovement> Movements { get; set; }

        public CashSession()
        {
            Movements = new List<CashMovement>();
        }
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StoreId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Type { get; set; }
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        public int Delta { get; set; }
        public string Reason { get; set; }
        public string Username { get; set; }
    }

    public class SupplierPurchase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string StoreId { get; set; }
        public string SupplierName { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string Username { get; set; }
    }

    public class PosDatabase
    {
        public List<User> Users { get; set; }
        public List<Store> Stores { get; set; }
        public List<Product> Products { get; set; }
        public List<Sale> Sales { get; set; }
        public List<CashSession> CashSessions { get; set; }
        public List<StockMovement> StockMovements { get; set; }
        public List<SupplierPurchase> SupplierPurchases { get; set; }
        public int NextProductId { get; set; }
        public int NextSaleId { get; set; }
        public int NextCashSessionId { get; set; }
        public int NextCashMovementId { get; set; }
        public int NextStockMovementId { get; set; }
        public int NextSupplierPurchaseId { get; set; }

        public PosDatabase()
        {
            Users = new List<User>();
            Stores = new List<Store>();
            Products = new List<Product>();
            Sales = new List<Sale>();
            CashSessions = new List<CashSession>();
            StockMovements = new List<StockMovement>();
            SupplierPurchases = new List<SupplierPurchase>();
            NextProductId = 1;
            NextSaleId = 1;
            NextCashSessionId = 1;
            NextCashMovementId = 1;
            NextStockMovementId = 1;
            NextSupplierPurchaseId = 1;
        }
    }
}
