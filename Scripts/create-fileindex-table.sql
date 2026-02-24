-- Script tạo bảng FileIndex cho Tra Cứu Files (indexer Python)
-- Chạy script này trên database quanlyphancong nếu migration chưa được apply

USE quanlyphancong;
GO

-- Kiểm tra và tạo bảng FileIndex nếu chưa tồn tại
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileIndex')
BEGIN
    CREATE TABLE [dbo].[FileIndex] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [FullPath] nvarchar(2000) NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [FolderPath] nvarchar(2000) NOT NULL,
        [Ext] nvarchar(50) NOT NULL,
        [NameNormalized] nvarchar(1000) NULL,
        [Mtime] float NOT NULL,
        CONSTRAINT [PK_FileIndex] PRIMARY KEY ([Id])
    );
    
    -- Tạo các index để tăng tốc độ tìm kiếm
    CREATE INDEX [IX_FileIndex_FolderPath] ON [dbo].[FileIndex] ([FolderPath]);
    CREATE INDEX [IX_FileIndex_NameNormalized] ON [dbo].[FileIndex] ([NameNormalized]);
    CREATE INDEX [IX_FileIndex_Ext] ON [dbo].[FileIndex] ([Ext]);
    
    PRINT 'Bảng FileIndex đã được tạo thành công!';
END
ELSE
BEGIN
    PRINT 'Bảng FileIndex đã tồn tại.';
END
GO
