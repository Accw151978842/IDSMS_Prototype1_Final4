-- =============================================================================
-- IDSMS - Reset Demo Accounts
-- =============================================================================
-- Purpose: Restore the 5 demo accounts (admin / sales / logistics / warehouse /
--          service) when one or more of them have been deleted, deactivated,
--          or had their password changed.
--
-- How to run:
--   1. Open phpMyAdmin (or MySQL Workbench / mysql CLI)
--   2. Select the IDSMS database
--   3. Open the SQL tab, paste the contents of this file, click "Go"
--   4. Restart IDSMS - all 5 demo accounts should now work
--
-- Demo credentials (after running this script):
--   admin     / admin123    (Administrator)
--   sales     / sales123    (Sales)
--   logistics / log123      (Logistics)
--   warehouse / ware123     (Warehouse)
--   service   / svc123      (Service)
--
-- SHA-256 hashes match the application's SecurityService.Hash() output.
-- =============================================================================

USE IDSMS;

-- ---------------------------------------------------------------------------
-- Step 1. Inspect current state (optional - run this first to see what's wrong)
-- ---------------------------------------------------------------------------
SELECT user_id, username, role, active FROM users ORDER BY user_id;

-- ---------------------------------------------------------------------------
-- Step 2. Restore / overwrite the 5 demo accounts
--   REPLACE INTO = DELETE existing row with same PK then INSERT.
--   Safe to run multiple times. Will NOT touch other (non-demo) users.
-- ---------------------------------------------------------------------------
REPLACE INTO users (user_id, username, password_hash, full_name, role, active) VALUES
 ('U00001', 'admin',     '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'System Administrator',  'Administrator', 1),
 ('U00002', 'sales',     '6bc0a63cb29c92306020c0a6bbc358cc4628db277dc06e253535e126517ad637', 'Sales Officer',         'Sales',         1),
 ('U00003', 'logistics', '77e9d03d2fa3fa2f24c1a02710b3566e337340e1ced5217bf863d8ea37f4658f', 'Logistics Officer',     'Logistics',     1),
 ('U00004', 'warehouse', 'b9682c7a000340c73a1d9165b3793a3f9bd39f4c1f1eb091cb3c769d488e71bc', 'Warehouse Officer',     'Warehouse',     1),
 ('U00005', 'service',   '45cdd0d48c1d78bd22f1d02f3267003590841a8999db837736bcbd292991b62d', 'After-Service Officer', 'Service',       1);

-- ---------------------------------------------------------------------------
-- Step 3. Verify - all 5 should appear with active = 1
-- ---------------------------------------------------------------------------
SELECT user_id, username, role, active FROM users ORDER BY user_id;
