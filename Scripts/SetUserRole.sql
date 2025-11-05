-- Script để set role cho user theo email
-- Usage: Chạy script này trong SQL Server Management Studio hoặc sqlcmd

DECLARE @Email NVARCHAR(200) = 'chinhdvt@gmail.com';
DECLARE @RoleName NVARCHAR(100) = 'Admin'; -- Có thể thay đổi thành 'User', 'Manager', etc.

-- Tìm user và role
DECLARE @UserId INT;
DECLARE @RoleId INT;

SELECT @UserId = UserId 
FROM Users 
WHERE Email = @Email;

IF @UserId IS NULL
BEGIN
    PRINT '❌ Không tìm thấy user với email: ' + @Email;
    RETURN;
END

SELECT @RoleId = RoleId 
FROM Roles 
WHERE RoleName = @RoleName;

IF @RoleId IS NULL
BEGIN
    PRINT '❌ Không tìm thấy role: ' + @RoleName;
    PRINT 'Available roles:';
    SELECT RoleName FROM Roles;
    RETURN;
END

-- Kiểm tra xem user đã có role này chưa
IF EXISTS (SELECT 1 FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
BEGIN
    PRINT 'ℹ️  User đã có role ' + @RoleName + ' rồi!';
    RETURN;
END

-- Thêm role cho user
INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
VALUES (@UserId, @RoleId, GETUTCDATE());

PRINT '✅ Đã set role ''' + @RoleName + ''' cho user ' + @Email + ' thành công!';

-- Hiển thị thông tin user và roles sau khi update
SELECT 
    u.UserId,
    u.UserName,
    u.FullName,
    u.Email,
    u.FirebaseUID,
    STRING_AGG(r.RoleName, ', ') AS Roles
FROM Users u
LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN Roles r ON ur.RoleId = r.RoleId
WHERE u.UserId = @UserId
GROUP BY u.UserId, u.UserName, u.FullName, u.Email, u.FirebaseUID;

