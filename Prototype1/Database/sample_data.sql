-- =============================================================================
-- IDSMS - Sample Seed Data (matches the application's built-in seed)
-- Passwords are stored as SHA-256 hex digests of the plaintext.
--   admin    -> admin123
--   sales    -> sales123
--   logistics-> log123
--   warehouse-> ware123
--   service  -> svc123
-- =============================================================================

USE IDSMS;
GO

-- Users
INSERT INTO dbo.Users (UserId, Username, PasswordHash, FullName, Role, Active) VALUES
 ('U00001','admin',     '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9','System Administrator','Administrator',1),
 ('U00002','sales',     '12be1c1d1c52cba6c98d2113fcf03f0eb83a3552efa01f88a06c4995c5c0d6e2','Sales Officer',       'Sales',1),
 ('U00003','logistics', '32a5a07db70a781cf6e0a83b3a08e0b8ec77824a1ed30b18b3a1f96d6e4dd2bb','Logistics Officer',   'Logistics',1),
 ('U00004','warehouse', 'ea34c7b2db28e3eb6deec3a0a7a3b1f8a45cbf5e3a5d44b3f9620f6b27dcf03b','Warehouse Officer',   'Warehouse',1),
 ('U00005','service',   '7cf6c6b1d1a3a7b9c50a73f1c2ef5d9ea1ab8b73da9f7c20e89d0c63b78f4a8e','After-Service Officer','Service',1);
-- (Hash values above are illustrative; the application regenerates them at first run.)

-- Customers
INSERT INTO dbo.Customers (CustomerId, CompanyName, ContactPerson, Phone, Email, Address, CustomerType) VALUES
 ('C00001','Harbour View Hotel','Mr. Lam','29001234','purchasing@hvhotel.hk','12 Harbour Road, Wan Chai, Hong Kong','Corporate'),
 ('C00002','Golden Star Restaurant','Ms. Tang','29005678','admin@goldenstar.hk','55 Nathan Road, Tsim Sha Tsui','Corporate'),
 ('C00003','Ng Family Residence','Mrs. Ng','92223344','ngfamily@gmail.com','Flat 5B, Sunshine Garden, Tai Po','Retail'),
 ('C00004','City Office Tower Ltd.','Mr. Chiu','29008888','facilities@cot.com.hk','168 Queen''s Road Central','Corporate');

-- Suppliers
INSERT INTO dbo.Suppliers (SupplierId, CompanyName, ContactPerson, Phone, Email, Address, PaymentTerms) VALUES
 ('SP00001','Dongguan Woodcraft Co.','Mr. Zhang','+86-769-23456789','sales@dgwood.cn','Industrial Zone, Dongguan, Guangdong','Net 30'),
 ('SP00002','Shenzhen Fabric & Foam Ltd.','Ms. Liu','+86-755-87654321','info@szfabric.cn','Bao''an District, Shenzhen','Net 45'),
 ('SP00003','HK Hardware Trading','Mr. Yip','27001122','order@hkhardware.hk','Kwai Chung, New Territories','COD');

-- Staff
INSERT INTO dbo.Staff (StaffId, FullName, Position, Department, Phone, Email, HireDate) VALUES
 ('S00001','Chan Tai Man','Sales Manager','Sales and Marketing','98765432','tmchan@plf.com.hk','2020-03-01'),
 ('S00002','Wong Siu Ling','Sales Officer','Sales and Marketing','97654321','slwong@plf.com.hk','2021-06-15'),
 ('S00003','Lee Ka Ho','Warehouse Supervisor','Inventory Control','96543210','khlee@plf.com.hk','2019-09-01'),
 ('S00004','Cheung Mei Yee','Logistics Officer','Logistics','95432109','mycheung@plf.com.hk','2022-01-10');

-- Items
INSERT INTO dbo.Items (ItemId, ItemName, Category, Unit, UnitPrice, StockQty, ReorderLevel, SupplierId) VALUES
 ('I00001','Executive Office Desk (Oak)','Office','PC',4200,25,5,'SP00001'),
 ('I00002','Ergonomic Mesh Chair','Office','PC',1850,60,10,'SP00001'),
 ('I00003','3-Seater Leather Sofa','Living','PC',8800,12,3,'SP00002'),
 ('I00004','Queen Size Bed Frame','Bedroom','PC',3500,18,4,'SP00001'),
 ('I00005','Dining Table Set (6 Seats)','Dining','SET',6200,9,3,'SP00001'),
 ('I00006','Bookshelf - 5 Tier','Living','PC',1450,40,8,'SP00001'),
 ('I00007','Bedside Cabinet','Bedroom','PC',980,32,6,'SP00001'),
 ('I00008','Reception Counter (Premium)','Office','PC',12500,4,2,'SP00001');

-- Sales Orders
INSERT INTO dbo.SalesOrders (OrderId, OrderDate, RequiredDate, CustomerId, Status, Remarks, CreatedBy) VALUES
 ('SO00001', GETDATE()-7, GETDATE()+3, 'C00001','Confirmed','Hotel lobby refresh - rush order','sales'),
 ('SO00002', GETDATE()-3, GETDATE()+10,'C00004','Pending','New office fit-out','sales');

INSERT INTO dbo.SalesOrderLines (OrderId, ItemId, ItemName, Quantity, UnitPrice) VALUES
 ('SO00001','I00003','3-Seater Leather Sofa',4,8800),
 ('SO00001','I00006','Bookshelf - 5 Tier',6,1450),
 ('SO00002','I00001','Executive Office Desk (Oak)',8,4200),
 ('SO00002','I00002','Ergonomic Mesh Chair',12,1850),
 ('SO00002','I00008','Reception Counter (Premium)',1,12500);

-- Delivery Note
INSERT INTO dbo.DeliveryNotes (DeliveryId, OrderId, DeliveryDate, DriverName, VehicleNo, Status, ReplySlipStatus, CustomerSignature, Remarks) VALUES
 ('DN00001','SO00001', GETDATE()+1, 'Mr. Ho','LV2345','Scheduled','Pending','', 'Deliver before 10:00 a.m.');

-- Goods Received
INSERT INTO dbo.GoodsReceived (ReceiptId, ReceiveDate, SupplierId, ItemId, Quantity, PurchaseOrderNo, ReceivedBy, Condition, Remarks) VALUES
 ('GR00001', GETDATE()-2, 'SP00001','I00001',10,'PO00010','warehouse','Good','Carton intact');

-- After-Service Request
INSERT INTO dbo.AfterServiceRequests (RequestId, RequestDate, OrderId, CustomerId, RequestType, ItemId, Quantity, Reason, Status, HandledBy, Resolution) VALUES
 ('AS00001', GETDATE()-1, 'SO00001','C00001','Replacement','I00003',1,'One sofa cushion has visible stitching defect','Open','service','');
GO
