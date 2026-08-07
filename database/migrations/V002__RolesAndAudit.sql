USE XsiBookkeeping;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppUsers')
BEGIN
    CREATE TABLE dbo.AppUsers (
        AppUserId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        WindowsLogin NVARCHAR(256) NOT NULL,
        DisplayName NVARCHAR(200) NULL,
        Role NVARCHAR(20) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT (1),
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_AppUsers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        ModifiedAtUtc DATETIME2(3) NULL,
        CONSTRAINT UQ_AppUsers_WindowsLogin UNIQUE (WindowsLogin),
        CONSTRAINT CK_AppUsers_Role CHECK (Role IN (N'User', N'Admin', N'Sysadmin'))
    );

    CREATE INDEX IX_AppUsers_Role ON dbo.AppUsers(Role, IsActive);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AuditLogs')
BEGIN
    CREATE TABLE dbo.AuditLogs (
        AuditLogId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ActorLogin NVARCHAR(256) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityType NVARCHAR(50) NULL,
        EntityId NVARCHAR(50) NULL,
        Details NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_AuditLogs_CreatedAtUtc ON dbo.AuditLogs(CreatedAtUtc DESC);
    CREATE INDEX IX_AuditLogs_ActorLogin ON dbo.AuditLogs(ActorLogin, CreatedAtUtc DESC);
END
GO
