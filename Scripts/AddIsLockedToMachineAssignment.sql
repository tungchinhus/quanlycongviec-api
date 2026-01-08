-- Add IsLocked column to MachineAssignment table
-- This column is used to lock assignments after user thiết kế confirms completion (PersonConfirmation = true)
-- User kiểm soát (Manager role) can unlock assignments to allow user thiết kế to update workitems

IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[MachineAssignment]') 
    AND name = 'IsLocked'
)
BEGIN
    ALTER TABLE [dbo].[MachineAssignment]
    ADD [IsLocked] BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added IsLocked column to MachineAssignment table';
END
ELSE
BEGIN
    PRINT 'IsLocked column already exists in MachineAssignment table';
END
GO


