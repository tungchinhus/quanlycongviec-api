-- Add RequestDocument column to MachineAssignment table
-- ĐĐH/Giấy đề nghị

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') 
    AND name = 'RequestDocument'
)
BEGIN
    ALTER TABLE [dbo].[MachineAssignment]
    ADD [RequestDocument] NVARCHAR(255) NULL;
    
    PRINT 'Added RequestDocument column to MachineAssignment table';
END
ELSE
BEGIN
    PRINT 'RequestDocument column already exists in MachineAssignment table';
END
