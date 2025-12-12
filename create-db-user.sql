-- Script tạo SQL User mới cho ứng dụng quanlyfilesBE
-- Chạy script này trong SQL Server Management Studio với quyền sysadmin

USE [master]
GO

-- Tạo Login mới (thay đổi username và password theo nhu cầu)
CREATE LOGIN [quanlyfiles_user] WITH PASSWORD = 'QuanLyFiles@2024', 
    DEFAULT_DATABASE = [quanlyphancong],
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = ON
GO

-- Cấp quyền server roles
ALTER SERVER ROLE [dbcreator] ADD MEMBER [quanlyfiles_user]
GO

-- Chuyển sang database quanlyphancong
USE [quanlyphancong]
GO

-- Tạo User trong database
CREATE USER [quanlyfiles_user] FOR LOGIN [quanlyfiles_user]
GO

-- Cấp quyền db_owner cho user
ALTER ROLE [db_owner] ADD MEMBER [quanlyfiles_user]
GO

-- Kiểm tra user đã được tạo
SELECT 
    name AS [User Name],
    type_desc AS [Type],
    default_schema_name AS [Default Schema]
FROM sys.database_principals
WHERE name = 'quanlyfiles_user'
GO

PRINT 'User quanlyfiles_user đã được tạo thành công!'
PRINT 'Username: quanlyfiles_user'
PRINT 'Password: QuanLyFiles@2024'
PRINT ''
PRINT 'Hãy cập nhật connection string trong appsettings.Development.json với thông tin trên.'

