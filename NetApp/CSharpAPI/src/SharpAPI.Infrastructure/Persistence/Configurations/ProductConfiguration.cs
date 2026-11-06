using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Product entity to dbo.Products (see SQLServerDb/scripts/02_Schema.sql).
/// </summary>
/// <remarks>
/// <para>
/// This is the FLUENT API alternative to putting [Table]/[Column] attributes on the
/// entity. The Domain stays persistence-ignorant: it has no EF attributes and no
/// knowledge that the column is really called "ProductID".
/// </para>
/// <para>
/// DATABASE-FIRST: the SQL scripts own the schema (the DBA-friendly approach), and this
/// class simply describes it. Teams that prefer CODE-FIRST would generate EF Migrations
/// from these same configurations instead.
/// </para>
/// </remarks>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "dbo");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("ProductID");
        builder.Property(p => p.Name).HasColumnName("ProductName")
            .HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.SupplierId).HasColumnName("SupplierID");
        builder.Property(p => p.CategoryId).HasColumnName("CategoryID");
        builder.Property(p => p.QuantityPerUnit).HasMaxLength(Product.QuantityPerUnitMaxLength);

        // Always give money an explicit precision. Without it EF warns and may truncate.
        builder.Property(p => p.UnitPrice).HasPrecision(19, 4);

        builder.Property(p => p.CreatedAtUtc).HasPrecision(3);
        builder.Property(p => p.ModifiedAtUtc).HasPrecision(3);

        // ROWVERSION: SQL Server generates it on every insert/update. EF reads it back
        // (OUTPUT clause) and adds it to the WHERE clause of UPDATE/DELETE statements.
        builder.Property(p => p.RowVersion).IsRowVersion();

        // A computed C# property, not a column.
        builder.Ignore(p => p.NeedsReorder);

        // Relationships. Restrict = you can't delete a category that still has products
        // (matches the FK in SQL, which has no ON DELETE CASCADE).
        builder.HasOne(p => p.Category).WithMany()
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Supplier).WithMany()
            .HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Name).HasDatabaseName("IX_Products_ProductName");
    }
}
