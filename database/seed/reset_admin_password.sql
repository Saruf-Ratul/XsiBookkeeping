USE XsiBookkeeping;
GO

-- Resets default admin login to: admin / Ledger123!
MERGE dbo.AppUsers AS target
USING (SELECT N'ADMIN' AS WindowsLogin) AS source
ON target.WindowsLogin = source.WindowsLogin
WHEN MATCHED THEN
    UPDATE SET
        DisplayName = N'System Administrator',
        Role = N'Sysadmin',
        IsActive = 1,
        PasswordHash = N'OH5n3y0H9z5wF8zA9U06jg==|GG2OmCWXzUv430YkIzSz0mk6NALp4MlbJbZYEjmvi34=',
        ModifiedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (WindowsLogin, DisplayName, Role, IsActive, PasswordHash)
    VALUES (
        N'ADMIN',
        N'System Administrator',
        N'Sysadmin',
        1,
        N'OH5n3y0H9z5wF8zA9U06jg==|GG2OmCWXzUv430YkIzSz0mk6NALp4MlbJbZYEjmvi34='
    );
GO
