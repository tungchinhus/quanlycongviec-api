-- Migration script to convert PersonConfirmation from nvarchar(50) to bit
-- This script will:
-- 1. Add a temporary column
-- 2. Convert existing string values to bit
-- 3. Drop old column and rename new column

-- Check if column already exists and is already bit type
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkItem' 
    AND COLUMN_NAME = 'PersonConfirmation'
    AND DATA_TYPE = 'bit'
)
BEGIN
    PRINT 'PersonConfirmation column is already BIT type. No migration needed.';
    RETURN;
END

-- Check if column exists as nvarchar
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkItem' 
    AND COLUMN_NAME = 'PersonConfirmation'
)
BEGIN
    PRINT 'PersonConfirmation column does not exist. Creating as BIT.';
    ALTER TABLE WorkItem
    ADD PersonConfirmation BIT NULL;
    RETURN;
END

PRINT 'Starting migration: Converting PersonConfirmation from NVARCHAR to BIT...';

-- Step 1: Add temporary bit column
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WorkItem' 
    AND COLUMN_NAME = 'PersonConfirmation_Bit'
)
BEGIN
    ALTER TABLE WorkItem
    ADD PersonConfirmation_Bit BIT NULL;
    PRINT 'Step 1: Added temporary PersonConfirmation_Bit column.';
END

-- Step 2: Convert existing string values to bit
-- Convert "1", "true", "yes" -> 1 (true)
-- Convert "0", "false", "no" -> 0 (false)
-- NULL or empty -> NULL
UPDATE WorkItem
SET PersonConfirmation_Bit = 
    CASE 
        WHEN PersonConfirmation IS NULL OR LTRIM(RTRIM(PersonConfirmation)) = '' THEN NULL
        WHEN LOWER(LTRIM(RTRIM(PersonConfirmation))) IN ('1', 'true', 'yes', 'đúng', 'có') THEN 1
        WHEN LOWER(LTRIM(RTRIM(PersonConfirmation))) IN ('0', 'false', 'no', 'sai', 'không') THEN 0
        ELSE NULL
    END;

PRINT 'Step 2: Converted string values to bit.';

-- Step 3: Drop old column
ALTER TABLE WorkItem
DROP COLUMN PersonConfirmation;

PRINT 'Step 3: Dropped old PersonConfirmation column.';

-- Step 4: Rename new column to original name
EXEC sp_rename 'WorkItem.PersonConfirmation_Bit', 'PersonConfirmation', 'COLUMN';

PRINT 'Step 4: Renamed PersonConfirmation_Bit to PersonConfirmation.';
PRINT 'Migration completed successfully!';

-- Step 5: Verify the conversion
SELECT 
    WorkItemID,
    PersonConfirmation,
    CASE 
        WHEN PersonConfirmation IS NULL THEN 'NULL'
        WHEN PersonConfirmation = 1 THEN 'TRUE'
        WHEN PersonConfirmation = 0 THEN 'FALSE'
    END AS PersonConfirmation_Display
FROM WorkItem
ORDER BY WorkItemID;

