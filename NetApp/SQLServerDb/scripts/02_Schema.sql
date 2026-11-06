/* =============================================================================
   02_Schema.sql
   Tables, constraints and indexes. Column names/sizes follow the original
   Northwind schema. Products gains audit columns and a ROWVERSION column,
   which the API uses for optimistic concurrency.
   ============================================================================= */
USE NorthwindCatalog;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ---------- Categories ---------- */
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryID   INT            IDENTITY(1,1) NOT NULL,
        CategoryName NVARCHAR(15)   NOT NULL,
        Description  NVARCHAR(MAX)  NULL,
        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (CategoryID),
        CONSTRAINT UQ_Categories_CategoryName UNIQUE (CategoryName)
    );
END
GO

/* ---------- Suppliers ---------- */
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers
    (
        SupplierID   INT           IDENTITY(1,1) NOT NULL,
        CompanyName  NVARCHAR(40)  NOT NULL,
        ContactName  NVARCHAR(30)  NULL,
        City         NVARCHAR(15)  NULL,
        Country      NVARCHAR(15)  NULL,
        Phone        NVARCHAR(24)  NULL,
        CONSTRAINT PK_Suppliers PRIMARY KEY CLUSTERED (SupplierID)
    );

    CREATE NONCLUSTERED INDEX IX_Suppliers_CompanyName ON dbo.Suppliers (CompanyName);
END
GO

/* ---------- Products ---------- */
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductID       INT            IDENTITY(1,1) NOT NULL,
        ProductName     NVARCHAR(40)   NOT NULL,
        SupplierID      INT            NULL,
        CategoryID      INT            NULL,
        QuantityPerUnit NVARCHAR(20)   NULL,
        UnitPrice       DECIMAL(19,4)  NOT NULL CONSTRAINT DF_Products_UnitPrice    DEFAULT (0),
        UnitsInStock    SMALLINT       NOT NULL CONSTRAINT DF_Products_UnitsInStock DEFAULT (0),
        UnitsOnOrder    SMALLINT       NOT NULL CONSTRAINT DF_Products_UnitsOnOrder DEFAULT (0),
        ReorderLevel    SMALLINT       NOT NULL CONSTRAINT DF_Products_ReorderLevel DEFAULT (0),
        Discontinued    BIT            NOT NULL CONSTRAINT DF_Products_Discontinued DEFAULT (0),

        -- Auditing (set by the API through an EF Core SaveChanges interceptor)
        CreatedAtUtc    DATETIME2(3)   NOT NULL CONSTRAINT DF_Products_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        ModifiedAtUtc   DATETIME2(3)   NULL,

        -- Optimistic concurrency token: changes automatically on every UPDATE
        RowVersion      ROWVERSION     NOT NULL,

        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (ProductID),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories (CategoryID),
        CONSTRAINT FK_Products_Suppliers  FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers  (SupplierID),
        CONSTRAINT CK_Products_UnitPrice    CHECK (UnitPrice    >= 0),
        CONSTRAINT CK_Products_UnitsInStock CHECK (UnitsInStock >= 0),
        CONSTRAINT CK_Products_UnitsOnOrder CHECK (UnitsOnOrder >= 0),
        CONSTRAINT CK_Products_ReorderLevel CHECK (ReorderLevel >= 0)
    );

    -- Index every FK column (SQL Server doesn't do this for you) and the
    -- columns the API searches and sorts on.
    CREATE NONCLUSTERED INDEX IX_Products_CategoryID  ON dbo.Products (CategoryID);
    CREATE NONCLUSTERED INDEX IX_Products_SupplierID  ON dbo.Products (SupplierID);
    CREATE NONCLUSTERED INDEX IX_Products_ProductName ON dbo.Products (ProductName)
        INCLUDE (UnitPrice, UnitsInStock, Discontinued);
END
GO
