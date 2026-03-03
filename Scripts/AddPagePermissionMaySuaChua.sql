-- Thêm phân quyền cho trang Máy Sửa Chữa vào bảng PagePermissions
-- Chạy trên database. Có thể chạy nhiều lần (idempotent).

IF NOT EXISTS (SELECT 1 FROM [dbo].[PagePermissions] WHERE [PageRoute] = '/may-sua-chua')
BEGIN
    INSERT INTO [dbo].[PagePermissions] ([PageRoute], [PageName], [Description], [IsActive], [CreatedAt])
    VALUES ('/may-sua-chua', N'Máy Sửa Chữa', N'Quản lý ghi nhận TNTT máy sửa chữa theo năm', 1, GETUTCDATE());
    PRINT N'Đã thêm page permission: Máy Sửa Chữa';
END
ELSE
    PRINT N'Page /may-sua-chua đã tồn tại.';
GO
