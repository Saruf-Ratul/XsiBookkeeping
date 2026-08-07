USE XsiBookkeeping;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Companies)
BEGIN
    INSERT INTO dbo.Companies (Name, Country, SortOrder) VALUES
        (N'Acme Canada Ltd', N'CA', 0),
        (N'Summit US Corp', N'US', 1),
        (N'Northern Books', N'CA', 2);

    DECLARE @Acme BIGINT = (SELECT CompanyId FROM dbo.Companies WHERE Name = N'Acme Canada Ltd');
    DECLARE @Summit BIGINT = (SELECT CompanyId FROM dbo.Companies WHERE Name = N'Summit US Corp');
    DECLARE @Northern BIGINT = (SELECT CompanyId FROM dbo.Companies WHERE Name = N'Northern Books');

    INSERT INTO dbo.Accounts (CompanyId, Name, SortOrder) VALUES
        (@Acme, N'Operating Account', 0),
        (@Acme, N'Payroll', 1),
        (@Summit, N'Checking', 0),
        (@Summit, N'Credit Card', 1),
        (@Northern, N'Main Ledger', 0);
END
GO

-- Example task assignments (run after V004 migration and after users exist)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CompanyAssignments')
BEGIN
    DECLARE @AcmeId BIGINT = (SELECT TOP 1 CompanyId FROM dbo.Companies WHERE Name = N'Acme Canada Ltd');
    DECLARE @SummitId BIGINT = (SELECT TOP 1 CompanyId FROM dbo.Companies WHERE Name = N'Summit US Corp');
    DECLARE @NorthernId BIGINT = (SELECT TOP 1 CompanyId FROM dbo.Companies WHERE Name = N'Northern Books');

    IF @AcmeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.CompanyAssignments WHERE CompanyId = @AcmeId)
    BEGIN
        INSERT INTO dbo.CompanyAssignments (CompanyId, AppUserId, AssignedByLogin)
        SELECT @AcmeId, u.AppUserId, N'ADMIN'
        FROM dbo.AppUsers u
        WHERE u.Role = N'User' AND u.IsActive = 1;
    END

    IF @SummitId IS NOT NULL AND @NorthernId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.CompanyAssignments WHERE CompanyId = @SummitId)
    BEGIN
        INSERT INTO dbo.CompanyAssignments (CompanyId, AppUserId, AssignedByLogin)
        SELECT c.CompanyId, u.AppUserId, N'ADMIN'
        FROM (VALUES (@SummitId), (@NorthernId)) AS c(CompanyId)
        CROSS JOIN (
            SELECT TOP 1 AppUserId FROM dbo.AppUsers WHERE Role = N'User' AND IsActive = 1 ORDER BY AppUserId
        ) u;
    END
END
GO
