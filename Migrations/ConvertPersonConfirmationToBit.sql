-- Migration script to convert PersonConfirmation from nvarchar(50) to bit
-- This script will:
-- 1. Add a temporary column
-- 2. Convert existing string values to bit
-- 3. Drop old column and rename new column

-- Step 1: Add temporary bit column
ALTER TABLE WorkItem
ADD PersonConfirmation_Bit BIT NULL;

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

-- Step 3: Drop old column
ALTER TABLE WorkItem
DROP COLUMN PersonConfirmation;

-- Step 4: Rename new column to original name
EXEC sp_rename 'WorkItem.PersonConfirmation_Bit', 'PersonConfirmation', 'COLUMN';

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

