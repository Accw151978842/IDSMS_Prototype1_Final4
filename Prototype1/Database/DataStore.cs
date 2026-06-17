using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Prototype1.Models;

namespace Prototype1.Database
{
    /// <summary>
    /// All data access via MySQL (ADO.NET).
    /// Each property loads on demand from the DB.
    /// SaveAll() writes in-memory lists back to DB.
    /// </summary>
    public static class DataStore
    {
        public static List<User>                Users           = new List<User>();
        public static List<Staff>               StaffList       = new List<Staff>();
        public static List<Customer>            Customers       = new List<Customer>();
        public static List<Supplier>            Suppliers       = new List<Supplier>();
        public static List<Item>                Items           = new List<Item>();
        public static List<SalesOrder>          SalesOrders     = new List<SalesOrder>();
        public static List<DeliveryNote>        Deliveries      = new List<DeliveryNote>();
        public static List<GoodsReceived>       Receipts        = new List<GoodsReceived>();
        public static List<AfterServiceRequest> ServiceRequests = new List<AfterServiceRequest>();
        public static List<RawMaterialRequest>  RawMaterialRequests = new List<RawMaterialRequest>();
        public static List<Procurement>         Procurements    = new List<Procurement>();
        public static List<Quotation>           Quotations      = new List<Quotation>();
        public static List<AuditLog>            AuditLogs       = new List<AuditLog>();

        // ============================================================
        //  LOAD ALL
        // ============================================================
        public static void LoadAll()
        {
            Users           = LoadUsers();
            StaffList       = LoadStaff();
            Customers       = LoadCustomers();
            Suppliers       = LoadSuppliers();
            Items           = LoadItems();
            SalesOrders     = LoadSalesOrders();
            Deliveries      = LoadDeliveries();
            Receipts        = LoadReceipts();
            ServiceRequests = LoadServiceRequests();
            RawMaterialRequests = LoadRawMaterialRequests();
            Procurements    = LoadProcurements();
            Quotations      = LoadQuotations();
            AuditLogs       = LoadAuditLogs();

            if (Users.Count == 0)
            {
                SeedData.Seed();
                SaveAll();
                LoadAll(); // reload after seed
            }
        }

        // ============================================================
        //  SAVE ALL
        //  DELETE order: children first, parents last (respect FK)
        //  INSERT order: parents first, children last
        //  Wrapped in a transaction so a mid-way failure cannot leave
        //  the database half-populated.
        // ============================================================
        public static void SaveAll()
        {
            // Surface FK problems before touching the DB so the caller can
            // recover the order without losing it. We do NOT silently delete
            // anything from the in-memory lists - user data is preserved.
            ValidateSalesOrderCustomers();

            using (var conn = DbConnection.GetConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    // -- DELETE children first --
                    Exec(conn, tx, "DELETE FROM after_service");
                    Exec(conn, tx, "DELETE FROM delivery_notes");
                    Exec(conn, tx, "DELETE FROM sales_order_lines");
                    Exec(conn, tx, "DELETE FROM sales_orders");
                    // quotation (child) before procurement + rmr (parents)
                    Exec(conn, tx, "DELETE FROM quotation_lines");
                    Exec(conn, tx, "DELETE FROM quotations");
                    // procurement (children) before rmr (parent) - PO may reference RMR
                    Exec(conn, tx, "DELETE FROM procurement_lines");
                    Exec(conn, tx, "DELETE FROM procurements");
                    Exec(conn, tx, "DELETE FROM rmr_lines");
                    Exec(conn, tx, "DELETE FROM raw_material_requests");
                    Exec(conn, tx, "DELETE FROM goods_received");
                    Exec(conn, tx, "DELETE FROM items");
                    Exec(conn, tx, "DELETE FROM customers");
                    Exec(conn, tx, "DELETE FROM suppliers");
                    Exec(conn, tx, "DELETE FROM staff");
                    Exec(conn, tx, "DELETE FROM users");
                    Exec(conn, tx, "DELETE FROM audit_logs");

                    // -- INSERT parents first --
                    InsertUsers(conn, tx);
                    InsertStaff(conn, tx);
                    InsertCustomers(conn, tx);
                    InsertSuppliers(conn, tx);
                    InsertItems(conn, tx);
                    InsertSalesOrders(conn, tx);
                    InsertDeliveries(conn, tx);
                    InsertReceipts(conn, tx);
                    InsertServiceRequests(conn, tx);
                    InsertRawMaterialRequests(conn, tx);
                    InsertProcurements(conn, tx);
                    InsertQuotations(conn, tx);
                    InsertAuditLogs(conn, tx);

                    tx.Commit();
                }
                catch
                {
                    try { tx.Rollback(); } catch { }
                    throw;
                }
            }
        }

        private static void Exec(MySqlConnection conn, MySqlTransaction tx, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn, tx))
                cmd.ExecuteNonQuery();
        }

        // Raise a clear error if any sales order references a customer that
        // does not exist in the in-memory list. Nothing is removed; the
        // caller (e.g. SalesOrderEditForm) catches this and rolls back the
        // unsaved order so the user can fix the customer reference.
        private static void ValidateSalesOrderCustomers()
        {
            if (SalesOrders == null || Customers == null) return;
            var validCustomerIds = new HashSet<string>();
            foreach (var c in Customers)
            {
                if (!string.IsNullOrEmpty(c.CustomerId))
                    validCustomerIds.Add(c.CustomerId);
            }

            foreach (var o in SalesOrders)
            {
                if (string.IsNullOrEmpty(o.CustomerId) || !validCustomerIds.Contains(o.CustomerId))
                {
                    throw new InvalidOperationException(
                        "Sales order " + o.OrderId + " references customer '" + o.CustomerId +
                        "' which is not in the customer list. Please add the customer first, " +
                        "or edit the order to pick a valid customer.");
                }
            }
        }

        // Public guard for callers (e.g. SalesOrderEditForm) so they can
        // surface a friendly message before a save is attempted.
        public static bool CustomerExists(string customerId)
        {
            if (string.IsNullOrEmpty(customerId) || Customers == null) return false;
            for (int i = 0; i < Customers.Count; i++)
            {
                if (Customers[i].CustomerId == customerId) return true;
            }
            return false;
        }

        // ============================================================
        //  NEXT ID HELPER
        // ============================================================
        // Kept for backwards compatibility - delegates to NextId(prefix, existingIds)
        // by reconstructing a degenerate id list. Callers should prefer the
        // overload that takes the actual ID collection.
        public static string NextId(string prefix, int existingCount)
        {
            return prefix + (existingCount + 1).ToString("D5");
        }

        // Compute the next ID by taking max(numeric suffix) + 1 over the
        // supplied IDs. This is collision-safe even when records were
        // removed or imported with gaps. Falls back to 1 when no existing
        // ID matches the expected "<prefix><digits>" pattern.
        public static string NextId(string prefix, IEnumerable<string> existingIds)
        {
            int max = 0;
            if (existingIds != null)
            {
                foreach (var id in existingIds)
                {
                    if (string.IsNullOrEmpty(id) || !id.StartsWith(prefix)) continue;
                    int parsed;
                    if (int.TryParse(id.Substring(prefix.Length), out parsed) && parsed > max)
                        max = parsed;
                }
            }
            return prefix + (max + 1).ToString("D5");
        }

        // ============================================================
        //  USERS
        // ============================================================
        private static List<User> LoadUsers()
        {
            var list = new List<User>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM users", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new User
                    {
                        UserId       = r.GetString("user_id"),
                        Username     = r.GetString("username"),
                        PasswordHash = r.GetString("password_hash"),
                        FullName     = r.GetString("full_name"),
                        Role         = r.GetString("role"),
                        Active       = r.GetBoolean("active")
                    });
            }
            return list;
        }

        private static void InsertUsers(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var u in Users)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO users(user_id,username,password_hash,full_name,role,active) " +
                    "VALUES(@id,@un,@ph,@fn,@role,@active)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",     u.UserId);
                    cmd.Parameters.AddWithValue("@un",     u.Username);
                    cmd.Parameters.AddWithValue("@ph",     u.PasswordHash);
                    cmd.Parameters.AddWithValue("@fn",     u.FullName);
                    cmd.Parameters.AddWithValue("@role",   u.Role);
                    cmd.Parameters.AddWithValue("@active", u.Active);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  STAFF
        // ============================================================
        private static List<Staff> LoadStaff()
        {
            var list = new List<Staff>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM staff", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Staff
                    {
                        StaffId    = r.GetString("staff_id"),
                        FullName   = r.GetString("full_name"),
                        Position   = r.GetString("position"),
                        Department = r.GetString("department"),
                        Phone      = r.IsDBNull(r.GetOrdinal("phone")) ? "" : r.GetString("phone"),
                        Email      = r.IsDBNull(r.GetOrdinal("email")) ? "" : r.GetString("email"),
                        HireDate   = r.GetDateTime("hire_date")
                    });
            }
            return list;
        }

        private static void InsertStaff(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var s in StaffList)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO staff(staff_id,full_name,position,department,phone,email,hire_date) " +
                    "VALUES(@id,@fn,@pos,@dept,@ph,@em,@hd)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",   s.StaffId);
                    cmd.Parameters.AddWithValue("@fn",   s.FullName);
                    cmd.Parameters.AddWithValue("@pos",  s.Position);
                    cmd.Parameters.AddWithValue("@dept", s.Department);
                    cmd.Parameters.AddWithValue("@ph",   s.Phone);
                    cmd.Parameters.AddWithValue("@em",   s.Email);
                    cmd.Parameters.AddWithValue("@hd",   s.HireDate.Date);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  CUSTOMERS
        // ============================================================
        private static List<Customer> LoadCustomers()
        {
            var list = new List<Customer>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM customers", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Customer
                    {
                        CustomerId    = r.GetString("customer_id"),
                        CompanyName   = r.GetString("company_name"),
                        ContactPerson = r.IsDBNull(r.GetOrdinal("contact_person")) ? "" : r.GetString("contact_person"),
                        Phone         = r.IsDBNull(r.GetOrdinal("phone"))          ? "" : r.GetString("phone"),
                        Email         = r.IsDBNull(r.GetOrdinal("email"))          ? "" : r.GetString("email"),
                        Address       = r.IsDBNull(r.GetOrdinal("address"))        ? "" : r.GetString("address"),
                        CustomerType  = r.IsDBNull(r.GetOrdinal("customer_type"))  ? "" : r.GetString("customer_type")
                    });
            }
            return list;
        }

        private static void InsertCustomers(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var c in Customers)
            {
                if (string.IsNullOrEmpty(c.CustomerId))
                {
                    throw new InvalidOperationException(
                        "A customer record has no CustomerId; please reopen the Customer form and save it again.");
                }
                using (var cmd = new MySqlCommand(
                    "INSERT INTO customers(customer_id,company_name,contact_person,phone,email,address,customer_type) " +
                    "VALUES(@id,@cn,@cp,@ph,@em,@ad,@ct)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", c.CustomerId);
                    cmd.Parameters.AddWithValue("@cn", c.CompanyName);
                    cmd.Parameters.AddWithValue("@cp", c.ContactPerson);
                    cmd.Parameters.AddWithValue("@ph", c.Phone);
                    cmd.Parameters.AddWithValue("@em", c.Email);
                    cmd.Parameters.AddWithValue("@ad", c.Address);
                    cmd.Parameters.AddWithValue("@ct", c.CustomerType);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  SUPPLIERS
        // ============================================================
        private static List<Supplier> LoadSuppliers()
        {
            var list = new List<Supplier>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM suppliers", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Supplier
                    {
                        SupplierId    = r.GetString("supplier_id"),
                        CompanyName   = r.GetString("company_name"),
                        ContactPerson = r.IsDBNull(r.GetOrdinal("contact_person")) ? "" : r.GetString("contact_person"),
                        Phone         = r.IsDBNull(r.GetOrdinal("phone"))          ? "" : r.GetString("phone"),
                        Email         = r.IsDBNull(r.GetOrdinal("email"))          ? "" : r.GetString("email"),
                        Address       = r.IsDBNull(r.GetOrdinal("address"))        ? "" : r.GetString("address"),
                        PaymentTerms  = r.IsDBNull(r.GetOrdinal("payment_terms"))  ? "" : r.GetString("payment_terms")
                    });
            }
            return list;
        }

        private static void InsertSuppliers(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var s in Suppliers)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO suppliers(supplier_id,company_name,contact_person,phone,email,address,payment_terms) " +
                    "VALUES(@id,@cn,@cp,@ph,@em,@ad,@pt)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", s.SupplierId);
                    cmd.Parameters.AddWithValue("@cn", s.CompanyName);
                    cmd.Parameters.AddWithValue("@cp", s.ContactPerson);
                    cmd.Parameters.AddWithValue("@ph", s.Phone);
                    cmd.Parameters.AddWithValue("@em", s.Email);
                    cmd.Parameters.AddWithValue("@ad", s.Address);
                    cmd.Parameters.AddWithValue("@pt", s.PaymentTerms);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  ITEMS
        // ============================================================
        private static List<Item> LoadItems()
        {
            var list = new List<Item>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM items", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new Item
                    {
                        ItemId       = r.GetString("item_id"),
                        ItemName     = r.GetString("item_name"),
                        Category     = r.IsDBNull(r.GetOrdinal("category"))    ? "" : r.GetString("category"),
                        Unit         = r.IsDBNull(r.GetOrdinal("unit"))        ? "" : r.GetString("unit"),
                        UnitPrice    = r.GetDecimal("unit_price"),
                        StockQty     = r.GetInt32("stock_qty"),
                        ReorderLevel = r.GetInt32("reorder_level"),
                        SupplierId   = r.IsDBNull(r.GetOrdinal("supplier_id")) ? "" : r.GetString("supplier_id")
                    });
            }
            return list;
        }

        private static void InsertItems(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var i in Items)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO items(item_id,item_name,category,unit,unit_price,stock_qty,reorder_level,supplier_id) " +
                    "VALUES(@id,@nm,@cat,@unit,@price,@stock,@reorder,@sup)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",      i.ItemId);
                    cmd.Parameters.AddWithValue("@nm",      i.ItemName);
                    cmd.Parameters.AddWithValue("@cat",     i.Category);
                    cmd.Parameters.AddWithValue("@unit",    i.Unit);
                    cmd.Parameters.AddWithValue("@price",   i.UnitPrice);
                    cmd.Parameters.AddWithValue("@stock",   i.StockQty);
                    cmd.Parameters.AddWithValue("@reorder", i.ReorderLevel);
                    cmd.Parameters.AddWithValue("@sup",     string.IsNullOrEmpty(i.SupplierId) ? (object)DBNull.Value : i.SupplierId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  SALES ORDERS  (header + lines)
        // ============================================================
        private static List<SalesOrder> LoadSalesOrders()
        {
            var list = new List<SalesOrder>();
            using (var conn = DbConnection.GetConnection())
            {
                using (var cmd = new MySqlCommand("SELECT * FROM sales_orders ORDER BY order_id", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(new SalesOrder
                        {
                            OrderId      = r.GetString("order_id"),
                            OrderDate    = r.GetDateTime("order_date"),
                            CustomerId   = r.GetString("customer_id"),
                            Status       = r.GetString("status"),
                            RequiredDate = r.GetDateTime("required_date"),
                            Remarks      = r.IsDBNull(r.GetOrdinal("remarks"))    ? "" : r.GetString("remarks"),
                            CreatedBy    = r.IsDBNull(r.GetOrdinal("created_by")) ? "" : r.GetString("created_by")
                        });
                }
                foreach (var order in list)
                {
                    using (var cmd2 = new MySqlCommand(
                        "SELECT * FROM sales_order_lines WHERE order_id=@oid", conn))
                    {
                        cmd2.Parameters.AddWithValue("@oid", order.OrderId);
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            while (r2.Read())
                                order.Lines.Add(new SalesOrderLine
                                {
                                    ItemId    = r2.GetString("item_id"),
                                    ItemName  = r2.GetString("item_name"),
                                    Quantity  = r2.GetInt32("quantity"),
                                    UnitPrice = r2.GetDecimal("unit_price")
                                });
                        }
                    }
                }
            }
            return list;
        }

        private static void InsertSalesOrders(MySqlConnection conn, MySqlTransaction tx)
        {
            // Build a lookup of customers already queued for insert so we
            // can short-circuit before MySQL raises fk_so_customer.
            var validCustomerIds = new HashSet<string>();
            foreach (var c in Customers)
            {
                if (!string.IsNullOrEmpty(c.CustomerId))
                    validCustomerIds.Add(c.CustomerId);
            }

            foreach (var o in SalesOrders)
            {
                if (!validCustomerIds.Contains(o.CustomerId))
                {
                    throw new InvalidOperationException(
                        "Sales order " + o.OrderId + " references customer '" + o.CustomerId +
                        "' which does not exist. Please create the customer first or pick a valid one.");
                }
                using (var cmd = new MySqlCommand(
                    "INSERT INTO sales_orders(order_id,order_date,customer_id,status,required_date,remarks,created_by) " +
                    "VALUES(@id,@od,@cid,@st,@rd,@rem,@cb)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  o.OrderId);
                    cmd.Parameters.AddWithValue("@od",  o.OrderDate.Date);
                    cmd.Parameters.AddWithValue("@cid", o.CustomerId);
                    cmd.Parameters.AddWithValue("@st",  o.Status);
                    cmd.Parameters.AddWithValue("@rd",  o.RequiredDate.Date);
                    cmd.Parameters.AddWithValue("@rem", o.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@cb",  o.CreatedBy ?? "");
                    cmd.ExecuteNonQuery();
                }
                foreach (var ln in o.Lines)
                {
                    using (var cmd2 = new MySqlCommand(
                        "INSERT INTO sales_order_lines(order_id,item_id,item_name,quantity,unit_price) VALUES(@oid,@iid,@inm,@qty,@up)", conn, tx))
                    {
                        cmd2.Parameters.AddWithValue("@oid", o.OrderId);
                        cmd2.Parameters.AddWithValue("@iid", ln.ItemId);
                        cmd2.Parameters.AddWithValue("@inm", ln.ItemName);
                        cmd2.Parameters.AddWithValue("@qty", ln.Quantity);
                        cmd2.Parameters.AddWithValue("@up",  ln.UnitPrice);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
        }

        // ============================================================
        //  DELIVERY NOTES
        // ============================================================
        private static List<DeliveryNote> LoadDeliveries()
        {
            var list = new List<DeliveryNote>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM delivery_notes", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new DeliveryNote
                    {
                        DeliveryId        = r.GetString("delivery_id"),
                        OrderId           = r.GetString("order_id"),
                        DeliveryDate      = r.GetDateTime("delivery_date"),
                        DriverName        = r.IsDBNull(r.GetOrdinal("driver_name"))        ? "" : r.GetString("driver_name"),
                        VehicleNo         = r.IsDBNull(r.GetOrdinal("vehicle_no"))         ? "" : r.GetString("vehicle_no"),
                        Status            = r.IsDBNull(r.GetOrdinal("status"))             ? "" : r.GetString("status"),
                        ReplySlipStatus   = r.IsDBNull(r.GetOrdinal("reply_slip_status"))  ? "" : r.GetString("reply_slip_status"),
                        CustomerSignature = r.IsDBNull(r.GetOrdinal("customer_signature")) ? "" : r.GetString("customer_signature"),
                        Remarks           = r.IsDBNull(r.GetOrdinal("remarks"))            ? "" : r.GetString("remarks")
                    });
            }
            return list;
        }

        private static void InsertDeliveries(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var d in Deliveries)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO delivery_notes(delivery_id,order_id,delivery_date,driver_name,vehicle_no,status,reply_slip_status,customer_signature,remarks) " +
                    "VALUES(@id,@oid,@dd,@drv,@veh,@st,@rss,@sig,@rem)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  d.DeliveryId);
                    cmd.Parameters.AddWithValue("@oid", d.OrderId);
                    cmd.Parameters.AddWithValue("@dd",  d.DeliveryDate.Date);
                    cmd.Parameters.AddWithValue("@drv", d.DriverName);
                    cmd.Parameters.AddWithValue("@veh", d.VehicleNo);
                    cmd.Parameters.AddWithValue("@st",  d.Status);
                    cmd.Parameters.AddWithValue("@rss", d.ReplySlipStatus);
                    cmd.Parameters.AddWithValue("@sig", d.CustomerSignature);
                    cmd.Parameters.AddWithValue("@rem", d.Remarks);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  GOODS RECEIVED
        // ============================================================
        private static List<GoodsReceived> LoadReceipts()
        {
            var list = new List<GoodsReceived>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM goods_received", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new GoodsReceived
                    {
                        ReceiptId       = r.GetString("receipt_id"),
                        ReceiveDate     = r.GetDateTime("receive_date"),
                        SupplierId      = r.IsDBNull(r.GetOrdinal("supplier_id"))       ? "" : r.GetString("supplier_id"),
                        ItemId          = r.IsDBNull(r.GetOrdinal("item_id"))            ? "" : r.GetString("item_id"),
                        Quantity        = r.GetInt32("quantity"),
                        PurchaseOrderNo = r.IsDBNull(r.GetOrdinal("purchase_order_no")) ? "" : r.GetString("purchase_order_no"),
                        ReceivedBy      = r.IsDBNull(r.GetOrdinal("received_by"))       ? "" : r.GetString("received_by"),
                        Condition       = r.IsDBNull(r.GetOrdinal("condition"))         ? "" : r.GetString("condition"),
                        Remarks         = r.IsDBNull(r.GetOrdinal("remarks"))           ? "" : r.GetString("remarks")
                    });
            }
            return list;
        }

        private static void InsertReceipts(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var g in Receipts)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO goods_received(receipt_id,receive_date,supplier_id,item_id,quantity,purchase_order_no,received_by,`condition`,remarks) " +
                    "VALUES(@id,@rd,@sup,@itm,@qty,@po,@rb,@cond,@rem)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",   g.ReceiptId);
                    cmd.Parameters.AddWithValue("@rd",   g.ReceiveDate.Date);
                    cmd.Parameters.AddWithValue("@sup",  string.IsNullOrEmpty(g.SupplierId) ? (object)DBNull.Value : g.SupplierId);
                    cmd.Parameters.AddWithValue("@itm",  string.IsNullOrEmpty(g.ItemId)     ? (object)DBNull.Value : g.ItemId);
                    cmd.Parameters.AddWithValue("@qty",  g.Quantity);
                    cmd.Parameters.AddWithValue("@po",   g.PurchaseOrderNo);
                    cmd.Parameters.AddWithValue("@rb",   g.ReceivedBy);
                    cmd.Parameters.AddWithValue("@cond", g.Condition);
                    cmd.Parameters.AddWithValue("@rem",  g.Remarks);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  AFTER SERVICE
        // ============================================================
        private static List<AfterServiceRequest> LoadServiceRequests()
        {
            var list = new List<AfterServiceRequest>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM after_service", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new AfterServiceRequest
                    {
                        RequestId   = r.GetString("request_id"),
                        RequestDate = r.GetDateTime("request_date"),
                        OrderId     = r.IsDBNull(r.GetOrdinal("order_id"))     ? "" : r.GetString("order_id"),
                        CustomerId  = r.IsDBNull(r.GetOrdinal("customer_id"))  ? "" : r.GetString("customer_id"),
                        RequestType = r.IsDBNull(r.GetOrdinal("request_type")) ? "" : r.GetString("request_type"),
                        ItemId      = r.IsDBNull(r.GetOrdinal("item_id"))      ? "" : r.GetString("item_id"),
                        Quantity    = r.GetInt32("quantity"),
                        Reason      = r.IsDBNull(r.GetOrdinal("reason"))       ? "" : r.GetString("reason"),
                        Status      = r.IsDBNull(r.GetOrdinal("status"))       ? "" : r.GetString("status"),
                        HandledBy   = r.IsDBNull(r.GetOrdinal("handled_by"))   ? "" : r.GetString("handled_by"),
                        Resolution  = r.IsDBNull(r.GetOrdinal("resolution"))   ? "" : r.GetString("resolution")
                    });
            }
            return list;
        }

        private static void InsertServiceRequests(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var s in ServiceRequests)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO after_service(request_id,request_date,order_id,customer_id,request_type,item_id,quantity,reason,status,handled_by,resolution) " +
                    "VALUES(@id,@rd,@oid,@cid,@rt,@itm,@qty,@rsn,@st,@hb,@res)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  s.RequestId);
                    cmd.Parameters.AddWithValue("@rd",  s.RequestDate.Date);
                    cmd.Parameters.AddWithValue("@oid", string.IsNullOrEmpty(s.OrderId)    ? (object)DBNull.Value : s.OrderId);
                    cmd.Parameters.AddWithValue("@cid", string.IsNullOrEmpty(s.CustomerId) ? (object)DBNull.Value : s.CustomerId);
                    cmd.Parameters.AddWithValue("@rt",  s.RequestType);
                    cmd.Parameters.AddWithValue("@itm", string.IsNullOrEmpty(s.ItemId)     ? (object)DBNull.Value : s.ItemId);
                    cmd.Parameters.AddWithValue("@qty", s.Quantity);
                    cmd.Parameters.AddWithValue("@rsn", s.Reason);
                    cmd.Parameters.AddWithValue("@st",  s.Status);
                    cmd.Parameters.AddWithValue("@hb",  s.HandledBy);
                    cmd.Parameters.AddWithValue("@res", s.Resolution);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ============================================================
        //  RAW MATERIAL REQUESTS  (header + lines)
        // ============================================================
        private static List<RawMaterialRequest> LoadRawMaterialRequests()
        {
            var list = new List<RawMaterialRequest>();
            using (var conn = DbConnection.GetConnection())
            {
                // Header
                try
                {
                    using (var cmd = new MySqlCommand("SELECT * FROM raw_material_requests ORDER BY rmr_id", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            list.Add(new RawMaterialRequest
                            {
                                RmrId       = r.GetString("rmr_id"),
                                RequestDate = r.GetDateTime("request_date"),
                                RequestedBy = r.IsDBNull(r.GetOrdinal("requested_by")) ? "" : r.GetString("requested_by"),
                                Department  = r.IsDBNull(r.GetOrdinal("department"))   ? "" : r.GetString("department"),
                                Status      = r.IsDBNull(r.GetOrdinal("status"))       ? "" : r.GetString("status"),
                                Notes       = r.IsDBNull(r.GetOrdinal("notes"))        ? "" : r.GetString("notes")
                            });
                    }
                }
                catch (MySqlException) { return list; } // table not yet created

                foreach (var rmr in list)
                {
                    using (var cmd2 = new MySqlCommand("SELECT * FROM rmr_lines WHERE rmr_id=@id", conn))
                    {
                        cmd2.Parameters.AddWithValue("@id", rmr.RmrId);
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            while (r2.Read())
                                rmr.Lines.Add(new RmrLine
                                {
                                    ItemId    = r2.GetString("item_id"),
                                    ItemName  = r2.IsDBNull(r2.GetOrdinal("item_name")) ? "" : r2.GetString("item_name"),
                                    QtyNeeded = r2.GetInt32("qty_needed"),
                                    Notes     = r2.IsDBNull(r2.GetOrdinal("notes")) ? "" : r2.GetString("notes")
                                });
                        }
                    }
                }
            }
            return list;
        }

        private static void InsertRawMaterialRequests(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var rmr in RawMaterialRequests)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO raw_material_requests(rmr_id,request_date,requested_by,department,status,notes) " +
                    "VALUES(@id,@dt,@by,@dept,@st,@nt)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",   rmr.RmrId);
                    cmd.Parameters.AddWithValue("@dt",   rmr.RequestDate.Date);
                    cmd.Parameters.AddWithValue("@by",   rmr.RequestedBy ?? "");
                    cmd.Parameters.AddWithValue("@dept", rmr.Department  ?? "");
                    cmd.Parameters.AddWithValue("@st",   rmr.Status      ?? "Pending");
                    cmd.Parameters.AddWithValue("@nt",   rmr.Notes       ?? "");
                    cmd.ExecuteNonQuery();
                }
                foreach (var ln in rmr.Lines)
                {
                    using (var cmd2 = new MySqlCommand(
                        "INSERT INTO rmr_lines(rmr_id,item_id,item_name,qty_needed,notes) VALUES(@rid,@iid,@inm,@qty,@nt)", conn, tx))
                    {
                        cmd2.Parameters.AddWithValue("@rid", rmr.RmrId);
                        cmd2.Parameters.AddWithValue("@iid", ln.ItemId);
                        cmd2.Parameters.AddWithValue("@inm", ln.ItemName ?? "");
                        cmd2.Parameters.AddWithValue("@qty", ln.QtyNeeded);
                        cmd2.Parameters.AddWithValue("@nt",  ln.Notes ?? "");
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
        }

        // ============================================================
        //  PROCUREMENT (Purchase Orders)  (header + lines)
        // ============================================================
        private static List<Procurement> LoadProcurements()
        {
            var list = new List<Procurement>();
            using (var conn = DbConnection.GetConnection())
            {
                try
                {
                    using (var cmd = new MySqlCommand("SELECT * FROM procurements ORDER BY po_id", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            list.Add(new Procurement
                            {
                                PoId             = r.GetString("po_id"),
                                SupplierId       = r.IsDBNull(r.GetOrdinal("supplier_id"))       ? "" : r.GetString("supplier_id"),
                                OrderDate        = r.GetDateTime("order_date"),
                                ExpectedDelivery = r.GetDateTime("expected_delivery"),
                                Status           = r.IsDBNull(r.GetOrdinal("status"))           ? "" : r.GetString("status"),
                                LinkedRmrId      = r.IsDBNull(r.GetOrdinal("linked_rmr_id"))    ? "" : r.GetString("linked_rmr_id"),
                                CreatedBy        = r.IsDBNull(r.GetOrdinal("created_by"))       ? "" : r.GetString("created_by"),
                                Remarks          = r.IsDBNull(r.GetOrdinal("remarks"))          ? "" : r.GetString("remarks")
                            });
                    }
                }
                catch (MySqlException) { return list; }

                foreach (var po in list)
                {
                    using (var cmd2 = new MySqlCommand("SELECT * FROM procurement_lines WHERE po_id=@id", conn))
                    {
                        cmd2.Parameters.AddWithValue("@id", po.PoId);
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            while (r2.Read())
                                po.Lines.Add(new ProcurementLine
                                {
                                    ItemId    = r2.GetString("item_id"),
                                    ItemName  = r2.IsDBNull(r2.GetOrdinal("item_name")) ? "" : r2.GetString("item_name"),
                                    Quantity  = r2.GetInt32("quantity"),
                                    UnitPrice = r2.GetDecimal("unit_price")
                                });
                        }
                    }
                }
            }
            return list;
        }

        private static void InsertProcurements(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var po in Procurements)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO procurements(po_id,supplier_id,order_date,expected_delivery,status,linked_rmr_id,created_by,remarks) " +
                    "VALUES(@id,@sup,@od,@ed,@st,@rmr,@cb,@rem)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  po.PoId);
                    cmd.Parameters.AddWithValue("@sup", string.IsNullOrEmpty(po.SupplierId) ? (object)DBNull.Value : po.SupplierId);
                    cmd.Parameters.AddWithValue("@od",  po.OrderDate.Date);
                    cmd.Parameters.AddWithValue("@ed",  po.ExpectedDelivery.Date);
                    cmd.Parameters.AddWithValue("@st",  po.Status ?? "Draft");
                    cmd.Parameters.AddWithValue("@rmr", string.IsNullOrEmpty(po.LinkedRmrId) ? (object)DBNull.Value : po.LinkedRmrId);
                    cmd.Parameters.AddWithValue("@cb",  po.CreatedBy ?? "");
                    cmd.Parameters.AddWithValue("@rem", po.Remarks ?? "");
                    cmd.ExecuteNonQuery();
                }
                foreach (var ln in po.Lines)
                {
                    using (var cmd2 = new MySqlCommand(
                        "INSERT INTO procurement_lines(po_id,item_id,item_name,quantity,unit_price) VALUES(@pid,@iid,@inm,@qty,@up)", conn, tx))
                    {
                        cmd2.Parameters.AddWithValue("@pid", po.PoId);
                        cmd2.Parameters.AddWithValue("@iid", ln.ItemId);
                        cmd2.Parameters.AddWithValue("@inm", ln.ItemName ?? "");
                        cmd2.Parameters.AddWithValue("@qty", ln.Quantity);
                        cmd2.Parameters.AddWithValue("@up",  ln.UnitPrice);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
        }

        // ============================================================
        //  QUOTATIONS
        // ============================================================
        private static List<Quotation> LoadQuotations()
        {
            var list = new List<Quotation>();
            using (var conn = DbConnection.GetConnection())
            {
                try
                {
                    using (var cmd = new MySqlCommand("SELECT * FROM quotations ORDER BY quotation_id", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                            list.Add(new Quotation
                            {
                                QuotationId   = r.GetString("quotation_id"),
                                QuotationNo   = r.IsDBNull(r.GetOrdinal("quotation_no"))   ? "" : r.GetString("quotation_no"),
                                RmrId         = r.IsDBNull(r.GetOrdinal("rmr_id"))         ? "" : r.GetString("rmr_id"),
                                SupplierId    = r.IsDBNull(r.GetOrdinal("supplier_id"))    ? "" : r.GetString("supplier_id"),
                                QuoteDate     = r.GetDateTime("quote_date"),
                                ValidUntil    = r.IsDBNull(r.GetOrdinal("valid_until"))    ? r.GetDateTime("quote_date") : r.GetDateTime("valid_until"),
                                LeadTimeDays  = r.IsDBNull(r.GetOrdinal("lead_time_days")) ? 0  : r.GetInt32("lead_time_days"),
                                PaymentTerms  = r.IsDBNull(r.GetOrdinal("payment_terms"))  ? "" : r.GetString("payment_terms"),
                                Status        = r.IsDBNull(r.GetOrdinal("status"))         ? "" : r.GetString("status"),
                                ConvertedPoId = r.IsDBNull(r.GetOrdinal("converted_po_id"))? "" : r.GetString("converted_po_id"),
                                CreatedBy     = r.IsDBNull(r.GetOrdinal("created_by"))     ? "" : r.GetString("created_by"),
                                Remarks       = r.IsDBNull(r.GetOrdinal("remarks"))        ? "" : r.GetString("remarks")
                            });
                    }
                }
                catch (MySqlException) { return list; }

                foreach (var q in list)
                {
                    using (var cmd2 = new MySqlCommand("SELECT * FROM quotation_lines WHERE quotation_id=@id", conn))
                    {
                        cmd2.Parameters.AddWithValue("@id", q.QuotationId);
                        using (var r2 = cmd2.ExecuteReader())
                        {
                            while (r2.Read())
                                q.Lines.Add(new QuotationLine
                                {
                                    ItemId    = r2.GetString("item_id"),
                                    ItemName  = r2.IsDBNull(r2.GetOrdinal("item_name")) ? "" : r2.GetString("item_name"),
                                    Quantity  = r2.GetInt32("quantity"),
                                    UnitPrice = r2.GetDecimal("unit_price")
                                });
                        }
                    }
                }
            }
            return list;
        }

        private static void InsertQuotations(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var q in Quotations)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO quotations(quotation_id,quotation_no,rmr_id,supplier_id,quote_date,valid_until," +
                    "lead_time_days,payment_terms,status,converted_po_id,created_by,remarks) " +
                    "VALUES(@id,@qn,@rmr,@sup,@qd,@vu,@lt,@pt,@st,@cpo,@cb,@rem)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  q.QuotationId);
                    cmd.Parameters.AddWithValue("@qn",  q.QuotationNo ?? "");
                    cmd.Parameters.AddWithValue("@rmr", string.IsNullOrEmpty(q.RmrId) ? (object)DBNull.Value : q.RmrId);
                    cmd.Parameters.AddWithValue("@sup", string.IsNullOrEmpty(q.SupplierId) ? (object)DBNull.Value : q.SupplierId);
                    cmd.Parameters.AddWithValue("@qd",  q.QuoteDate.Date);
                    cmd.Parameters.AddWithValue("@vu",  q.ValidUntil.Date);
                    cmd.Parameters.AddWithValue("@lt",  q.LeadTimeDays);
                    cmd.Parameters.AddWithValue("@pt",  q.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@st",  q.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("@cpo", string.IsNullOrEmpty(q.ConvertedPoId) ? (object)DBNull.Value : q.ConvertedPoId);
                    cmd.Parameters.AddWithValue("@cb",  q.CreatedBy ?? "");
                    cmd.Parameters.AddWithValue("@rem", q.Remarks ?? "");
                    cmd.ExecuteNonQuery();
                }
                foreach (var ln in q.Lines)
                {
                    using (var cmd2 = new MySqlCommand(
                        "INSERT INTO quotation_lines(quotation_id,item_id,item_name,quantity,unit_price) VALUES(@qid,@iid,@inm,@qty,@up)", conn, tx))
                    {
                        cmd2.Parameters.AddWithValue("@qid", q.QuotationId);
                        cmd2.Parameters.AddWithValue("@iid", ln.ItemId);
                        cmd2.Parameters.AddWithValue("@inm", ln.ItemName ?? "");
                        cmd2.Parameters.AddWithValue("@qty", ln.Quantity);
                        cmd2.Parameters.AddWithValue("@up",  ln.UnitPrice);
                        cmd2.ExecuteNonQuery();
                    }
                }
            }
        }

        // ============================================================
        //  AUDIT LOGS
        // ============================================================
        private static List<AuditLog> LoadAuditLogs()
        {
            var list = new List<AuditLog>();
            using (var conn = DbConnection.GetConnection())
            using (var cmd = new MySqlCommand("SELECT * FROM audit_logs ORDER BY timestamp DESC", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    list.Add(new AuditLog
                    {
                        LogId     = r.GetString("log_id"),
                        Timestamp = r.GetDateTime("timestamp"),
                        Username  = r.IsDBNull(r.GetOrdinal("username")) ? "" : r.GetString("username"),
                        Action    = r.IsDBNull(r.GetOrdinal("action"))   ? "" : r.GetString("action"),
                        Detail    = r.IsDBNull(r.GetOrdinal("detail"))   ? "" : r.GetString("detail")
                    });
            }
            return list;
        }

        private static void InsertAuditLogs(MySqlConnection conn, MySqlTransaction tx)
        {
            foreach (var a in AuditLogs)
            {
                using (var cmd = new MySqlCommand(
                    "INSERT INTO audit_logs(log_id,timestamp,username,action,detail) " +
                    "VALUES(@id,@ts,@un,@act,@det)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id",  a.LogId);
                    cmd.Parameters.AddWithValue("@ts",  a.Timestamp);
                    cmd.Parameters.AddWithValue("@un",  a.Username);
                    cmd.Parameters.AddWithValue("@act", a.Action);
                    cmd.Parameters.AddWithValue("@det", a.Detail);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
