-- Create Notifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(200) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL DEFAULT 'info',
        [IsRead] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [ReadAt] datetime2 NULL,
        [RelatedEntityType] nvarchar(100) NULL,
        [RelatedEntityId] int NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId]);
    CREATE INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications] ([IsRead]);
    CREATE INDEX [IX_Notifications_CreatedAt] ON [dbo].[Notifications] ([CreatedAt]);

    PRINT 'Notifications table created successfully';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists';
END

