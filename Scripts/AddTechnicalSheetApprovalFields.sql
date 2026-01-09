-- Migration: AddTechnicalSheetApprovalFields
-- Description: Thêm các trường approval cho TechnicalSheet (ManagerL1 và Manager approval)

-- Add ManagerL1 approval fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalStatus')
    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalStatus] nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApproverFirebaseUID')
    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApproverFirebaseUID] nvarchar(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalDate')
    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalDate] datetime2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerL1ApprovalNotes')
    ALTER TABLE [TechnicalSheet] ADD [ManagerL1ApprovalNotes] nvarchar(1000) NULL;

-- Add Manager approval fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalStatus')
    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalStatus] nvarchar(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApproverFirebaseUID')
    ALTER TABLE [TechnicalSheet] ADD [ManagerApproverFirebaseUID] nvarchar(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalDate')
    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalDate] datetime2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TechnicalSheet]') AND name = 'ManagerApprovalNotes')
    ALTER TABLE [TechnicalSheet] ADD [ManagerApprovalNotes] nvarchar(1000) NULL;

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheet_ManagerL1ApprovalStatus' AND object_id = OBJECT_ID('TechnicalSheet'))
    CREATE INDEX [IX_TechnicalSheet_ManagerL1ApprovalStatus] ON [TechnicalSheet] ([ManagerL1ApprovalStatus]);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TechnicalSheet_ManagerApprovalStatus' AND object_id = OBJECT_ID('TechnicalSheet'))
    CREATE INDEX [IX_TechnicalSheet_ManagerApprovalStatus] ON [TechnicalSheet] ([ManagerApprovalStatus]);

-- Add migration record to __EFMigrationsHistory
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20250101000000_AddTechnicalSheetApprovalFields')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20250101000000_AddTechnicalSheetApprovalFields', '9.0.0');

PRINT 'Migration AddTechnicalSheetApprovalFields completed successfully!';
