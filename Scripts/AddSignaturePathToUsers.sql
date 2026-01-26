-- Script to add SignaturePath column to Users table
-- This column stores the file path of the user's electronic signature

-- Check if column already exists
IF NOT EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'SignaturePath'
)
BEGIN
    -- Add SignaturePath column
    ALTER TABLE [Users]
    ADD [SignaturePath] NVARCHAR(500) NULL;
    
    PRINT 'Column SignaturePath added successfully to Users table';
END
ELSE
BEGIN
    PRINT 'Column SignaturePath already exists in Users table';
END
GO

-- Verify the column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' 
AND COLUMN_NAME = 'SignaturePath';
GO
