# Oly Drugstore POS

Oly Drugstore POS is a Windows desktop point-of-sale and stock management application designed for small multi-store drugstores/convenience stores in Tunisia.

The first version targets old store computers and peripherals:

- Windows desktop app
- Barcode scanner support
- Ticket printer workflow
- Touch-screen friendly cashier screen
- Local offline data storage
- French and English interface
- Two-store structure ready for future online ordering sync

## Version 1 Scope

- Cashier/admin login
- Product and stock management
- Barcode optional for products
- Manual product search for products without barcode
- Purchase price, selling price, tax, quantity, expiry date
- Low-stock alerts
- Sales cart with cash/card/online/store payment type
- Employee and normal discounts
- Returns/refunds record
- Customer debt record
- Sales history with receipt preview and reprint
- Cash register sessions
- 200 DT fixed opening/remaining cash fund
- Cash withdrawals and deposits
- Shift closing report
- Category performance report with most/least sold products
- ABC stock strategy recommendations
- Admin action to apply ABC minimum stock levels
- Advanced admin reports for revenue, profit, margin, stock value, cashier performance, payment totals, and category totals
- Admin debt management with paid/unpaid tracking
- Admin low-stock and expiry alert tables
- Stock movement history for sales, returns, edits, and purchases
- Supplier purchase/restock workflow
- User management for admin/cashier accounts
- Duplicate barcode protection per store
- Manual backup/restore screen
- CSV/XLSX product import and product/sales export
- Printer selection dialog before ticket printing
- Hashed stored passwords
- Backup when closing shift
- Bilingual FR/EN labels

## Default Demo Logins

```text
Admin
Username: admin
Password: admin

Cashier
Username: cashier
Password: cashier
```

## Build

Run on Windows:

```powershell
.\build.ps1
```

Or:

```cmd
build.bat
```

The executable will be created in:

```text
bin\OlyDrugstorePOS.exe
```

## Data Storage

The app stores local data in:

```text
data\oly-pos-data.xml
```

Backups are saved to:

```text
backups\
```

Shift closing reports are printed and saved to:

```text
shift-reports\
```

## Product Import Format

Admins can import products from CSV or XLSX files. The file should use this column order:

```text
Store,Category,Name,Barcode,PurchasePrice,SalePrice,Tax,Quantity,Minimum,Expiry
```

A starter template is available in:

```text
docs\product-import-template.csv
```

## Future Production Steps

- Replace XML storage with SQLite after confirming target computer support
- Add direct ESC/POS ticket printer commands if the printer model requires it
- Add central backend sync for online store stock
- Add product import/export from Excel or CSV
- Add advanced user permission editor
- Add delivery/order integration
