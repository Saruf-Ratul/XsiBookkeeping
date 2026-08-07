USE XsiBookkeeping;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AccountAssignments')
BEGIN
    CREATE TABLE dbo.AccountAssignments (
        AccountAssignmentId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AccountId BIGINT NOT NULL,
        AppUserId BIGINT NOT NULL,
        AssignedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_AccountAssignments_AssignedAtUtc DEFAULT (SYSUTCDATETIME()),
        AssignedByLogin NVARCHAR(256) NULL,
        CONSTRAINT FK_AccountAssignments_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(AccountId) ON DELETE CASCADE,
        CONSTRAINT FK_AccountAssignments_AppUsers FOREIGN KEY (AppUserId) REFERENCES dbo.AppUsers(AppUserId) ON DELETE CASCADE,
        CONSTRAINT UQ_AccountAssignments_AccountUser UNIQUE (AccountId, AppUserId)
    );

    CREATE INDEX IX_AccountAssignments_AppUserId ON dbo.AccountAssignments(AppUserId);
    CREATE INDEX IX_AccountAssignments_AccountId ON dbo.AccountAssignments(AccountId);
END
GO

-- Copy existing company-level assignments to every task under that company
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AccountAssignments')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CompanyAssignments')
BEGIN
    INSERT INTO dbo.AccountAssignments (AccountId, AppUserId, AssignedByLogin)
    SELECT a.AccountId, ca.AppUserId, ca.AssignedByLogin
    FROM dbo.CompanyAssignments ca
    INNER JOIN dbo.Accounts a ON a.CompanyId = ca.CompanyId
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AccountAssignments aa
        WHERE aa.AccountId = a.AccountId AND aa.AppUserId = ca.AppUserId
    );
END
GO
