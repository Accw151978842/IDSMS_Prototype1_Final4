-- ============================================================
--  IDSMS  —  Setup script for NEW database `idsms2`
--  建立全新空白資料庫 + 全部 15 個 tables
--  IDSMS app 第一次 run 嘅時候，DataStore.LoadAll() 會自動 seed
--  Users / Staff / Customers / Suppliers / Items / Sales Orders 等
--  Base data。 RMR + Procurement sample data 由本檔案 INSERT。
-- ============================================================
--  使用方法（phpMyAdmin）：
--    1) 頂部 Tab 揀 SQL
--    2) Copy & paste 全部內容
--    3) 揀「Go / 執行」
--  使用方法（CLI）：
--    mysql -u root < setup_idsms2.sql
-- ============================================================

-- 建立新資料庫
CREATE DATABASE IF NOT EXISTS idsms2
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE idsms2;

-- ============================================================
--  BASE TABLES (11)
-- ============================================================

-- Users
CREATE TABLE IF NOT EXISTS users (
    user_id      VARCHAR(20)  PRIMARY KEY,
    username     VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(200) NOT NULL,
    full_name    VARCHAR(100) NOT NULL,
    role         VARCHAR(30)  NOT NULL,
    active       TINYINT(1)   NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Staff
CREATE TABLE IF NOT EXISTS staff (
    staff_id    VARCHAR(20)  PRIMARY KEY,
    full_name   VARCHAR(100) NOT NULL,
    position    VARCHAR(80)  NOT NULL,
    department  VARCHAR(80)  NOT NULL,
    phone       VARCHAR(30),
    email       VARCHAR(100),
    hire_date   DATE         NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Customers
CREATE TABLE IF NOT EXISTS customers (
    customer_id     VARCHAR(20)  PRIMARY KEY,
    company_name    VARCHAR(150) NOT NULL,
    contact_person  VARCHAR(100),
    phone           VARCHAR(30),
    email           VARCHAR(100),
    address         VARCHAR(250),
    customer_type   VARCHAR(30)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Suppliers
CREATE TABLE IF NOT EXISTS suppliers (
    supplier_id     VARCHAR(20)  PRIMARY KEY,
    company_name    VARCHAR(150) NOT NULL,
    contact_person  VARCHAR(100),
    phone           VARCHAR(30),
    email           VARCHAR(100),
    address         VARCHAR(250),
    payment_terms   VARCHAR(80)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sales Orders
CREATE TABLE IF NOT EXISTS sales_orders (
    order_id       VARCHAR(20)  PRIMARY KEY,
    order_date     DATE         NOT NULL,
    customer_id    VARCHAR(20)  NOT NULL,
    status         VARCHAR(30)  NOT NULL,
    required_date  DATE         NOT NULL,
    remarks        VARCHAR(500),
    created_by     VARCHAR(50),
    stock_deducted TINYINT(1)   NOT NULL DEFAULT 0,  -- 1 once shipped & stock deducted
    FOREIGN KEY (customer_id) REFERENCES customers(customer_id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Sales Order Lines
CREATE TABLE IF NOT EXISTS sales_order_lines (
    id          INT          AUTO_INCREMENT PRIMARY KEY,
    order_id    VARCHAR(20)  NOT NULL,
    item_id     VARCHAR(20)  NOT NULL,
    item_name   VARCHAR(150) NOT NULL,
    quantity    INT          NOT NULL,
    unit_price  DECIMAL(12,2) NOT NULL,
    FOREIGN KEY (order_id) REFERENCES sales_orders(order_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Audit Logs
CREATE TABLE IF NOT EXISTS audit_logs (
    log_id     VARCHAR(20)   PRIMARY KEY,
    timestamp  DATETIME      NOT NULL,
    username   VARCHAR(50),
    action     VARCHAR(100),
    detail     VARCHAR(500)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  FINAL4 NEW TABLES (4)
-- ============================================================

-- Raw Material Requests (Production)
CREATE TABLE IF NOT EXISTS raw_material_requests (
    rmr_id        VARCHAR(20)  PRIMARY KEY,
    request_date  DATE         NOT NULL,
    requested_by  VARCHAR(20),
    department    VARCHAR(50),
    status        VARCHAR(30)  NOT NULL DEFAULT 'Pending',
    notes         VARCHAR(500)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- RMR Line Items
CREATE TABLE IF NOT EXISTS rmr_lines (
    id          INT           AUTO_INCREMENT PRIMARY KEY,
    rmr_id      VARCHAR(20)   NOT NULL,
    item_id     VARCHAR(20)   NOT NULL,
    item_name   VARCHAR(150)  NOT NULL,
    qty_needed  INT           NOT NULL,
    notes       VARCHAR(250),
    FOREIGN KEY (rmr_id) REFERENCES raw_material_requests(rmr_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
    FOREIGN KEY (supplier_id)   REFERENCES suppliers(supplier_id)        ON DELETE SET NULL,
    FOREIGN KEY (linked_rmr_id) REFERENCES raw_material_requests(rmr_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Procurement Line Items
CREATE TABLE IF NOT EXISTS procurement_lines (
    id          INT            AUTO_INCREMENT PRIMARY KEY,
    po_id       VARCHAR(20)    NOT NULL,
    item_id     VARCHAR(20)    NOT NULL,
    item_name   VARCHAR(150)   NOT NULL,
    quantity    INT            NOT NULL,
    unit_price  DECIMAL(12,2)  NOT NULL,
    FOREIGN KEY (po_id) REFERENCES procurements(po_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  QUOTATIONS (P2 enhancement)
--  Flow: RMR -> RFQ -> 多個 supplier 報價 -> 揀中 -> Convert to PO
--  ★ 必須建立：DataStore.SaveAll() 會 DELETE/INSERT 呢兩個表，
--    如果缺少，一登入做 SaveAll() 就會掉 exception、儲存失敗。
-- ============================================================
CREATE TABLE IF NOT EXISTS quotations (
    quotation_id     VARCHAR(20)  NOT NULL,
    quotation_no     VARCHAR(50),                  -- supplier-side reference
    rmr_id           VARCHAR(20),                  -- linked RMR (nullable)
    supplier_id      VARCHAR(20),
    quote_date       DATE         NOT NULL,
    valid_until      DATE,
    lead_time_days   INT,
    payment_terms    VARCHAR(100),
    status           VARCHAR(20)  NOT NULL DEFAULT 'Pending',  -- Pending/Selected/Rejected/Expired/Converted
    converted_po_id  VARCHAR(20),                  -- PO id after conversion
    created_by       VARCHAR(50),
    remarks          VARCHAR(500),
    PRIMARY KEY (quotation_id),
    INDEX idx_quot_rmr (rmr_id),
    INDEX idx_quot_sup (supplier_id),
    INDEX idx_quot_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS quotation_lines (
    line_id          INT          AUTO_INCREMENT,
    quotation_id     VARCHAR(20)  NOT NULL,
    item_id          VARCHAR(20)  NOT NULL,
    item_name        VARCHAR(255),
    quantity         INT          NOT NULL DEFAULT 0,
    unit_price       DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    PRIMARY KEY (line_id),
    INDEX idx_qline_qid (quotation_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  SALES QUOTATIONS (P2 enhancement — sales side)
--  Flow: Customer enquiry -> Sales Quotation -> Accepted
--        -> Convert to Sales Order
--  ★ 必須建立：DataStore.SaveAll() 會 DELETE/INSERT 呢兩個表，
--    如果缺少，一儲存就會掉 exception。
--  (不設硬 FK 至 customers，因為 SaveAll() 會喺同一 transaction
--   先 DELETE customers，硬 FK 會引致刪除/插入次序問題。)
-- ============================================================
CREATE TABLE IF NOT EXISTS sales_quotations (
    quotation_id       VARCHAR(20)  NOT NULL,
    customer_id        VARCHAR(20),
    quote_date         DATE         NOT NULL,
    valid_until        DATE,
    status             VARCHAR(20)  NOT NULL DEFAULT 'Draft',  -- Draft/Sent/Accepted/Rejected/Expired/Converted
    converted_order_id VARCHAR(20),                            -- Sales Order id after conversion
    created_by         VARCHAR(50),
    remarks            VARCHAR(500),
    PRIMARY KEY (quotation_id),
    INDEX idx_sq_customer (customer_id),
    INDEX idx_sq_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS sales_quotation_lines (
    line_id          INT          AUTO_INCREMENT,
    quotation_id     VARCHAR(20)  NOT NULL,
    item_id          VARCHAR(20)  NOT NULL,
    item_name        VARCHAR(255),
    quantity         INT          NOT NULL DEFAULT 0,
    unit_price       DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    PRIMARY KEY (line_id),
    INDEX idx_sqline_qid (quotation_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  完成 — 全部 19 個 tables 已建立
--  下一步：
--    1) 啟動 IDSMS app  → DataStore 會自動 seed Users/Staff/
--       Customers/Suppliers/Items/SalesOrders/RMR/PO base data
--    2) 用 admin / admin123 登入
-- ============================================================
