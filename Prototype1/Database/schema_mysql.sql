-- ============================================================
--  IDSMS  —  MySQL Schema
--  Run this once in MySQL Workbench or CLI before starting app
-- ============================================================

CREATE DATABASE IF NOT EXISTS idsms CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE idsms;

-- Users
CREATE TABLE IF NOT EXISTS users (
    user_id      VARCHAR(20)  PRIMARY KEY,
    username     VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(200) NOT NULL,
    full_name    VARCHAR(100) NOT NULL,
    role         VARCHAR(30)  NOT NULL,
    active       TINYINT(1)   NOT NULL DEFAULT 1
);

-- Staff
CREATE TABLE IF NOT EXISTS staff (
    staff_id    VARCHAR(20)  PRIMARY KEY,
    full_name   VARCHAR(100) NOT NULL,
    position    VARCHAR(80)  NOT NULL,
    department  VARCHAR(80)  NOT NULL,
    phone       VARCHAR(30),
    email       VARCHAR(100),
    hire_date   DATE         NOT NULL
);

-- Customers
CREATE TABLE IF NOT EXISTS customers (
    customer_id     VARCHAR(20)  PRIMARY KEY,
    company_name    VARCHAR(150) NOT NULL,
    contact_person  VARCHAR(100),
    phone           VARCHAR(30),
    email           VARCHAR(100),
    address         VARCHAR(250),
    customer_type   VARCHAR(30)
);

-- Suppliers
CREATE TABLE IF NOT EXISTS suppliers (
    supplier_id     VARCHAR(20)  PRIMARY KEY,
    company_name    VARCHAR(150) NOT NULL,
    contact_person  VARCHAR(100),
    phone           VARCHAR(30),
    email           VARCHAR(100),
    address         VARCHAR(250),
    payment_terms   VARCHAR(80)
);

-- Items
CREATE TABLE IF NOT EXISTS items (
    item_id        VARCHAR(20)    PRIMARY KEY,
    item_name      VARCHAR(150)   NOT NULL,
    category       VARCHAR(80),
    unit           VARCHAR(20),
    unit_price     DECIMAL(12,2)  NOT NULL DEFAULT 0,
    stock_qty      INT            NOT NULL DEFAULT 0,
    reorder_level  INT            NOT NULL DEFAULT 0,
    supplier_id    VARCHAR(20),
    FOREIGN KEY (supplier_id) REFERENCES suppliers(supplier_id) ON DELETE SET NULL
);

-- Sales Orders
CREATE TABLE IF NOT EXISTS sales_orders (
    order_id       VARCHAR(20)  PRIMARY KEY,
    order_date     DATE         NOT NULL,
    customer_id    VARCHAR(20)  NOT NULL,
    status         VARCHAR(30)  NOT NULL,
    required_date  DATE         NOT NULL,
    remarks        VARCHAR(500),
    created_by     VARCHAR(50),
    FOREIGN KEY (customer_id) REFERENCES customers(customer_id) ON DELETE RESTRICT
);

-- Sales Order Lines
CREATE TABLE IF NOT EXISTS sales_order_lines (
    id          INT          AUTO_INCREMENT PRIMARY KEY,
    order_id    VARCHAR(20)  NOT NULL,
    item_id     VARCHAR(20)  NOT NULL,
    item_name   VARCHAR(150) NOT NULL,
    quantity    INT          NOT NULL,
    unit_price  DECIMAL(12,2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES sales_orders(order_id) ON DELETE CASCADE
);

-- Delivery Notes
CREATE TABLE IF NOT EXISTS delivery_notes (
    delivery_id         VARCHAR(20)  PRIMARY KEY,
    order_id            VARCHAR(20)  NOT NULL,
    delivery_date       DATE         NOT NULL,
    driver_name         VARCHAR(100),
    vehicle_no          VARCHAR(30),
    status              VARCHAR(30),
    reply_slip_status   VARCHAR(50),
    customer_signature  VARCHAR(200),
    remarks             VARCHAR(500),
    FOREIGN KEY (order_id) REFERENCES sales_orders(order_id) ON DELETE RESTRICT
);

-- Goods Received
CREATE TABLE IF NOT EXISTS goods_received (
    receipt_id        VARCHAR(20)  PRIMARY KEY,
    receive_date      DATE         NOT NULL,
    supplier_id       VARCHAR(20),
    item_id           VARCHAR(20),
    quantity          INT          NOT NULL,
    purchase_order_no VARCHAR(50),
    received_by       VARCHAR(100),
    `condition`       VARCHAR(50),
    remarks           VARCHAR(500)
);

-- After Service Requests
CREATE TABLE IF NOT EXISTS after_service (
    request_id   VARCHAR(20)  PRIMARY KEY,
    request_date DATE         NOT NULL,
    order_id     VARCHAR(20),
    customer_id  VARCHAR(20),
    request_type VARCHAR(50),
    item_id      VARCHAR(20),
    quantity     INT          NOT NULL DEFAULT 0,
    reason       VARCHAR(500),
    status       VARCHAR(30),
    handled_by   VARCHAR(100),
    resolution   VARCHAR(500)
);

-- Audit Logs
CREATE TABLE IF NOT EXISTS audit_logs (
    log_id     VARCHAR(20)   PRIMARY KEY,
    timestamp  DATETIME      NOT NULL,
    username   VARCHAR(50),
    action     VARCHAR(100),
    detail     VARCHAR(500)
);

-- ============================================================
--  FINAL4 NEW TABLES
-- ============================================================

-- Raw Material Requests (Production)
CREATE TABLE IF NOT EXISTS raw_material_requests (
    rmr_id        VARCHAR(20)  PRIMARY KEY,
    request_date  DATE         NOT NULL,
    requested_by  VARCHAR(20),
    department    VARCHAR(50),
    status        VARCHAR(30)  NOT NULL DEFAULT 'Pending',
    notes         VARCHAR(500)
);

-- RMR line items
CREATE TABLE IF NOT EXISTS rmr_lines (
    id          INT           AUTO_INCREMENT PRIMARY KEY,
    rmr_id      VARCHAR(20)   NOT NULL,
    item_id     VARCHAR(20)   NOT NULL,
    item_name   VARCHAR(150)  NOT NULL,
    qty_needed  INT           NOT NULL,
    notes       VARCHAR(250),
    FOREIGN KEY (rmr_id) REFERENCES raw_material_requests(rmr_id) ON DELETE CASCADE
);

-- Procurements (Purchase Orders)
CREATE TABLE IF NOT EXISTS procurements (
    po_id              VARCHAR(20)  PRIMARY KEY,
    supplier_id        VARCHAR(20),
    order_date         DATE         NOT NULL,
    expected_delivery  DATE         NOT NULL,
    status             VARCHAR(30)  NOT NULL DEFAULT 'Draft',
    linked_rmr_id      VARCHAR(20)  NULL,
    created_by         VARCHAR(50),
    remarks            VARCHAR(500),
    FOREIGN KEY (supplier_id)   REFERENCES suppliers(supplier_id)            ON DELETE SET NULL,
    FOREIGN KEY (linked_rmr_id) REFERENCES raw_material_requests(rmr_id)     ON DELETE SET NULL
);

-- Procurement line items
CREATE TABLE IF NOT EXISTS procurement_lines (
    id          INT            AUTO_INCREMENT PRIMARY KEY,
    po_id       VARCHAR(20)    NOT NULL,
    item_id     VARCHAR(20)    NOT NULL,
    item_name   VARCHAR(150)   NOT NULL,
    quantity    INT            NOT NULL,
    unit_price  DECIMAL(12,2)  NOT NULL,
    FOREIGN KEY (po_id) REFERENCES procurements(po_id) ON DELETE CASCADE
);

-- ============================================================
--  FINAL4 SAMPLE DATA (run after the app has seeded base tables)
-- ============================================================

INSERT IGNORE INTO raw_material_requests(rmr_id, request_date, requested_by, department, status, notes) VALUES
('RMR00001', CURDATE() - INTERVAL 5 DAY, 'S00003', 'Warehouse', 'Approved', 'Restock fast-moving office furniture'),
('RMR00002', CURDATE() - INTERVAL 2 DAY, 'S00001', 'Production', 'Pending',  'Living-room set for upcoming hotel project');

INSERT IGNORE INTO rmr_lines(rmr_id, item_id, item_name, qty_needed, notes) VALUES
('RMR00001', 'I00001', 'Executive Office Desk (Oak)', 10, 'Below reorder level soon'),
('RMR00001', 'I00002', 'Ergonomic Mesh Chair',        20, ''),
('RMR00002', 'I00003', '3-Seater Leather Sofa',        6, 'Project Harbour View phase 2'),
('RMR00002', 'I00006', 'Bookshelf - 5 Tier',          12, '');

INSERT IGNORE INTO procurements(po_id, supplier_id, order_date, expected_delivery, status, linked_rmr_id, created_by, remarks) VALUES
('PO00001', 'SP00001', CURDATE() - INTERVAL 3 DAY, CURDATE() + INTERVAL 11 DAY, 'Sent',  'RMR00001', 'admin', 'Net 30 terms'),
('PO00002', 'SP00002', CURDATE() - INTERVAL 1 DAY, CURDATE() + INTERVAL 14 DAY, 'Draft', NULL,       'admin', 'Awaiting confirmation');

INSERT IGNORE INTO procurement_lines(po_id, item_id, item_name, quantity, unit_price) VALUES
('PO00001', 'I00001', 'Executive Office Desk (Oak)', 10, 4200.00),
('PO00001', 'I00002', 'Ergonomic Mesh Chair',        20, 1850.00),
('PO00002', 'I00003', '3-Seater Leather Sofa',        4, 8800.00);

-- Tip: After running this script, restart the IDSMS app so DataStore.LoadAll() picks up the new rows.
