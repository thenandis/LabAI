using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharpAPI.Domain.Entities;

namespace SharpAPI.Infrastructure.Persistence.Configurations;

/// <summary>Maps Category → dbo.Categories.</summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "dbo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("CategoryID");
        builder.Property(c => c.Name).HasColumnName("CategoryName")
            .HasMaxLength(Category.NameMaxLength).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

/// <summary>Maps Supplier → dbo.Suppliers.</summary>
internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "dbo");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("SupplierID");
        builder.Property(s => s.CompanyName).HasMaxLength(Supplier.CompanyNameMaxLength).IsRequired();
        builder.Property(s => s.ContactName).HasMaxLength(30);
        builder.Property(s => s.City).HasMaxLength(15);
        builder.Property(s => s.Country).HasMaxLength(15);
        builder.Property(s => s.Phone).HasMaxLength(24);
    }
}
