IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'XsiBookkeeping')
BEGIN
    CREATE DATABASE XsiBookkeeping;
END
GO

USE XsiBookkeeping;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Companies')
BEGIN
    CREATE TABLE dbo.Companies (
        CompanyId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Country CHAR(2) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_Companies_SortOrder DEFAULT (0),
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Companies_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Accounts')
BEGIN
    CREATE TABLE dbo.Accounts (
        AccountId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId BIGINT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_Accounts_SortOrder DEFAULT (0),
        CONSTRAINT FK_Accounts_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Accounts_CompanyId ON dbo.Accounts(CompanyId, SortOrder);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Completions')
BEGIN
    CREATE TABLE dbo.Completions (
        CompletionId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId BIGINT NOT NULL,
        AccountId BIGINT NOT NULL,
        MonthKey CHAR(7) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Completions_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedByUser NVARCHAR(256) NULL,
        CONSTRAINT FK_Completions_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId) ON DELETE CASCADE,
        CONSTRAINT FK_Completions_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountId) ON DELETE NO ACTION,
        CONSTRAINT UQ_Completions_Company_Account_Month UNIQUE (CompanyId, AccountId, MonthKey),
        CONSTRAINT CK_Completions_Status CHECK (Status IN (N'none', N'in-progress', N'done'))
    );

    CREATE INDEX IX_Completions_MonthKey ON dbo.Completions(MonthKey);
    CREATE INDEX IX_Completions_Company_Month ON dbo.Completions(CompanyId, MonthKey);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Comments')
BEGIN
    CREATE TABLE dbo.Comments (
        CommentId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId BIGINT NOT NULL,
        Author NVARCHAR(256) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Comments_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Comments_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Comments_CompanyId ON dbo.Comments(CompanyId, CreatedAtUtc);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'OverdueReasons')
BEGIN
    CREATE TABLE dbo.OverdueReasons (
        OverdueReasonId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId BIGINT NOT NULL,
        Period CHAR(7) NOT NULL,
        Reason NVARCHAR(MAX) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_OverdueReasons_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedByUser NVARCHAR(256) NULL,
        CONSTRAINT FK_OverdueReasons_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId) ON DELETE CASCADE,
        CONSTRAINT UQ_OverdueReasons_Company_Period UNIQUE (CompanyId, Period)
    );

    CREATE INDEX IX_OverdueReasons_Period ON dbo.OverdueReasons(Period);
END
GO
