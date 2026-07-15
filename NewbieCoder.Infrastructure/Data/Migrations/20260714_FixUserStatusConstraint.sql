-- Fix: Update ck_users_status check constraint to include 'LOCKED' status
-- Date: 2026-07-14
-- Purpose: Fixes the constraint that was missing 'LOCKED' after AddUserLockoutFields migration
-- ============================================================

BEGIN;

-- Drop the old constraint and recreate with LOCKED included
ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;

ALTER TABLE users ADD CONSTRAINT ck_users_status
    CHECK (status IN ('ACT','INACT','BAN','CLS','LOCKED'));

COMMIT;

-- Verify the constraint was updated
SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conname = 'ck_users_status'
  AND conrelid = 'users'::regclass;
