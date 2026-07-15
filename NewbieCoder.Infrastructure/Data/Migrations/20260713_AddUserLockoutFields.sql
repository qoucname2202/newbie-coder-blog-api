-- Migration: AddUserLockoutFields
-- Date: 2026-07-13
-- Description: Adds account lockout columns to users table and extends status CHECK constraint.
-- ============================================================

BEGIN;

-- 1. Add lockout columns
ALTER TABLE users
    ADD COLUMN IF NOT EXISTS locked_at TIMESTAMP WITH TIME ZONE NULL,
    ADD COLUMN IF NOT EXISTS locked_until TIMESTAMP WITH TIME ZONE NULL,
    ADD COLUMN IF NOT EXISTS locked_reason VARCHAR(500) NULL,
    ADD COLUMN IF NOT EXISTS locked_by BIGINT NULL;

-- 2. Add indexes for lockout management queries
CREATE INDEX IF NOT EXISTS ix_users_locked_until_active
    ON users (locked_until)
    WHERE locked_until IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_users_locked_by
    ON users (locked_by);

-- 3. Update CHECK constraint to include LOCKED status
ALTER TABLE users
    DROP CONSTRAINT IF EXISTS ck_users_status;

ALTER TABLE users
    ADD CONSTRAINT ck_users_status
        CHECK (status IN ('ACT','INACT','BAN','CLS','LOCKED'));

-- 4. Add FK for locked_by -> users.id (skip if already exists)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_users_locked_by'
          AND table_name = 'users'
    ) THEN
        ALTER TABLE users
            ADD CONSTRAINT fk_users_locked_by
                FOREIGN KEY (locked_by)
                REFERENCES users(id)
                ON DELETE NO ACTION;
    END IF;
END $$;

COMMIT;

-- ============================================================
-- Rollback (run this if you need to revert):
-- ============================================================
-- BEGIN;
-- ALTER TABLE users DROP CONSTRAINT IF EXISTS fk_users_locked_by;
-- DROP INDEX IF EXISTS ix_users_locked_by;
-- DROP INDEX IF EXISTS ix_users_locked_until_active;
-- ALTER TABLE users DROP COLUMN IF EXISTS locked_by;
-- ALTER TABLE users DROP COLUMN IF EXISTS locked_reason;
-- ALTER TABLE users DROP COLUMN IF EXISTS locked_until;
-- ALTER TABLE users DROP COLUMN IF EXISTS locked_at;
-- ALTER TABLE users DROP CONSTRAINT IF EXISTS ck_users_status;
-- ALTER TABLE users ADD CONSTRAINT ck_users_status CHECK (status IN ('ACT','INACT','BAN','CLS'));
-- COMMIT;
