USE XsiBookkeeping;
GO

-- Default login: admin / Ledger123!
-- Change the password after first sign-in via Admin > Users.
IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE WindowsLogin = N'ADMIN')
BEGIN
    INSERT INTO dbo.AppUsers (WindowsLogin, DisplayName, Role, IsActive, PasswordHash)
    VALUES (
        N'ADMIN',
        N'System Administrator',
        N'Sysadmin',
        1,
        N'OH5n3y0H9z5wF8zA9U06jg==|GG2OmCWXzUv430YkIzSz0mk6NALp4MlbJbZYEjmvi34='
    );
END
ELSE
BEGIN
    UPDATE dbo.AppUsers
    SET PasswordHash = N'OH5n3y0H9z5wF8zA9U06jg==|GG2OmCWXzUv430YkIzSz0mk6NALp4MlbJbZYEjmvi34='
    WHERE WindowsLogin = N'ADMIN' AND (PasswordHash IS NULL OR PasswordHash = N'');
END
GO
