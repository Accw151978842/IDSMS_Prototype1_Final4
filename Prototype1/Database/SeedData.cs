using System;
using System.Collections.Generic;
using Prototype1.Models;

namespace Prototype1.Database
{
    internal static class SeedData
    {
        public static void Seed()
        {
            DataStore.Users.AddRange(new[]
            {
                new User { UserId = "U00001", Username = "admin", PasswordHash = SecurityService.Hash("admin123"), FullName = "System Administrator", Role = "Administrator", Active = true },
                new User { UserId = "U00002", Username = "sales", PasswordHash = SecurityService.Hash("sales123"), FullName = "Sales Officer", Role = "Sales", Active = true },
                new User { UserId = "U00003", Username = "logistics", PasswordHash = SecurityService.Hash("log123"), FullName = "Logistics Officer", Role = "Logistics", Active = true },
                new User { UserId = "U00004", Username = "warehouse", PasswordHash = SecurityService.Hash("ware123"), FullName = "Warehouse Officer", Role = "Warehouse", Active = true },
                new User { UserId = "U00005", Username = "service", PasswordHash = SecurityService.Hash("svc123"), FullName = "After-Service Officer", Role = "Service", Active = true }
            });

            DataStore.StaffList.AddRange(new[]
            {
                new Staff { StaffId = "S00001", FullName = "Chan Tai Man", Position = "Sales Manager", Department = "Sales", Phone = "98765432", Email = "tmchan@plf.com.hk", HireDate = new DateTime(2020, 3, 1) },
                new Staff { StaffId = "S00002", FullName = "Wong Siu Ling", Position = "Sales Officer", Department = "Sales", Phone = "97654321", Email = "slwong@plf.com.hk", HireDate = new DateTime(2021, 6, 15) },
                new Staff { StaffId = "S00003", FullName = "Lee Ka Ho", Position = "Warehouse Supervisor", Department = "Warehouse", Phone = "96543210", Email = "khlee@plf.com.hk", HireDate = new DateTime(2019, 9, 1) },
                new Staff { StaffId = "S00004", FullName = "Cheung Mei Yee", Position = "Logistics Officer", Department = "Logistics", Phone = "95432109", Email = "mycheung@plf.com.hk", HireDate = new DateTime(2022, 1, 10) }
            });

            DataStore.Customers.AddRange(new[]
            {
                new Customer { CustomerId = "C00001", CompanyName = "Harbour View Hotel", ContactPerson = "Mr. Lam", Phone = "29001234", Email = "purchasing@hvhotel.hk", Address = "12 Harbour Road, Wan Chai, Hong Kong", CustomerType = "Corporate" },
                new Customer { CustomerId = "C00002", CompanyName = "Golden Star Restaurant", ContactPerson = "Ms. Tang", Phone = "29005678", Email = "admin@goldenstar.hk", Address = "55 Nathan Road, Tsim Sha Tsui", CustomerType = "Corporate" },
                new Customer { CustomerId = "C00003", CompanyName = "Ng Family Residence", ContactPerson = "Mrs. Ng", Phone = "92223344", Email = "ngfamily@gmail.com", Address = "Flat 5B, Sunshine Garden, Tai Po", CustomerType = "Retail" },
                new Customer { CustomerId = "C00004", CompanyName = "City Office Tower Ltd.", ContactPerson = "Mr. Chiu", Phone = "29008888", Email = "facilities@cot.com.hk", Address = "168 Queen's Road Central, Central", CustomerType = "Corporate" }
            });

            DataStore.Suppliers.AddRange(new[]
            {
                new Supplier { SupplierId = "SP00001", CompanyName = "Dongguan Woodcraft Co.", ContactPerson = "Mr. Zhang", Phone = "+86-769-23456789", Email = "sales@dgwood.cn", Address = "Industrial Zone, Dongguan, Guangdong", PaymentTerms = "Net 30" },
                new Supplier { SupplierId = "SP00002", CompanyName = "Shenzhen Fabric & Foam Ltd.", ContactPerson = "Ms. Liu", Phone = "+86-755-87654321", Email = "info@szfabric.cn", Address = "Bao'an District, Shenzhen", PaymentTerms = "Net 45" },
                new Supplier { SupplierId = "SP00003", CompanyName = "HK Hardware Trading", ContactPerson = "Mr. Yip", Phone = "27001122", Email = "order@hkhardware.hk", Address = "Kwai Chung, New Territories", PaymentTerms = "COD" }
            });

            DataStore.Items.AddRange(new[]
            {
                new Item { ItemId = "I00001", ItemName = "Executive Office Desk (Oak)", Category = "Office", Unit = "PC", UnitPrice = 4200m, StockQty = 25, ReorderLevel = 5, SupplierId = "SP00001" },
                new Item { ItemId = "I00002", ItemName = "Ergonomic Mesh Chair", Category = "Office", Unit = "PC", UnitPrice = 1850m, StockQty = 60, ReorderLevel = 10, SupplierId = "SP00001" },
                new Item { ItemId = "I00003", ItemName = "3-Seater Leather Sofa", Category = "Living", Unit = "PC", UnitPrice = 8800m, StockQty = 12, ReorderLevel = 3, SupplierId = "SP00002" },
                new Item { ItemId = "I00004", ItemName = "Queen Size Bed Frame", Category = "Bedroom", Unit = "PC", UnitPrice = 3500m, StockQty = 18, ReorderLevel = 4, SupplierId = "SP00001" },
                new Item { ItemId = "I00005", ItemName = "Dining Table Set (6 Seats)", Category = "Dining", Unit = "SET", UnitPrice = 6200m, StockQty = 9, ReorderLevel = 3, SupplierId = "SP00001" },
                new Item { ItemId = "I00006", ItemName = "Bookshelf - 5 Tier", Category = "Living", Unit = "PC", UnitPrice = 1450m, StockQty = 40, ReorderLevel = 8, SupplierId = "SP00001" },
                new Item { ItemId = "I00007", ItemName = "Bedside Cabinet", Category = "Bedroom", Unit = "PC", UnitPrice = 980m, StockQty = 32, ReorderLevel = 6, SupplierId = "SP00001" },
                new Item { ItemId = "I00008", ItemName = "Reception Counter (Premium)", Category = "Office", Unit = "PC", UnitPrice = 12500m, StockQty = 4, ReorderLevel = 2, SupplierId = "SP00001" }
            });

            var order1 = new SalesOrder
            {
                OrderId = "SO00001",
                OrderDate = DateTime.Today.AddDays(-7),
                CustomerId = "C00001",
                Status = "Confirmed",
                RequiredDate = DateTime.Today.AddDays(3),
                Remarks = "Hotel lobby refresh - rush order",
                CreatedBy = "sales"
            };
            order1.Lines.Add(new SalesOrderLine { ItemId = "I00003", ItemName = "3-Seater Leather Sofa", Quantity = 4, UnitPrice = 8800m });
            order1.Lines.Add(new SalesOrderLine { ItemId = "I00006", ItemName = "Bookshelf - 5 Tier", Quantity = 6, UnitPrice = 1450m });

            var order2 = new SalesOrder
            {
                OrderId = "SO00002",
                OrderDate = DateTime.Today.AddDays(-3),
                CustomerId = "C00004",
                Status = "Pending",
                RequiredDate = DateTime.Today.AddDays(10),
                Remarks = "New office fit-out",
                CreatedBy = "sales"
            };
            order2.Lines.Add(new SalesOrderLine { ItemId = "I00001", ItemName = "Executive Office Desk (Oak)", Quantity = 8, UnitPrice = 4200m });
            order2.Lines.Add(new SalesOrderLine { ItemId = "I00002", ItemName = "Ergonomic Mesh Chair", Quantity = 12, UnitPrice = 1850m });
            order2.Lines.Add(new SalesOrderLine { ItemId = "I00008", ItemName = "Reception Counter (Premium)", Quantity = 1, UnitPrice = 12500m });

            DataStore.SalesOrders.Add(order1);
            DataStore.SalesOrders.Add(order2);

            DataStore.Deliveries.Add(new DeliveryNote
            {
                DeliveryId = "DN00001",
                OrderId = "SO00001",
                DeliveryDate = DateTime.Today.AddDays(1),
                DriverName = "Mr. Ho",
                VehicleNo = "LV2345",
                Status = "Scheduled",
                ReplySlipStatus = "Pending",
                CustomerSignature = "",
                Remarks = "Deliver before 10:00 a.m."
            });

            DataStore.Receipts.Add(new GoodsReceived
            {
                ReceiptId = "GR00001",
                ReceiveDate = DateTime.Today.AddDays(-2),
                SupplierId = "SP00001",
                ItemId = "I00001",
                Quantity = 10,
                PurchaseOrderNo = "PO00010",
                ReceivedBy = "warehouse",
                Condition = "Good",
                Remarks = "Carton intact"
            });

            DataStore.ServiceRequests.Add(new AfterServiceRequest
            {
                RequestId = "AS00001",
                RequestDate = DateTime.Today.AddDays(-1),
                OrderId = "SO00001",
                CustomerId = "C00001",
                RequestType = "Replacement",
                ItemId = "I00003",
                Quantity = 1,
                Reason = "One sofa cushion has visible stitching defect",
                Status = "Open",
                HandledBy = "service",
                Resolution = ""
            });
        }
    }
}
