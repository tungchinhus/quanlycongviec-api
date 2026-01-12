-- Migration: CreateTechnicalSheetApprovalTable
-- Description: Tạo bảng TechnicalSheetApproval để lưu lịch sử ký duyệt TBKT
-- Date: 2026-01-XX

-- Create TechnicalSheetApproval table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheetApproval]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TechnicalSheetApproval] (
        [ApprovalID] INT IDENTITY(1,1) NOT NULL,
        [TBKT_ID] VARCHAR(50) NOT NULL,  -- Phải khớp với TechnicalSheet.TBKT_ID (VARCHAR)
        [ApprovalLevel] NVARCHAR(20) NOT NULL,  -- 'ManagerL1' hoặc 'Manager'
        [ApprovalStatus] NVARCHAR(20) NOT NULL,  -- 'Pending', 'Approved', 'Rejected'
        [ApproverFirebaseUID] NVARCHAR(200) NOT NULL,
        [ApproverName] NVARCHAR(100) NOT NULL,
        [ApprovalDate] DATETIME2 NOT NULL,
        [Notes] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [PK_TechnicalSheetApproval] PRIMARY KEY CLUSTERED ([ApprovalID] ASC),
        CONSTRAINT [FK_TechnicalSheetApproval_TechnicalSheet] 
            FOREIGN KEY ([TBKT_ID]) REFERENCES [dbo].[TechnicalSheet]([TBKT_ID])
            ON DELETE CASCADE
    );
    
    PRINT 'Table TechnicalSheetApproval created successfully!';
END
ELSE
BEGIN
    PRINT 'Table TechnicalSheetApproval already exists.';
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheetApproval_TBKT_ID' AND object_id = OBJECT_ID('TechnicalSheetApproval'))
BEGIN
    CREATE INDEX [IX_TechnicalSheetApproval_TBKT_ID] 
        ON [dbo].[TechnicalSheetApproval]([TBKT_ID]);
    PRINT 'Index IX_TechnicalSheetApproval_TBKT_ID created successfully!';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheetApproval_ApprovalLevel' AND object_id = OBJECT_ID('TechnicalSheetApproval'))
BEGIN
    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalLevel] 
        ON [dbo].[TechnicalSheetApproval]([ApprovalLevel]);
    PRINT 'Index IX_TechnicalSheetApproval_ApprovalLevel created successfully!';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheetApproval_ApprovalStatus' AND object_id = OBJECT_ID('TechnicalSheetApproval'))
BEGIN
    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalStatus] 
        ON [dbo].[TechnicalSheetApproval]([ApprovalStatus]);
    PRINT 'Index IX_TechnicalSheetApproval_ApprovalStatus created successfully!';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheetApproval_ApprovalDate' AND object_id = OBJECT_ID('TechnicalSheetApproval'))
BEGIN
    CREATE INDEX [IX_TechnicalSheetApproval_ApprovalDate] 
        ON [dbo].[TechnicalSheetApproval]([ApprovalDate]);
    PRINT 'Index IX_TechnicalSheetApproval_ApprovalDate created successfully!';
END
GO

PRINT 'Migration CreateTechnicalSheetApprovalTable completed successfully!';
