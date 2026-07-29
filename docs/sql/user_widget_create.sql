-- Epic 18 Story 18.2: User dashboard widget preferences.
-- Run once per database. Stores which widgets each user has enabled on the home dashboard.

IF OBJECT_ID('dbo.USER_WIDGET', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[USER_WIDGET](
        [UserId] [uniqueidentifier] NOT NULL,
        [WidgetCode] [nvarchar](64) NOT NULL,
        CONSTRAINT [PK_USER_WIDGET] PRIMARY KEY CLUSTERED ([UserId] ASC, [WidgetCode] ASC),
        CONSTRAINT [FK_USER_WIDGET_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[aspnet_Users] ([UserId])
    );
END
GO
