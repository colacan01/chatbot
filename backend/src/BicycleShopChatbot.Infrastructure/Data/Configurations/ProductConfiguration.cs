using BicycleShopChatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BicycleShopChatbot.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.ProductCode)
            .IsUnique();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NameKorean)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Category);

        builder.Property(p => p.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Brand);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.Description)
            .HasColumnType("TEXT");

        builder.Property(p => p.DescriptionKorean)
            .HasColumnType("TEXT");

        builder.Property(p => p.Specifications)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasDefaultValue("{}");

        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => new { p.IsAvailable, p.StockQuantity });

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
