-- ============================================================
--  IDSMS  —  Final4 Migration Script
--  只新增 Final4 嘅 4 個新 tables + sample data
--  可在 phpMyAdmin > Import 直接執行（用 INSERT IGNORE，跑多次都安全）
-- ============================================================
--  使用方法（phpMyAdmin）：
--    1) 左邊揀資料庫 `idsms`
--    2) 頂部 Tab 揀「Import / 匯入」
--    3) Choose File 揀本檔案 → Go
--    4) 完成後重啟 IDSMS app，DataStore.LoadAll() 會載入新 rows
-- ============================================================

USE idsms;

-- ------------------------------------------------------------
--  1) Raw Material Requests (Production)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS raw_material_requests (
    rmr_id        VARCHAR(20)  PRIMARY KEY,
    request_date  DATE         NOT NULL,
    requested_by  VARCHAR(20),
    department    VARCHAR(50),
    status        VARCHAR(30)  NOT NULL DEFAULT 'Pending',
    notes         VARCHAR(500)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
--  2) RMR Line Items
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS rmr_lines (
    id          INT           AUTO_INCREMENT PRIMARY KEY,
    rmr_id      VARCHAR(20)   NOT NULL,
    item_id     VARCHAR(20)   NOT NULL,
    item_name   VARCHAR(150)  NOT NULL,
    qty_needed  INT           NOT NULL,
    notes       VARCHAR(250),
    FOREIGN KEY (rmr_id) REFERENCES raw_material_requests(rmr_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------------------------------------------------------------
--  3) Procurements (Purchase Orders)
-- ------------------------------------------------------------
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

-- ------------------------------------------------------------
--  4) Procurement Line Items
-- ------------------------------------------------------------
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
--  Sample Data
--  注意：以下 INSERT 依賴 base tables 已經有
--        staff (S00001, S00003)、suppliers (SP00001, SP00002)、
--        items (I00001~I00006)
--  如果 base table 未有資料，先跑 schema_mysql.sql 或開 IDSMS app
--  讓 DataStore 先 seed 一次。
-- ============================================================

-- Raw Material Requests
INSERT IGNORE INTO raw_material_requests(rmr_id, request_date, requested_by, department, status, notes) VALUES
('RMR00001', CURDATE() - INTERVAL 5 DAY, 'S00003', 'Warehouse',  'Approved', 'Restock fast-moving office furniture'),
('RMR00002', CURDATE() - INTERVAL 2 DAY, 'S00001', 'Production', 'Pending',  'Living-room set for upcoming hotel project');

-- RMR Lines
INSERT IGNORE INTO rmr_lines(rmr_id, item_id, item_name, qty_needed, notes) VALUES
('RMR00001', 'I00001', 'Executive Office Desk (Oak)', 10, 'Below reorder level soon'),
('RMR00001', 'I00002', 'Ergonomic Mesh Chair',        20, ''),
('RMR00002', 'I00003', '3-Seater Leather Sofa',        6, 'Project Harbour View phase 2'),
('RMR00002', 'I00006', 'Bookshelf - 5 Tier',          12, '');

-- Procurements (Purchase Orders)
INSERT IGNORE INTO procurements(po_id, supplier_id, order_date, expected_delivery, status, linked_rmr_id, created_by, remarks) VALUES
('PO00001', 'SP00001', CURDATE() - INTERVAL 3 DAY, CURDATE() + INTERVAL 11 DAY, 'Sent',  'RMR00001', 'admin', 'Net 30 terms'),
('PO00002', 'SP00002', CURDATE() - INTERVAL 1 DAY, CURDATE() + INTERVAL 14 DAY, 'Draft', NULL,       'admin', 'Awaiting confirmation');

-- Procurement Lines
INSERT IGNORE INTO procurement_lines(po_id, item_id, item_name, quantity, unit_price) VALUES
('PO00001', 'I00001', 'Executive Office Desk (Oak)', 10, 4200.00),
('PO00001', 'I00002', 'Ergonomic Mesh Chair',        20, 1850.00),
('PO00002', 'I00003', '3-Seater Leather Sofa',        4, 8800.00);

-- ============================================================
--  QUOTATIONS (P2 enhancement)
--  Flow: RMR -> RFQ -> 多個 supplier 報價 -> 揀中 -> Convert to PO
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS quotation_lines (
    line_id          INT          AUTO_INCREMENT,
    quotation_id     VARCHAR(20)  NOT NULL,
    item_id          VARCHAR(20)  NOT NULL,
    item_name        VARCHAR(255),
    quantity         INT          NOT NULL DEFAULT 0,
    unit_price       DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    PRIMARY KEY (line_id),
    INDEX idx_qline_qid (quotation_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Sample data: 3 suppliers competing on RMR00001 (Executive Desk + Ergonomic Chair)
INSERT IGNORE INTO quotations(quotation_id, quotation_no, rmr_id, supplier_id, quote_date, valid_until,
    lead_time_days, payment_terms, status, converted_po_id, created_by, remarks) VALUES
('QT00001', 'SF-Q-2026-018', 'RMR00001', 'SP00001', CURDATE() - INTERVAL 8 DAY,  CURDATE() + INTERVAL 22 DAY, 14, 'Net 30',   'Selected',  'PO00001', 'admin', 'Best total + reasonable lead time'),
('QT00002', 'GHF-2026-1124', 'RMR00001', 'SP00002', CURDATE() - INTERVAL 8 DAY,  CURDATE() + INTERVAL 22 DAY, 10, 'Net 30',   'Rejected',  NULL,      'admin', 'Faster but 8% more expensive'),
('QT00003', 'IL-Q-2026-007', 'RMR00001', 'SP00003', CURDATE() - INTERVAL 7 DAY,  CURDATE() + INTERVAL 23 DAY, 21, 'Net 60',   'Rejected',  NULL,      'admin', 'Cheapest but 21-day lead time'),
-- 2 suppliers competing on RMR00002 (Sofa + Bookshelf)
('QT00004', 'IL-Q-2026-009', 'RMR00002', 'SP00003', CURDATE() - INTERVAL 5 DAY,  CURDATE() + INTERVAL 25 DAY, 18, 'Net 30',   'Pending',   NULL,      'admin', 'Awaiting comparison'),
('QT00005', 'GHF-2026-1140', 'RMR00002', 'SP00002', CURDATE() - INTERVAL 4 DAY,  CURDATE() + INTERVAL 26 DAY, 12, 'Net 30',   'Pending',   NULL,      'admin', 'Awaiting comparison');

INSERT IGNORE INTO quotation_lines(quotation_id, item_id, item_name, quantity, unit_price) VALUES
-- QT00001 (Selected - became PO00001)
('QT00001', 'I00001', 'Executive Office Desk (Oak)', 10, 4200.00),
('QT00001', 'I00002', 'Ergonomic Mesh Chair',        20, 1850.00),
-- QT00002 (Rejected - higher price, faster lead)
('QT00002', 'I00001', 'Executive Office Desk (Oak)', 10, 4500.00),
('QT00002', 'I00002', 'Ergonomic Mesh Chair',        20, 2050.00),
-- QT00003 (Rejected - cheapest but 21-day lead)
('QT00003', 'I00001', 'Executive Office Desk (Oak)', 10, 3950.00),
('QT00003', 'I00002', 'Ergonomic Mesh Chair',        20, 1720.00),
-- QT00004 (Pending on RMR00002)
('QT00004', 'I00003', '3-Seater Leather Sofa',        6, 8400.00),
('QT00004', 'I00006', 'Bookshelf - 5 Tier',          12, 2200.00),
-- QT00005 (Pending on RMR00002)
('QT00005', 'I00003', '3-Seater Leather Sofa',        6, 8800.00),
('QT00005', 'I00006', 'Bookshelf - 5 Tier',          12, 2050.00);

-- ============================================================
--  完成 — 請重啟 IDSMS app 讓 DataStore.LoadAll() 載入新資料
-- ============================================================
