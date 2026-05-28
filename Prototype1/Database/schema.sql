-- =============================================================================
-- Integrated Delivery Services Management System (IDSMS)
-- Premium Living Furniture Co. Ltd.
-- Prototype I - Database Schema Reference
-- Target: SQL Server / SQL Server LocalDB / SQL Server Express
-- =============================================================================
-- Note: The prototype application currently stores data in JSON files under
-- the application's AppData folder for ease of demonstration. This script is
-- provided as a reference for the database design and can be used to create
-- an equivalent SQL Server database for future iterations of the project.
-- =============================================================================

IF DB_ID('IDSMS') IS NULL
BEGIN
    CREATE DATABASE IDSMS;
END
GO

USE IDSMS;
GO

-- -----------------------------------------------------------------------------
-- Users (System Security & Control)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
CREATE TABLE dbo.Users (
    UserId       NVARCHAR(10)  NOT NULL PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(128) NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    Role         NVARCHAR(30)  NOT NULL,
    Active       BIT           NOT NULL DEFAULT 1
);

-- -----------------------------------------------------------------------------
-- Staff
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Staff', 'U') IS NOT NULL DROP TABLE dbo.Staff;
CREATE TABLE dbo.Staff (
    StaffId    NVARCHAR(10)  NOT NULL PRIMARY KEY,
    FullName   NVARCHAR(100) NOT NULL,
    Position   NVARCHAR(50),
    Department NVARCHAR(50),
    Phone      NVARCHAR(30),
    Email      NVARCHAR(100),
    HireDate   DATE
);

-- -----------------------------------------------------------------------------
-- Customers
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
CREATE TABLE dbo.Customers (
    CustomerId    NVARCHAR(10)  NOT NULL PRIMARY KEY,
    CompanyName   NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone         NVARCHAR(30),
    Email         NVARCHAR(100),
    Address       NVARCHAR(255),
    CustomerType  NVARCHAR(30)
);

-- -----------------------------------------------------------------------------
-- Suppliers
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;
CREATE TABLE dbo.Suppliers (
    SupplierId    NVARCHAR(10)  NOT NULL PRIMARY KEY,
    CompanyName   NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone         NVARCHAR(30),
    Email         NVARCHAR(100),
    Address       NVARCHAR(255),
    PaymentTerms  NVARCHAR(50)
);

-- -----------------------------------------------------------------------------
-- Items (Inventory Master)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Items', 'U') IS NOT NULL DROP TABLE dbo.Items;
CREATE TABLE dbo.Items (
    ItemId        NVARCHAR(10)  NOT NULL PRIMARY KEY,
    ItemName      NVARCHAR(150) NOT NULL,
    Category      NVARCHAR(50),
    Unit          NVARCHAR(20),
    UnitPrice     DECIMAL(12,2) NOT NULL DEFAULT 0,
    StockQty      INT           NOT NULL DEFAULT 0,
    ReorderLevel  INT           NOT NULL DEFAULT 0,
    SupplierId    NVARCHAR(10) NULL REFERENCES dbo.Suppliers(SupplierId)
);

-- -----------------------------------------------------------------------------
-- Sales Orders
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.SalesOrderLines', 'U') IS NOT NULL DROP TABLE dbo.SalesOrderLines;
IF OBJECT_ID('dbo.SalesOrders', 'U')     IS NOT NULL DROP TABLE dbo.SalesOrders;

CREATE TABLE dbo.SalesOrders (
    OrderId      NVARCHAR(10) NOT NULL PRIMARY KEY,
    OrderDate    DATE         NOT NULL,
    RequiredDate DATE,
    CustomerId   NVARCHAR(10) NOT NULL REFERENCES dbo.Customers(CustomerId),
    Status       NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    Remarks      NVARCHAR(255),
    CreatedBy    NVARCHAR(50)
);

CREATE TABLE dbo.SalesOrderLines (
    LineId    INT IDENTITY(1,1) PRIMARY KEY,
    OrderId   NVARCHAR(10)  NOT NULL REFERENCES dbo.SalesOrders(OrderId),
    ItemId    NVARCHAR(10)  NOT NULL REFERENCES dbo.Items(ItemId),
    ItemName  NVARCHAR(150) NOT NULL,
    Quantity  INT           NOT NULL,
    UnitPrice DECIMAL(12,2) NOT NULL
);

-- -----------------------------------------------------------------------------
-- Delivery Notes (with Reply Slip status)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.DeliveryNotes', 'U') IS NOT NULL DROP TABLE dbo.DeliveryNotes;
CREATE TABLE dbo.DeliveryNotes (
    DeliveryId        NVARCHAR(10) NOT NULL PRIMARY KEY,
    OrderId           NVARCHAR(10) NOT NULL REFERENCES dbo.SalesOrders(OrderId),
    DeliveryDate      DATE         NOT NULL,
    DriverName        NVARCHAR(100),
    VehicleNo         NVARCHAR(20),
    Status            NVARCHAR(20) NOT NULL DEFAULT 'Scheduled',
    ReplySlipStatus   NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    CustomerSignature NVARCHAR(100),
    Remarks           NVARCHAR(255)
);

-- -----------------------------------------------------------------------------
-- Goods Received (Inward goods record)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.GoodsReceived', 'U') IS NOT NULL DROP TABLE dbo.GoodsReceived;
CREATE TABLE dbo.GoodsReceived (
    ReceiptId       NVARCHAR(10) NOT NULL PRIMARY KEY,
    ReceiveDate     DATE         NOT NULL,
    SupplierId      NVARCHAR(10) NOT NULL REFERENCES dbo.Suppliers(SupplierId),
    ItemId          NVARCHAR(10) NOT NULL REFERENCES dbo.Items(ItemId),
    Quantity        INT          NOT NULL,
    PurchaseOrderNo NVARCHAR(20),
    ReceivedBy      NVARCHAR(50),
    Condition       NVARCHAR(20),
    Remarks         NVARCHAR(255)
);

-- -----------------------------------------------------------------------------
-- After-Service Requests
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.AfterServiceRequests', 'U') IS NOT NULL DROP TABLE dbo.AfterServiceRequests;
CREATE TABLE dbo.AfterServiceRequests (
    RequestId   NVARCHAR(10) NOT NULL PRIMARY KEY,
    RequestDate DATE         NOT NULL,
    OrderId     NVARCHAR(10),
    CustomerId  NVARCHAR(10) NOT NULL REFERENCES dbo.Customers(CustomerId),
    RequestType NVARCHAR(20) NOT NULL,
    ItemId      NVARCHAR(10) NOT NULL REFERENCES dbo.Items(ItemId),
    Quantity    INT          NOT NULL,
    Reason      NVARCHAR(500),
    Status      NVARCHAR(20) NOT NULL DEFAULT 'Open',
    HandledBy   NVARCHAR(50),
    Resolution  NVARCHAR(500)
);

-- -----------------------------------------------------------------------------
-- Audit Log (System Security & Control)
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
CREATE TABLE dbo.AuditLogs (
    LogId     NVARCHAR(15)  NOT NULL PRIMARY KEY,
    Timestamp DATETIME2     NOT NULL,
    Username  NVARCHAR(50),
    Action    NVARCHAR(100),
    Detail    NVARCHAR(500)
);
GO
