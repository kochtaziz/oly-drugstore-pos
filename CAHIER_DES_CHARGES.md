# Requirements Specification - Oly Drugstore POS

## 1. Project Overview

### 1.1 Project Name

**Oly Drugstore POS**

### 1.2 Project Type

Windows desktop point-of-sale and stock management application for small multi-store drugstores / convenience stores in Tunisia.

### 1.3 Background

The client operates multiple drugstore-style retail locations. Each store has a cashier computer, a USB barcode scanner, and a small receipt printer. The existing software is old, slow, not practical for touch-screen use, and does not properly support modern stock, sales, cash, and multi-store workflows.

The goal is to create a new local desktop application that is fast, simple, touch-friendly, and usable on older Windows store computers.

This project is also designed as the first step toward a future complete retail platform with online ordering, real-time stock visibility, delivery integration, and centralized multi-store synchronization.

### 1.4 Main Objective

Build a reliable POS application that allows the store team to:

- Manage products and stock per store.
- Sell products using a barcode scanner or touch interface.
- Support cash, card, online, and in-store payment records.
- Manage discounts, employee discounts, returns, customer debt, deposits, and withdrawals.
- Open and close cashier shifts.
- Keep a fixed 200 TND cash drawer fund.
- Calculate expected cash, cash difference, and bank deposit.
- Print customer receipts.
- Generate, print, and save shift closing reports.
- Work locally without an internet connection.

## 2. Stakeholders

| Stakeholder | Role |
| --- | --- |
| Business Owner | Owns the stores and validates business requirements. |
| Store Manager | Supervises cashiers, stock, reports, and daily operations. |
| Administrator | Manages products, prices, stores, and stock. |
| Cashier | Processes sales, payments, returns, and shift closing. |
| Developer | Designs, builds, tests, documents, and delivers the application. |

## 3. Project Scope

### 3.1 Included in Version 1

- Windows desktop application.
- French and English interface.
- Admin and cashier login.
- Basic multi-store support.
- Product and stock management.
- Barcode scanning support.
- Manual/touch product selection.
- Product categories.
- Sales cart.
- Quantity controls.
- Manual discounts.
- Employee discount.
- Return records.
- Customer debt records.
- Cash, card, online, and in-store payment methods.
- Cash register sessions.
- Cash deposits and withdrawals.
- Fixed 200 TND opening/remaining cash fund.
- Customer receipt printing.
- Shift closing report generation.
- Automatic printing of shift closing report.
- Local saving of shift closing reports.
- XML local data storage.
- Automatic backup on shift closing.

### 3.2 Not Included in Version 1

- Real-time cloud synchronization.
- Central backend server.
- Customer mobile app.
- Real online payment processing.
- Delivery integration with Yassir, Glovo, Aramex, or similar services.
- Full supplier management.
- Full accounting system.
- Official fiscal tax compliance module.
- Remote web dashboard.
- Advanced user permissions.
- Excel/CSV import and export.

These items are planned as future improvements.

## 4. Technical Constraints

### 4.1 Target Environment

- Windows store computer.
- Possible older hardware.
- Possible Windows XP support depending on available .NET runtime.
- Touch-screen use.
- Mouse and keyboard support.
- USB barcode scanner.
- Windows-compatible receipt printer.
- Offline local usage.

### 4.2 Selected Technology

| Area | Choice |
| --- | --- |
| Application type | Desktop application |
| Programming language | C# |
| UI framework | Windows Forms |
| Version 1 storage | Local XML file |
| Printing | `System.Drawing.Printing.PrintDocument` |
| Operating system | Windows |
| Operating model | Offline first |

### 4.3 Technical Justification

C# WinForms is selected because:

- It works well on Windows cashier computers.
- It is lightweight and fast.
- It is more suitable than a web app for old local store machines.
- It supports local peripherals through Windows.
- It supports local receipt printing.
- It reduces dependency on internet connectivity.

## 5. Functional Requirements

## 5.1 Authentication

### Objective

Restrict access depending on the user role.

### Roles

#### Administrator

The administrator can:

- Log in.
- Select the active store.
- Add products.
- Edit products.
- Delete products.
- Manage purchase prices.
- Manage selling prices.
- Manage quantities.
- Manage minimum stock levels.
- Manage expiry dates.
- View stock per store.

#### Cashier

The cashier can:

- Log in.
- Select the active store.
- Open a cash session.
- Sell products.
- Scan barcodes.
- Search products manually.
- Select products by category.
- Modify quantities in the cart.
- Apply discounts.
- Apply employee discount.
- Mark a sale as return.
- Mark a sale as customer debt.
- Select payment method.
- Checkout.
- Print receipt.
- Add cash deposit.
- Add cash withdrawal.
- Close shift.

### Demo Accounts

| Role | Username | Password |
| --- | --- | --- |
| Admin | `admin` | `admin` |
| Cashier | `cashier` | `cashier` |

In production, these demo credentials must be changed.

## 5.2 Store Management

### Objective

Allow the system to manage multiple stores.

### Store Data

- Store ID.
- Store name.
- Address.

### Initial Stores

- `STORE-1`: Oly Drugstore 1.
- `STORE-2`: Oly Drugstore 2.

### Business Rules

- Each product belongs to a store.
- Stock is separated by store.
- Sales are recorded per store.
- Cash sessions are attached to a store.

## 5.3 Product Management

### Objective

Allow the administrator to add, edit, and delete products available for sale.

### Product Fields

| Field | Description |
| --- | --- |
| ID | Internal automatic product identifier |
| Name | Product name |
| Category | Product category |
| Barcode | Optional barcode scanned by the USB scanner |
| Purchase price | Product buying cost |
| Sale price | Product selling price |
| Tax | Indicative tax value/rate |
| Quantity | Available stock quantity |
| Minimum quantity | Low-stock alert threshold |
| Expiry date | Expiry or reference date |
| Store | Store linked to the product stock |

### Initial Categories

- Drinks.
- Snacks.
- Hygiene.
- Household.
- Baby.
- Dairy.
- Tobacco.
- Services.

### Business Rules

- A product may or may not have a barcode.
- Products without barcode must still be sellable through search or touch selection.
- Selling price must be greater than or equal to 0.
- Quantity must be greater than or equal to 0.
- Minimum quantity is used for low-stock alerts.
- Product deletion must require confirmation.

## 5.4 Administrator Interface

### Objective

Provide a simple interface for stock management.

### Required Elements

- Stock table by store.
- Product form.
- Save product button.
- Delete product button.
- Store selector.
- Price fields.
- Quantity fields.
- Expiry date field.

### UX Rules

- Fields must be readable.
- Important columns must not be cropped.
- The interface must work on different screen sizes.
- Labels must be aligned.
- Admin should only add, edit, or delete products and stock data.

## 5.5 Cashier Interface

### Objective

Allow fast, touch-friendly sales processing.

### Required Screen Elements

- Scanner/search input.
- Quantity input.
- Plus/minus quantity buttons.
- Add button.
- Vertical one-column category list.
- Vertical category scrollbar.
- Product cards.
- Vertical product scrollbar.
- Sales cart.
- Remove item button.
- Decrease quantity button.
- Increase quantity button.
- Clear cart button.
- Employee discount checkbox.
- Return checkbox.
- Debt checkbox.
- Discount input.
- Payment method selector.
- Customer name input.
- Total amount.
- Checkout button.
- Print receipt button.

### Touch UX Rules

- Buttons must be large enough for touch screens.
- Categories must appear in one vertical column.
- Products must be easy to select.
- The cashier should not need unnecessary repeated clicks.
- Barcode scanner input should automatically add matching products.
- Selected quantity should apply directly when adding products.
- Receipt printing must remain a separate button so the cashier can choose whether to print.

## 5.6 Sales Cart

### Objective

Allow the cashier to prepare a sale before checkout.

### Sales Process

1. Cashier opens a shift.
2. Cashier scans or selects a product.
3. Product is added to the cart.
4. Cashier adjusts quantities if needed.
5. Cashier applies discount if needed.
6. Cashier selects payment method.
7. Cashier checks out.
8. Stock is updated.
9. Receipt can be printed if needed.

### Business Rules

- A sale cannot be completed without an open cash session.
- If a product already exists in the cart, its quantity increases.
- If quantity becomes 0 or less, the product is removed from the cart.
- Total amount cannot be negative.
- Normal sale decreases stock.
- Return sale increases stock.

## 5.7 Payment Management

### Payment Methods

- Cash.
- Card.
- Online.
- In store.

### Business Rules

- Cash sales affect expected physical cash.
- Non-cash payments appear in reports but do not increase expected drawer cash.
- Customer debt is tracked separately.

## 5.8 Discounts

### Discount Types

- Manual discount.
- Employee discount.

### Rules

- Employee discount automatically applies 10% of the subtotal.
- Discounts cannot make the final total negative.
- Discounts must appear on the receipt.

## 5.9 Returns

### Objective

Record customer returns.

### Rules

- Return is marked on the sale.
- Return value is handled as negative business activity in reports.
- Returned product quantity is added back to stock.
- Shift closing report must show returns.

## 5.10 Customer Debt

### Objective

Record unpaid sales that the customer will pay later.

### Data

- Customer name.
- Amount.
- Ticket.
- Date.

### Rules

- Debt must appear on the receipt.
- Debt must appear separately in reports.
- Debt must not be counted as collected cash.

## 5.11 Cash Sessions

### Objective

Control the opening and closing cycle of the cashier drawer.

### Opening a Shift

When opening:

- A cash session is created.
- Store is attached.
- Cashier is attached.
- Opening fund is fixed at 200 TND.
- Opening date and time are recorded.

### Closing a Shift

When closing:

- Cashier enters counted cash.
- System calculates expected cash.
- System calculates cash difference.
- System calculates bank deposit.
- System keeps 200 TND as remaining drawer fund.
- Session is closed.
- Data is saved.
- Backup is created.
- Shift closing report is generated.
- Report is saved on the PC.
- Report is printed automatically.

### Formulas

Expected cash:

```text
opening fund + cash sales + deposits - withdrawals
```

Bank deposit:

```text
max(0, counted cash - 200 TND)
```

Cash difference:

```text
counted cash - expected cash
```

## 5.12 Cash Movements

### Movement Types

- Deposit.
- Withdrawal.

### Use Cases

Cash movements are used to record:

- Cash added to the drawer.
- Cash withdrawn for store expenses.
- Exceptional cash movement.

### Fields

- Type.
- Amount.
- Reason.
- User.
- Date.

## 5.13 Customer Receipts

### Objective

Allow the customer to receive a receipt.

### Receipt Content

- Store/application name.
- Ticket number.
- Date.
- Cashier.
- Product list.
- Quantity.
- Unit price.
- Line total.
- Discount.
- Final total.
- Payment method.
- Customer debt note if applicable.
- Thank-you message.

### Rules

- Receipt printing is triggered by a dedicated button.
- Cashier can checkout without printing if the customer does not need a receipt.
- If printing fails, receipt content should be displayed on screen.

## 5.14 Shift Closing Report

### Objective

Provide proof and summary of the cashier shift.

### Report Content

- Application name.
- Report type.
- Report date.
- Store.
- Cashier.
- Session number.
- Opening time.
- Closing time.
- Opening fund.
- Cash sales.
- Card/other sales.
- Debt sales.
- Returns.
- Deposits.
- Withdrawals.
- Expected cash.
- Counted cash.
- Difference.
- Amount left in drawer.
- Bank deposit.
- Ticket list.
- Cash movement list.
- Cashier signature line.
- Manager signature line.

### Saving Location

Shift closing reports are saved to:

```text
bin\shift-reports\
```

### Format

Version 1:

```text
.txt
```

Future possible formats:

- PDF.
- Excel.
- Automatic email.
- Backend synchronization.

## 5.15 Store Reports

### Objective

Show a quick overview of store activity and alerts.

### Content

- Number of sales.
- Total sales.
- Debt total.
- Low-stock products.
- Products expiring soon.

## 5.16 Data Storage and Backup

### Main Storage

Application data is stored locally in:

```text
bin\data\oly-pos-data.xml
```

### Backups

Automatic backups are stored in:

```text
bin\backups\
```

### Rules

- Data must be saved after important operations.
- A backup must be created at shift closing.
- The application must work without internet.

## 6. Non-Functional Requirements

### 6.1 Performance

The application must be fast and smooth:

- Fast startup.
- Fast product search.
- Immediate product add.
- No visible lag during normal cashier operations.
- Smooth touch scrolling.
- No freezing during simple operations.

### 6.2 Usability

- Clear interface.
- Large buttons.
- Touch-screen compatible.
- Vertical category column.
- Product cards.
- Text must not be cropped.
- Prices and quantities must be visible.
- Professional and aligned design.

### 6.3 Reliability

- Data must not be lost after normal operations.
- Sales must update stock.
- Shift closing must save reports.
- Printing errors must not crash the app.

### 6.4 Accessibility

- Readable contrast.
- Simple fonts.
- Usable with touch, mouse, and keyboard.
- French and English labels.

### 6.5 Security

Version 1:

- Username/password login.
- Admin/cashier role separation.

Future improvements:

- Hashed passwords.
- Detailed permissions.
- Audit log.
- Session locking.
- Encrypted backups.

## 7. Functional Architecture

### 7.1 Main Modules

```text
Login
  -> Authentication

MainForm
  -> Main interface
  -> Admin screen
  -> Cashier screen
  -> Cash reports

DataStore
  -> XML loading
  -> XML saving
  -> Backups
  -> Business operations

Models
  -> User
  -> Store
  -> Product
  -> Sale
  -> SaleItem
  -> CashSession
  -> CashMovement

UiTheme
  -> Styling
  -> Buttons
  -> Panels
  -> Tables

Localization
  -> French / English labels
```

### 7.2 Sales Flow

```text
Cashier login
  -> Select store
  -> Open shift
  -> Scan/select product
  -> Cart
  -> Discount/return/debt if needed
  -> Payment method
  -> Checkout
  -> Stock update
  -> Optional receipt printing
```

### 7.3 Shift Closing Flow

```text
Enter counted cash
  -> Calculate expected cash
  -> Calculate difference
  -> Calculate bank deposit
  -> Close session
  -> Save XML data
  -> Create backup
  -> Generate report
  -> Save report
  -> Print report
```

## 8. Data Model

### 8.1 User

| Field | Type | Description |
| --- | --- | --- |
| Username | Text | Login identifier |
| Password | Text | Password |
| Role | Enum | Admin or Cashier |
| FullName | Text | Full name |

### 8.2 Store

| Field | Type | Description |
| --- | --- | --- |
| Id | Text | Store identifier |
| Name | Text | Store name |
| Address | Text | Store address |

### 8.3 Product

| Field | Type | Description |
| --- | --- | --- |
| Id | Number | Product identifier |
| Name | Text | Product name |
| Category | Text | Category |
| Barcode | Text | Barcode |
| PurchasePrice | Decimal | Purchase price |
| SalePrice | Decimal | Selling price |
| TaxRate | Decimal | Tax |
| Quantity | Number | Stock |
| MinimumQuantity | Number | Minimum stock threshold |
| ExpiryDate | Date | Expiry date |
| StoreId | Text | Linked store |

### 8.4 Sale

| Field | Type | Description |
| --- | --- | --- |
| Id | Number | Sale identifier |
| TicketNumber | Text | Ticket number |
| CashierUsername | Text | Cashier |
| StoreId | Text | Store |
| CreatedAt | Date | Sale date |
| PaymentMethod | Text | Payment method |
| Discount | Decimal | Discount |
| IsReturn | Boolean | Return flag |
| IsDebt | Boolean | Debt flag |
| CustomerName | Text | Customer |
| Items | List | Sold items |
| Total | Decimal | Calculated total |

### 8.5 CashSession

| Field | Type | Description |
| --- | --- | --- |
| Id | Number | Session identifier |
| StoreId | Text | Store |
| CashierUsername | Text | Cashier |
| OpenedAt | Date | Opening time |
| ClosedAt | Date | Closing time |
| IsOpen | Boolean | Session status |
| OpeningFund | Decimal | Opening cash fund |
| CountedCash | Decimal | Counted physical cash |
| ExpectedCash | Decimal | Expected cash |
| BankDeposit | Decimal | Bank deposit amount |
| Difference | Decimal | Cash difference |
| Movements | List | Deposits/withdrawals |

## 9. Business Rules

1. Cashier must open a shift before making sales.
2. Only one open shift per cashier is allowed.
3. Opening fund is fixed at 200 TND.
4. At closing, 200 TND remains in the drawer.
5. Remaining counted cash is considered bank deposit.
6. Normal sale decreases stock.
7. Return increases stock.
8. Debt does not count as collected cash.
9. Discount cannot make final total negative.
10. Products without barcode must remain sellable.
11. Scanned barcode should automatically add the product.
12. Receipt printing must be separate from checkout.
13. Shift closing report must always be saved locally.
14. Backup must be created on each shift closing.

## 10. User Interface Requirements

### 10.1 Login Screen

Required elements:

- Application name/logo.
- Username field.
- Password field.
- Language selector.
- Sign-in button.

### 10.2 Admin Screen

Required elements:

- Stock table.
- Product form.
- Save button.
- Delete button.
- Store selector.

### 10.3 Cashier Screen

Required elements:

- Scanner/search field.
- Quantity controls.
- Vertical categories.
- Category scrollbar.
- Product cards.
- Product scrollbar.
- Cart.
- Discount.
- Payment.
- Debt.
- Return.
- Checkout.
- Print.

### 10.4 Cash Session Screen

Required elements:

- Session status.
- Cash KPI.
- Open shift button.
- Counted cash input.
- Close shift button.
- Cash movements.

### 10.5 Reports Screen

Required elements:

- Text report.
- Low-stock alerts.
- Expiry alerts.

## 11. Printing Requirements

### 11.1 Customer Receipt

Receipt must be printed only when requested by the cashier.

### 11.2 Shift Closing Report

Shift report must be printed automatically when closing a shift.

### 11.3 Print Error Handling

If printing fails:

- Application must not crash.
- Content should be displayed on screen.
- Report must remain saved on the PC.

## 12. Generated Files

| Type | Folder | Description |
| --- | --- | --- |
| Data | `bin\data\` | Main XML data file |
| Backups | `bin\backups\` | Automatic backups |
| Shift reports | `bin\shift-reports\` | End-of-shift reports |
| Executable | `bin\` | Compiled application |

## 13. Testing Requirements

### 13.1 Functional Tests

- Admin login.
- Cashier login.
- Add product.
- Edit product.
- Delete product.
- Scan product.
- Search product.
- Add product quantity.
- Cash sale.
- Card sale.
- Debt sale.
- Return sale.
- Manual discount.
- Employee discount.
- Cash withdrawal.
- Cash deposit.
- Shift closing.
- Receipt printing.
- Shift report printing.
- Shift report saving.
- Automatic backup.

### 13.2 UI Tests

- 1024x700 screen.
- Fullscreen mode.
- Touch-screen use.
- Mouse use.
- Category scrolling.
- Product scrolling.
- Price alignment.
- Quantity alignment.
- Login readability.

### 13.3 Data Tests

- Stock decreases after sale.
- Stock increases after return.
- Sale total is correct.
- Cash difference is correct.
- Bank deposit is correct.
- Debt is separated.
- Shift report is correct.

### 13.4 Error Tests

- Product not found.
- Cash session not open.
- Delete without product selection.
- Printer unavailable.
- Missing data file.

## 14. Deliverables

### Version 1 Deliverables

- C# source code.
- Windows executable.
- README file.
- Requirements specification.
- Demo data.
- Admin interface.
- Cashier interface.
- Receipt printing.
- Shift report printing.
- XML storage.

### Technical Deliverables

- `src/`
- `build.ps1`
- `build.bat`
- `README.md`
- `CAHIER_DES_CHARGES.md`
- `bin/OlyDrugstorePOS.exe` after build.

## 15. Proposed Timeline

| Phase | Estimated Duration | Description |
| --- | --- | --- |
| Phase 1 | 2-3 days | Analysis, requirements, functional mockup |
| Phase 2 | 4-6 days | Authentication, products, stock, store management |
| Phase 3 | 5-7 days | Cashier screen, cart, scanner, payment |
| Phase 4 | 3-4 days | Sessions, closing, reports, printing |
| Phase 5 | 2-3 days | Touch UI, visual polish, performance |
| Phase 6 | 2-3 days | Testing, documentation, delivery |

## 16. Future Improvements

### Short Term

- Excel import/export.
- User management.
- Detailed permissions.
- PDF shift reports.
- Direct ESC/POS receipt printer commands.
- Advanced search.
- Sales history.
- Supplier management.

### Medium Term

- Replace XML with SQLite.
- Advanced admin dashboard.
- Multi-store synchronization.
- Cloud backup.
- Customer orders.
- Delivery management.
- E-commerce web app.

### Long Term

- Connected online store.
- Real-time stock per store.
- Delivery service integration.
- Online payment.
- Central API.
- Sales analytics.
- Stock prediction.
- Multi-device synchronization.

## 17. Acceptance Criteria

The project is acceptable if:

- The application launches on Windows.
- Admin and cashier can log in.
- Admin can manage products.
- Cashier can sell using scanner or touch interface.
- Sales cannot be completed without an open shift.
- Stock updates after sales.
- Returns work correctly.
- Debts are recorded.
- Discounts are applied.
- Shift closing calculates expected cash, difference, and bank deposit.
- Shift report is saved and printed.
- Backups are generated.
- Interface remains usable in fullscreen and smaller screens.
- Main operations are fast and smooth.

## 18. Risks and Limitations

| Risk | Impact | Proposed Solution |
| --- | --- | --- |
| Old computer hardware | Reduced performance | Lightweight local desktop app |
| Different printer models | Printing issues | Windows printer support, future ESC/POS |
| Windows XP compatibility | Runtime limitations | Test target .NET runtime |
| Corrupted XML data | Data loss | Automatic backups |
| Cashier mistakes | Cash differences | Closing report and signatures |
| No internet | No synchronization | Offline-first design |

## 19. Conclusion

Oly Drugstore POS is a local desktop application designed to modernize cashier operations and stock management for a small multi-store drugstore business. Version 1 focuses on speed, reliability, touch-screen usability, local storage, shift closing control, and printable reports.

The system creates a strong foundation for future development, including a centralized backend, online ordering, real-time stock synchronization, delivery integration, and advanced business analytics.
