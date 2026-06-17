# IDSMS Prototype I

**Project:** Integrated Delivery Services Management System (IDSMS)
**Client (Case Study):** Premium Living Furniture Co. Ltd.
**Course:** ITP4915M System Development Project (Prototype I)

This Visual Studio C# WinForms application is the Prototype I submission for
Group 17. It demonstrates the basic-stage requirements of the project: a menu
program with login, sales order processing, logistics handling (delivery
notes and reply slips), inward goods recording, after-service management,
master data maintenance, and simple role-based system security.

## Prerequisites

- Visual Studio 2019 / 2022 (Community Edition is sufficient) with the
  ".NET desktop development" workload installed.
- A running **MySQL** server (e.g. via XAMPP / MySQL 8.x). The app connects
  to it with the MySql.Data ADO.NET connector.

## Database setup (do this BEFORE first run)

1. Start MySQL (e.g. start the MySQL module in the XAMPP Control Panel).
2. Open **phpMyAdmin** (or the MySQL CLI) and run the script
   `Database/setup_idsms2.sql`. It creates the database **`idsms2`** and all
   **17 tables** (11 base + 4 Final4 production/procurement + 2 quotation
   tables).
   - phpMyAdmin: top tab **SQL** → paste the whole file → **Go**.
   - CLI: `mysql -u root < Database/setup_idsms2.sql`
3. Confirm the connection settings in `Database/DbConnection.cs` match your
   MySQL setup (default: `Server=localhost; Port=3306; Database=idsms2;
   Uid=root; Pwd=` — empty password). Change them if your MySQL uses a
   different user/password.

## How to open and run

1. Open `Prototype1.slnx` (or open `Prototype1/Prototype1.csproj` directly).
2. If Visual Studio shows the new solution-file format dialog, allow it to
   migrate or open the `.csproj` directly.
3. Restore NuGet packages if prompted (MySql.Data and its dependencies).
4. Press **F5** (Start Debugging) or **Ctrl+F5** (Start Without Debugging).
5. The application launches the Login window. Sign in with one of the demo
   accounts below.

On first run, if the `users` table is empty, `DataStore.LoadAll()` seeds the
base demo data (users, staff, customers, suppliers, items, sample orders,
RMRs and POs) directly into MySQL and reloads it. All data is persisted in
the `idsms2` database — not in local files.

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

The application persists all data to a **MySQL** database named `idsms2`
via ADO.NET (see `Database/DataStore.cs` and `Database/DbConnection.cs`).
`DataStore.LoadAll()` reads every table into in-memory lists on start-up,
and `DataStore.SaveAll()` writes them back inside a single transaction.

Scripts in the `Database/` folder:

- **`setup_idsms2.sql`** — the authoritative script. Creates the `idsms2`
  database and all 17 tables. **Run this one.**
- `migration_final4.sql` — legacy: adds the Final4 + quotation tables onto an
  existing `idsms` database. Kept for reference only.
- `schema.sql` / `schema_mysql.sql` — legacy reference schemas (SQL Server
  style / `idsms` database). Not used by the current build.
- `extended_sample_data.sql`, `sample_data.sql`, `reset_demo_users.sql` —
  optional sample / utility scripts.

## Project layout

```
Prototype1/
  Program.cs                   - Application entry point
  Models/Entities.cs           - Domain entity classes
  Database/DbConnection.cs     - MySQL connection settings + factory
  Database/DataStore.cs        - MySQL (ADO.NET) repository: LoadAll / SaveAll
  Database/SeedData.cs         - Initial demo data (seeded on first run)
  Database/SecurityService.cs  - Login, password hashing, audit, RBAC, session
  Database/*.sql               - Database creation / sample-data scripts
  Forms/                       - All Windows Forms screens
```

## Limitations / next steps

- Persistence uses a "delete-all then re-insert" strategy in
  `DataStore.SaveAll()`. This is simple and transaction-safe for the
  prototype, but should move to per-record INSERT/UPDATE/DELETE for scale
  and multi-user safety.
- Reports/printing currently render as plain text preview windows.
- Permissions are coarse-grained (role check at menu open); finer field-level
  controls will be added later.
- Stock is increased on goods-received and on return/replacement closure, but
  is not yet decremented when a sales order is shipped/delivered.
