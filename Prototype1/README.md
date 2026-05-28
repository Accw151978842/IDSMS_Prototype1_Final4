# IDSMS Prototype I

**Project:** Integrated Delivery Services Management System (IDSMS)
**Client (Case Study):** Premium Living Furniture Co. Ltd.
**Course:** ITP4915M System Development Project (Prototype I)

This Visual Studio C# WinForms application is the Prototype I submission for
Group 17. It demonstrates the basic-stage requirements of the project: a menu
program with login, sales order processing, logistics handling (delivery
notes and reply slips), inward goods recording, after-service management,
master data maintenance, and simple role-based system security.

## How to open and run

1. Install Visual Studio 2019 / 2022 (Community Edition is sufficient) with the
   ".NET desktop development" workload installed.
2. Open `Prototype1.slnx` (or open the project file `Prototype1/Prototype1.csproj`
   directly).
3. If Visual Studio shows the new solution-file format dialog, allow it to
   migrate or open the `.csproj` directly.
4. Press **F5** (Start Debugging) or **Ctrl+F5** (Start Without Debugging).
5. The application launches the Login window. Sign in with one of the demo
   accounts below.

The first run creates an `AppData` folder beside the executable that holds
the JSON data files. Restart the application to reload from the JSON files.

## Demo accounts

| Username   | Password   | Role                  |
| ---------- | ---------- | --------------------- |
| admin      | admin123   | Administrator (all)   |
| sales      | sales123   | Sales                 |
| logistics  | log123     | Logistics             |
| warehouse  | ware123    | Warehouse             |
| service    | svc123     | After-Service         |

## Modules implemented

- **Login / Main Menu** with role-based menu and access checks.
- **Dashboard** tiles showing open orders, pending deliveries, low-stock items.
- **Order Processing** – list, create, edit, view, and cancel sales orders.
- **Logistics** – generate delivery notes from sales orders, record reply
  slips with customer signature, preview a printable delivery note.
- **Inventory Control** – record inward goods receipt, automatic stock
  update, item master with low-stock highlighting, stock adjustment.
- **After-Service** – return, replacement, refund, and repair requests;
  closing return/replacement requests increases stock.
- **Master Data** – customers, suppliers, staff, items.
- **System Security** – user account maintenance, password change,
  password reset, audit log viewer, and audit logging on key actions.

## Database

The prototype persists data to JSON files in `AppData/` (see `Services/DataStore.cs`).
A reference SQL Server schema and a sample seed script are provided in the
`Database/` folder (`schema.sql`, `sample_data.sql`) for the next iteration
when the team migrates to a real database back-end.

## Project layout

```
Prototype1/
  Program.cs                  - Application entry point
  Models/Entities.cs          - Domain entity classes
  Services/DataStore.cs       - JSON-backed in-memory repository
  Services/SeedData.cs        - Initial demo data
  Services/SecurityService.cs - Login, password hashing, audit, RBAC
  Forms/                      - All Windows Forms screens
  Database/                   - SQL schema and sample data scripts
```

## Limitations / next steps

- Data is currently stored in JSON files for the prototype. The team will
  migrate the persistence layer to SQL Server LocalDB / Express using the
  provided `schema.sql` in Prototype II.
- Reports/printing currently render as plain text preview windows.
- Permissions are coarse-grained (role check at menu open); finer field-level
  controls will be added in Prototype II.
- The schema does not yet include quotations, invoices, or purchase orders
  beyond a `PO No.` field on the goods-received record; these will be added
  as the design extends to cover quotation and invoicing modules.
