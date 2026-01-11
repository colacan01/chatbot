using BicycleShopChatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BicycleShopChatbot.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product_embeddings");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.ProductCode)
            .HasColumnName("product_code")
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.ProductCode)
            .IsUnique();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.NameKorean)
            .HasColumnName("name_korean")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Category);

        builder.Property(p => p.Brand)
            .HasColumnName("brand")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.Brand);

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        builder.Property(p => p.DescriptionKorean)
            .HasColumnName("description_korean")
            .HasColumnType("TEXT");

        builder.Property(p => p.Specifications)
            .HasColumnName("specifications")
            .IsRequired()
            .HasColumnType("JSONB")
            .HasDefaultValue("{}");

        builder.Property(p => p.StockQuantity)
            .HasColumnName("stock_quantity")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsAvailable)
            .HasColumnName("is_available")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => new { p.IsAvailable, p.StockQuantity });

        builder.Property(p => p.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
