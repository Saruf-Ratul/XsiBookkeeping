USE XsiBookkeeping;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CompanyAssignments')
BEGIN
    CREATE TABLE dbo.CompanyAssignments (
        CompanyAssignmentId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyId BIGINT NOT NULL,
        AppUserId BIGINT NOT NULL,
        AssignedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyAssignments_AssignedAtUtc DEFAULT (SYSUTCDATETIME()),
        AssignedByLogin NVARCHAR(256) NULL,
        CONSTRAINT FK_CompanyAssignments_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(CompanyId) ON DELETE CASCADE,
        CONSTRAINT FK_CompanyAssignments_AppUsers FOREIGN KEY (AppUserId) REFERENCES dbo.AppUsers(AppUserId) ON DELETE CASCADE,
        CONSTRAINT UQ_CompanyAssignments_CompanyUser UNIQUE (CompanyId, AppUserId)
    );

    CREATE INDEX IX_CompanyAssignments_AppUserId ON dbo.CompanyAssignments(AppUserId);
    CREATE INDEX IX_CompanyAssignments_CompanyId ON dbo.CompanyAssignments(CompanyId);
END
GO
