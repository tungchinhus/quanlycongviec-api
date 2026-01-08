-- Script để cập nhật tên user theo email để đồng bộ với Firebase Authentication custom claims
-- Usage: Chạy script này trong SQL Server Management Studio hoặc sqlcmd

-- Cập nhật tên cho tung.lm@thibidi.com
DECLARE @Email1 NVARCHAR(200) = 'tung.lm@thibidi.com';
DECLARE @FullName1 NVARCHAR(150) = 'Lê Minh Tùng';
DECLARE @UserId1 INT;

SELECT @UserId1 = UserId 
FROM Users 
WHERE Email = @Email1;

IF @UserId1 IS NULL
BEGIN
    PRINT '❌ Không tìm thấy user với email: ' + @Email1;
END
ELSE
BEGIN
    UPDATE Users 
    SET FullName = @FullName1
    WHERE UserId = @UserId1;
    
    PRINT '✅ Đã cập nhật tên cho ' + @Email1 + ' thành: ' + @FullName1;
    
    -- Hiển thị thông tin user sau khi update
    SELECT 
        UserId,
        UserName,
        FullName,
        Email,
        FirebaseUID,
        IsActive
    FROM Users
    WHERE UserId = @UserId1;
END

PRINT '';

-- Cập nhật tên cho hoa.dc@thibidi.com
DECLARE @Email2 NVARCHAR(200) = 'hoa.dc@thibidi.com';
DECLARE @FullName2 NVARCHAR(150) = 'Dương Công Hòa';
DECLARE @UserId2 INT;

SELECT @UserId2 = UserId 
FROM Users 
WHERE Email = @Email2;

IF @UserId2 IS NULL
BEGIN
    PRINT '❌ Không tìm thấy user với email: ' + @Email2;
END
ELSE
BEGIN
    UPDATE Users 
    SET FullName = @FullName2
    WHERE UserId = @UserId2;
    
    PRINT '✅ Đã cập nhật tên cho ' + @Email2 + ' thành: ' + @FullName2;
    
    -- Hiển thị thông tin user sau khi update
    SELECT 
        UserId,
        UserName,
        FullName,
        Email,
        FirebaseUID,
        IsActive
    FROM Users
    WHERE UserId = @UserId2;
END

PRINT '';
PRINT '📋 Tóm tắt:';
PRINT 'Đã cập nhật tên cho 2 user để đồng bộ với Firebase Authentication custom claims';
