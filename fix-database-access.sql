-- Script kiểm tra và sửa lỗi database không thể truy cập
-- Chạy trong SQL Server Management Studio với quyền sysadmin

USE [master]
GO

-- Kiểm tra trạng thái database
SELECT 
    name AS [Database Name],
    state_desc AS [State],
    user_access_desc AS [User Access],
    recovery_model_desc AS [Recovery Model],
    is_read_only AS [Read Only],
    is_auto_close_on AS [Auto Close]
FROM sys.databases
WHERE name = 'quanlyphancong'
GO

-- Nếu database bị OFFLINE, set lại ONLINE
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'quanlyphancong' AND state_desc = 'OFFLINE')
BEGIN
    PRINT 'Database quanlyphancong đang OFFLINE. Đang set lại ONLINE...'
    ALTER DATABASE [quanlyphancong] SET ONLINE
    PRINT 'Đã set database ONLINE.'
END
GO

-- Nếu database bị SUSPECT, thử sửa
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'quanlyphancong' AND state_desc = 'SUSPECT')
BEGIN
    PRINT 'Database quanlyphancong đang ở trạng thái SUSPECT. Đang thử sửa...'
    
    -- Thử set emergency mode
    ALTER DATABASE [quanlyphancong] SET EMERGENCY
    GO
    
    -- Thử set single user mode
    ALTER DATABASE [quanlyphancong] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
    GO
    
    -- Thử repair
    DBCC CHECKDB ([quanlyphancong], REPAIR_ALLOW_DATA_LOSS)
    GO
    
    -- Set lại multi user
    ALTER DATABASE [quanlyphancong] SET MULTI_USER
    GO
    
    -- Set lại online
    ALTER DATABASE [quanlyphancong] SET ONLINE
    GO
    
    PRINT 'Đã thử sửa database. Kiểm tra lại trạng thái.'
END
GO

-- Nếu database bị RESTRICTED_USER, set lại MULTI_USER
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'quanlyphancong' AND user_access_desc = 'RESTRICTED')
BEGIN
    PRINT 'Database quanlyphancong đang ở chế độ RESTRICTED_USER. Đang set lại MULTI_USER...'
    ALTER DATABASE [quanlyphancong] SET MULTI_USER
    PRINT 'Đã set database về MULTI_USER.'
END
GO

-- Kiểm tra lại trạng thái sau khi sửa
SELECT 
    name AS [Database Name],
    state_desc AS [State],
    user_access_desc AS [User Access],
    recovery_model_desc AS [Recovery Model],
    is_read_only AS [Read Only]
FROM sys.databases
WHERE name = 'quanlyphancong'
GO

-- Thử kết nối vào database
USE [quanlyphancong]
GO

SELECT 'Kết nối thành công vào database quanlyphancong!' AS [Status]
GO

-- Kiểm tra các bảng trong database
SELECT 
    TABLE_SCHEMA AS [Schema],
    TABLE_NAME AS [Table Name]
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME
GO

PRINT ''
PRINT '=== Hoàn tất kiểm tra ==='
PRINT 'Nếu vẫn còn lỗi, có thể cần restore database từ backup.'

