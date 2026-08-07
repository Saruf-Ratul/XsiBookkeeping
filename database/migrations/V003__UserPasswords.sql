USE XsiBookkeeping;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AppUsers') AND name = N'PasswordHash'
)
BEGIN
    ALTER TABLE dbo.AppUsers ADD PasswordHash NVARCHAR(256) NULL;
END
GO
