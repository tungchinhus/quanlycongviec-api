-- =============================================
-- Script tạo Database cho hệ thống Quản lý Files
-- Hệ thống lai: Firebase Authentication + SQL Server Local DB
-- =============================================

USE master;
GO

-- Tạo database nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'quanlyphancong')
BEGIN
    CREATE DATABASE quanlyphancong;
END
GO

USE quanlyphancong;
GO

-- =============================================
-- 1. Bảng Users
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [Users] (
        [UserId] INT IDENTITY(1,1) NOT NULL,
        [UserName] NVARCHAR(100) NOT NULL,
        [PasswordHash] NVARCHAR(200) NOT NULL,
        [FullName] NVARCHAR(150) NULL,
        [Email] NVARCHAR(200) NULL,
        [FirebaseUID] NVARCHAR(200) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
    );
    
    -- Index unique cho UserName
    CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
    
    -- Index unique cho FirebaseUID
    CREATE UNIQUE INDEX [IX_Users_FirebaseUID] ON [Users] ([FirebaseUID]) WHERE [FirebaseUID] IS NOT NULL;
    
    -- Index cho Email
    CREATE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL;
END
GO

-- =============================================
-- 2. Bảng Roles
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [Roles] (
        [RoleId] INT IDENTITY(1,1) NOT NULL,
        [RoleName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(250) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
    
    -- Index unique cho RoleName
    CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
END
GO

-- =============================================
-- 3. Bảng Permissions
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permissions')
BEGIN
    CREATE TABLE [Permissions] (
        [PermissionId] INT IDENTITY(1,1) NOT NULL,
        [PermissionName] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(250) NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([PermissionId])
    );
    
    -- Index unique cho PermissionName
    CREATE UNIQUE INDEX [IX_Permissions_PermissionName] ON [Permissions] ([PermissionName]);
END
GO

-- =============================================
-- 4. Bảng UserRoles (Junction Table)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] INT NOT NULL,
        [RoleId] INT NOT NULL,
        [AssignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) 
            REFERENCES [Roles] ([RoleId]) ON DELETE CASCADE
    );
    
    -- Index cho UserId
    CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);
    
    -- Index cho RoleId
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END
GO

-- =============================================
-- 5. Bảng RolePermissions (Junction Table)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RolePermissions')
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] INT NOT NULL,
        [PermissionId] INT NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Roles] FOREIGN KEY ([RoleId]) 
            REFERENCES [Roles] ([RoleId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions] FOREIGN KEY ([PermissionId]) 
            REFERENCES [Permissions] ([PermissionId]) ON DELETE CASCADE
    );
    
    -- Index cho RoleId
    CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
    
    -- Index cho PermissionId
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END
GO

-- =============================================
-- 6. Bảng Folders
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Folders')
BEGIN
    CREATE TABLE [Folders] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FolderName] NVARCHAR(255) NOT NULL,
        [FolderPath] NVARCHAR(500) NOT NULL,
        [ParentFolderId] INT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_Folders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Folders_ParentFolder] FOREIGN KEY ([ParentFolderId]) 
            REFERENCES [Folders] ([Id]) ON DELETE NO ACTION
    );
    
    -- Index cho FolderName
    CREATE INDEX [IX_Folders_FolderName] ON [Folders] ([FolderName]);
    
    -- Index cho ParentFolderId
    CREATE INDEX [IX_Folders_ParentFolderId] ON [Folders] ([ParentFolderId]);
END
GO

-- =============================================
-- 7. Bảng Files
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Files')
BEGIN
    CREATE TABLE [Files] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [FileType] NVARCHAR(100) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [UploadDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UploadedBy] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Files] PRIMARY KEY ([Id])
    );
    
    -- Index cho FileName
    CREATE INDEX [IX_Files_FileName] ON [Files] ([FileName]);
    
    -- Index cho FileType
    CREATE INDEX [IX_Files_FileType] ON [Files] ([FileType]);
END
GO

-- =============================================
-- 8. Seed dữ liệu mẫu (Roles và Permissions)
-- =============================================

-- Insert Roles nếu chưa có
IF NOT EXISTS (SELECT * FROM [Roles] WHERE [RoleName] = 'Admin')
BEGIN
    INSERT INTO [Roles] ([RoleName], [Description])
    VALUES ('Admin', 'System administrator');
END
GO

IF NOT EXISTS (SELECT * FROM [Roles] WHERE [RoleName] = 'User')
BEGIN
    INSERT INTO [Roles] ([RoleName], [Description])
    VALUES ('User', 'Standard user');
END
GO

-- Insert Permissions nếu chưa có
IF NOT EXISTS (SELECT * FROM [Permissions] WHERE [PermissionName] = 'files.view')
BEGIN
    INSERT INTO [Permissions] ([PermissionName], [Description])
    VALUES ('files.view', 'View files');
END
GO

IF NOT EXISTS (SELECT * FROM [Permissions] WHERE [PermissionName] = 'files.manage')
BEGIN
    INSERT INTO [Permissions] ([PermissionName], [Description])
    VALUES ('files.manage', 'Manage files');
END
GO

IF NOT EXISTS (SELECT * FROM [Permissions] WHERE [PermissionName] = 'users.manage')
BEGIN
    INSERT INTO [Permissions] ([PermissionName], [Description])
    VALUES ('users.manage', 'Manage users');
END
GO

-- Gán tất cả permissions cho Admin role
DECLARE @AdminRoleId INT = (SELECT [RoleId] FROM [Roles] WHERE [RoleName] = 'Admin');

IF @AdminRoleId IS NOT NULL
BEGIN
    -- Gán permission files.view
    IF NOT EXISTS (SELECT * FROM [RolePermissions] WHERE [RoleId] = @AdminRoleId AND [PermissionId] = (SELECT [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'files.view'))
    BEGIN
        INSERT INTO [RolePermissions] ([RoleId], [PermissionId])
        SELECT @AdminRoleId, [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'files.view';
    END
    
    -- Gán permission files.manage
    IF NOT EXISTS (SELECT * FROM [RolePermissions] WHERE [RoleId] = @AdminRoleId AND [PermissionId] = (SELECT [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'files.manage'))
    BEGIN
        INSERT INTO [RolePermissions] ([RoleId], [PermissionId])
        SELECT @AdminRoleId, [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'files.manage';
    END
    
    -- Gán permission users.manage
    IF NOT EXISTS (SELECT * FROM [RolePermissions] WHERE [RoleId] = @AdminRoleId AND [PermissionId] = (SELECT [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'users.manage'))
    BEGIN
        INSERT INTO [RolePermissions] ([RoleId], [PermissionId])
        SELECT @AdminRoleId, [PermissionId] FROM [Permissions] WHERE [PermissionName] = 'users.manage';
    END
END
GO

-- Note: Admin user sẽ được tạo tự động bởi DbSeeder khi ứng dụng khởi động
-- Username: admin, Password: Admin@123
GO

PRINT 'Database created successfully!';
PRINT 'Tables created: Users, Roles, Permissions, UserRoles, RolePermissions, Folders, Files';
PRINT 'Default data seeded: Admin role, User role, and sample permissions';
GO

