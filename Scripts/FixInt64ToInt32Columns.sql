-- Script to fix Int64 (bigint) to Int32 (int) casting issues
-- Run this script if you're getting "Unable to cast object of type 'System.Int64' to type 'System.Int32'" errors
-- This script ensures ALL ID columns and status columns are int type

-- For SQL Server:
-- ============================================
-- FILES TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Files' 
           AND COLUMN_NAME = 'Id' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Files WHERE Id > 2147483647)
    BEGIN
        ALTER TABLE Files ALTER COLUMN Id int NOT NULL;
        PRINT 'Changed Files.Id from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Files.Id - values exceed int range';
    END
END

-- ============================================
-- FOLDERS TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Folders' 
           AND COLUMN_NAME = 'Id' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Folders WHERE Id > 2147483647)
    BEGIN
        ALTER TABLE Folders ALTER COLUMN Id int NOT NULL;
        PRINT 'Changed Folders.Id from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Folders.Id - values exceed int range';
    END
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Folders' 
           AND COLUMN_NAME = 'ParentFolderId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Folders WHERE ParentFolderId IS NOT NULL AND ParentFolderId > 2147483647)
    BEGIN
        ALTER TABLE Folders ALTER COLUMN ParentFolderId int NULL;
        PRINT 'Changed Folders.ParentFolderId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Folders.ParentFolderId - values exceed int range';
    END
END

-- ============================================
-- USERS TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Users' 
           AND COLUMN_NAME = 'UserId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Users WHERE UserId > 2147483647)
    BEGIN
        ALTER TABLE Users ALTER COLUMN UserId int NOT NULL;
        PRINT 'Changed Users.UserId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Users.UserId - values exceed int range';
    END
END

-- ============================================
-- ROLES TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Roles' 
           AND COLUMN_NAME = 'RoleId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Roles WHERE RoleId > 2147483647)
    BEGIN
        ALTER TABLE Roles ALTER COLUMN RoleId int NOT NULL;
        PRINT 'Changed Roles.RoleId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Roles.RoleId - values exceed int range';
    END
END

-- ============================================
-- PERMISSIONS TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Permissions' 
           AND COLUMN_NAME = 'PermissionId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Permissions WHERE PermissionId > 2147483647)
    BEGIN
        ALTER TABLE Permissions ALTER COLUMN PermissionId int NOT NULL;
        PRINT 'Changed Permissions.PermissionId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Permissions.PermissionId - values exceed int range';
    END
END

-- ============================================
-- USERROLES TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'UserRoles' 
           AND COLUMN_NAME = 'UserId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM UserRoles WHERE UserId > 2147483647)
    BEGIN
        ALTER TABLE UserRoles ALTER COLUMN UserId int NOT NULL;
        PRINT 'Changed UserRoles.UserId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert UserRoles.UserId - values exceed int range';
    END
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'UserRoles' 
           AND COLUMN_NAME = 'RoleId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM UserRoles WHERE RoleId > 2147483647)
    BEGIN
        ALTER TABLE UserRoles ALTER COLUMN RoleId int NOT NULL;
        PRINT 'Changed UserRoles.RoleId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert UserRoles.RoleId - values exceed int range';
    END
END

-- ============================================
-- ROLEPERMISSIONS TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'RolePermissions' 
           AND COLUMN_NAME = 'RoleId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM RolePermissions WHERE RoleId > 2147483647)
    BEGIN
        ALTER TABLE RolePermissions ALTER COLUMN RoleId int NOT NULL;
        PRINT 'Changed RolePermissions.RoleId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert RolePermissions.RoleId - values exceed int range';
    END
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'RolePermissions' 
           AND COLUMN_NAME = 'PermissionId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM RolePermissions WHERE PermissionId > 2147483647)
    BEGIN
        ALTER TABLE RolePermissions ALTER COLUMN PermissionId int NOT NULL;
        PRINT 'Changed RolePermissions.PermissionId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert RolePermissions.PermissionId - values exceed int range';
    END
END

-- ============================================
-- SETTINGS TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Settings' 
           AND COLUMN_NAME = 'SettingId' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM Settings WHERE SettingId > 2147483647)
    BEGIN
        ALTER TABLE Settings ALTER COLUMN SettingId int NOT NULL;
        PRINT 'Changed Settings.SettingId from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert Settings.SettingId - values exceed int range';
    END
END

-- ============================================
-- TECHNICALNOTIFICATION TABLE
-- ============================================
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'TechnicalNotification' 
           AND COLUMN_NAME = 'NotificationID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM TechnicalNotification WHERE NotificationID > 2147483647)
    BEGIN
        ALTER TABLE TechnicalNotification ALTER COLUMN NotificationID int NOT NULL;
        PRINT 'Changed TechnicalNotification.NotificationID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert TechnicalNotification.NotificationID - values exceed int range';
    END
END

-- ============================================
-- MACHINEASSIGNMENT TABLE
-- ============================================
-- Change AssignmentID columns from bigint to int (if they exist as bigint)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'MachineAssignment' 
           AND COLUMN_NAME = 'AssignmentID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    -- First, ensure no data exceeds int range
    IF NOT EXISTS (SELECT * FROM MachineAssignment WHERE AssignmentID > 2147483647)
    BEGIN
        ALTER TABLE MachineAssignment ALTER COLUMN AssignmentID int NOT NULL;
        PRINT 'Changed MachineAssignment.AssignmentID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert AssignmentID - values exceed int range';
    END
END

-- Change ApprovalID columns
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'AssignmentApproval' 
           AND COLUMN_NAME = 'ApprovalID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM AssignmentApproval WHERE ApprovalID > 2147483647)
    BEGIN
        ALTER TABLE AssignmentApproval ALTER COLUMN ApprovalID int NOT NULL;
        PRINT 'Changed AssignmentApproval.ApprovalID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert ApprovalID - values exceed int range';
    END
END

-- Change AssignmentID foreign key in AssignmentApproval
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'AssignmentApproval' 
           AND COLUMN_NAME = 'AssignmentID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM AssignmentApproval WHERE AssignmentID > 2147483647)
    BEGIN
        ALTER TABLE AssignmentApproval ALTER COLUMN AssignmentID int NOT NULL;
        PRINT 'Changed AssignmentApproval.AssignmentID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert AssignmentApproval.AssignmentID - values exceed int range';
    END
END

-- Change ChangeID columns
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'WorkChange' 
           AND COLUMN_NAME = 'ChangeID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM WorkChange WHERE ChangeID > 2147483647)
    BEGIN
        ALTER TABLE WorkChange ALTER COLUMN ChangeID int NOT NULL;
        PRINT 'Changed WorkChange.ChangeID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert ChangeID - values exceed int range';
    END
END

-- Change AssignmentID foreign key in WorkChange
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'WorkChange' 
           AND COLUMN_NAME = 'AssignmentID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM WorkChange WHERE AssignmentID > 2147483647)
    BEGIN
        ALTER TABLE WorkChange ALTER COLUMN AssignmentID int NOT NULL;
        PRINT 'Changed WorkChange.AssignmentID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert WorkChange.AssignmentID - values exceed int range';
    END
END

-- Change WorkItemID columns
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'WorkItem' 
           AND COLUMN_NAME = 'WorkItemID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM WorkItem WHERE WorkItemID > 2147483647)
    BEGIN
        ALTER TABLE WorkItem ALTER COLUMN WorkItemID int NOT NULL;
        PRINT 'Changed WorkItem.WorkItemID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert WorkItemID - values exceed int range';
    END
END

-- Change AssignmentID foreign key in WorkItem
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'WorkItem' 
           AND COLUMN_NAME = 'AssignmentID' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM WorkItem WHERE AssignmentID > 2147483647)
    BEGIN
        ALTER TABLE WorkItem ALTER COLUMN AssignmentID int NOT NULL;
        PRINT 'Changed WorkItem.AssignmentID from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert WorkItem.AssignmentID - values exceed int range';
    END
END

-- Change Status column if it's bigint
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'MachineAssignment' 
           AND COLUMN_NAME = 'status' 
           AND DATA_TYPE = 'bigint')
BEGIN
    IF NOT EXISTS (SELECT * FROM MachineAssignment WHERE status > 2147483647)
    BEGIN
        ALTER TABLE MachineAssignment ALTER COLUMN status int NOT NULL;
        PRINT 'Changed MachineAssignment.status from bigint to int';
    END
    ELSE
    BEGIN
        PRINT 'ERROR: Cannot convert status - values exceed int range';
    END
END

PRINT 'Script completed. Please check for any errors above.';

