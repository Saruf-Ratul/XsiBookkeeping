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


USE XsiBookkeeping;
GO

-- Default password for all users below: Ledger123!
DECLARE @DefaultPasswordHash NVARCHAR(256) = N'OH5n3y0H9z5wF8zA9U06jg==|GG2OmCWXzUv430YkIzSz0mk6NALp4MlbJbZYEjmvi34=';

MERGE dbo.AppUsers AS target
USING (VALUES
    (N'JESSIE',  N'Jessie',  N'Admin',     1),
    (N'SARUF',   N'Saruf',   N'Sysadmin',  1),
    (N'MARIAM',  N'Mariam',  N'User',      1),
    (N'MARZ',    N'Marz',    N'Admin',     1),
    (N'CHA',     N'Cha',     N'User',      1),
    (N'HUMAYRA', N'Humayra', N'User',      1)
) AS source (WindowsLogin, DisplayName, Role, IsActive)
ON target.WindowsLogin = source.WindowsLogin
WHEN MATCHED THEN
    UPDATE SET
        DisplayName = source.DisplayName,
        Role = source.Role,
        IsActive = source.IsActive,
        PasswordHash = @DefaultPasswordHash,
        ModifiedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (WindowsLogin, DisplayName, Role, IsActive, PasswordHash)
    VALUES (source.WindowsLogin, source.DisplayName, source.Role, source.IsActive, @DefaultPasswordHash);
GO

-- Verify
SELECT AppUserId, WindowsLogin, DisplayName, Role, IsActive
FROM dbo.AppUsers
ORDER BY Role DESC, WindowsLogin;
GO