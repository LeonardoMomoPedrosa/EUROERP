-- Epic 18 Story 18.2: User dashboard shortcuts (quick links).
-- Run once per database. Stores which menu routes each user has pinned as shortcuts on the home dashboard.

IF OBJECT_ID('dbo.USER_SHORTCUT', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[USER_SHORTCUT](
        [UserId] [uniqueidentifier] NOT NULL,
        [Route] [nvarchar](256) NOT NULL,
        [SortOrder] [int] NOT NULL,
        CONSTRAINT [PK_USER_SHORTCUT] PRIMARY KEY CLUSTERED ([UserId] ASC, [Route] ASC),
        CONSTRAINT [FK_USER_SHORTCUT_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[aspnet_Users] ([UserId])
    );
END
GO
