/* =============================================================================
   01_CreateDatabase.sql
   Creates the NorthwindCatalog database (a trimmed copy of Microsoft's
   Northwind sample). Every script is idempotent, so running it again is safe.
   ============================================================================= */
SET NOCOUNT ON;
GO

IF DB_ID(N'NorthwindCatalog') IS NULL
BEGIN
    PRINT 'Creating database NorthwindCatalog...';
    CREATE DATABASE NorthwindCatalog;
END
GO

-- READ_COMMITTED_SNAPSHOT: readers don't block writers (and vice versa).
-- Big win for read-heavy APIs; it is the default on Azure SQL.
ALTER DATABASE NorthwindCatalog SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
ALTER DATABASE NorthwindCatalog SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
