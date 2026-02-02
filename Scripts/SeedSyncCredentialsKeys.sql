-- Thêm các key Settings cho sync client (syncToServer): credentials lưu trong DB, không lưu trong appsettings.
-- Chạy script này một lần, sau đó cập nhật giá trị qua API PUT /api/settings/sync-credentials hoặc cập nhật trực tiếp trong bảng Settings.

IF NOT EXISTS (SELECT 1 FROM Settings WHERE [Key] = N'sync-network-username')
INSERT INTO Settings ([Key], Value, Description, CreatedAt, UpdatedAt)
VALUES (N'sync-network-username', N'', N'Sync client: network share username (đọc bởi syncToServer)', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Settings WHERE [Key] = N'sync-network-password')
INSERT INTO Settings ([Key], Value, Description, CreatedAt, UpdatedAt)
VALUES (N'sync-network-password', N'', N'Sync client: network share password (đọc bởi syncToServer)', GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Settings WHERE [Key] = N'sync-network-domain')
INSERT INTO Settings ([Key], Value, Description, CreatedAt, UpdatedAt)
VALUES (N'sync-network-domain', N'', N'Sync client: network share domain (tùy chọn)', GETUTCDATE(), GETUTCDATE());
