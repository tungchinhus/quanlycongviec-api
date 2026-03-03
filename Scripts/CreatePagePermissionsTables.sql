-- Create PagePermissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PagePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PagePermissions] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [PageRoute] NVARCHAR(200) NOT NULL UNIQUE,
        [PageName] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_PagePermissions_PageRoute ON [dbo].[PagePermissions]([PageRoute]);
    PRINT 'Created PagePermissions table';
END
ELSE
BEGIN
    PRINT 'PagePermissions table already exists';
END
GO

-- Create UserPagePermissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserPagePermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserPagePermissions] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] INT NOT NULL,
        [PagePermissionId] INT NOT NULL,
        [CanView] BIT NOT NULL DEFAULT 1,
        [CanCreate] BIT NOT NULL DEFAULT 0,
        [CanEdit] BIT NOT NULL DEFAULT 0,
        [CanDelete] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        CONSTRAINT FK_UserPagePermissions_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
        CONSTRAINT FK_UserPagePermissions_PagePermission FOREIGN KEY ([PagePermissionId]) REFERENCES [dbo].[PagePermissions]([Id]) ON DELETE CASCADE,
        CONSTRAINT UQ_UserPagePermissions_User_Page UNIQUE ([UserId], [PagePermissionId])
    );
    
    CREATE INDEX IX_UserPagePermissions_UserId ON [dbo].[UserPagePermissions]([UserId]);
    CREATE INDEX IX_UserPagePermissions_PagePermissionId ON [dbo].[UserPagePermissions]([PagePermissionId]);
    PRINT 'Created UserPagePermissions table';
END
ELSE
BEGIN
    PRINT 'UserPagePermissions table already exists';
END
GO

-- Seed initial page permissions
IF NOT EXISTS (SELECT * FROM [dbo].[PagePermissions] WHERE [PageRoute] = '/dashboard')
BEGIN
    INSERT INTO [dbo].[PagePermissions] ([PageRoute], [PageName], [Description], [IsActive])
    VALUES 
        ('/dashboard', 'Dashboard', 'Trang tổng quan', 1),
        ('/files', 'Quản lý File', 'Quản lý và upload files', 1),
        ('/assignments', 'Giao Việc', 'Danh sách giao việc', 1),
        ('/assignments/new', 'Tạo Giao Việc', 'Tạo giao việc mới', 1),
        ('/assignments/:id', 'Chi Tiết Giao Việc', 'Xem chi tiết giao việc', 1),
        ('/approvals', 'Ký Duyệt', 'Danh sách ký duyệt', 1),
        ('/approval-workflow', 'Quy Trình Ký Duyệt', 'Quản lý quy trình ký duyệt', 1),
        ('/work-items', 'Công Việc', 'Danh sách công việc', 1),
        ('/users', 'Quản lý User', 'Quản lý người dùng', 1),
        ('/roles', 'Quản lý Roles', 'Quản lý vai trò', 1),
        ('/settings', 'Cài đặt', 'Cài đặt hệ thống', 1),
        ('/tbkt-list', 'Danh Sách TBKT', 'Tra cứu thông số MBA', 1),
        ('/tbkt-management', 'Quản Lý Đề Nghị TBKT', 'Quản lý đề nghị TBKT', 1),
        ('/tiep-nhan-thong-tin', 'Tiếp Nhận Thông Tin', 'Quản lý tiếp nhận thông tin', 1),
        ('/ho-so-thau', 'Hồ Sơ Thầu', 'Quản lý hồ sơ thầu', 1),
        ('/tsmay', 'TS May', 'Quản lý TS May', 1),
        ('/excel-reader', 'Excel Reader', 'Đọc file Excel', 1);
    
    PRINT 'Seeded initial page permissions';
END
ELSE
BEGIN
    PRINT 'Page permissions already seeded';
END
GO

PRINT 'Page permissions setup completed';

