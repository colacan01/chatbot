using BicycleShopChatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BicycleShopChatbot.Infrastructure.Data.Configurations;

public class FAQConfiguration : IEntityTypeConfiguration<FAQ>
{
    public void Configure(EntityTypeBuilder<FAQ> builder)
    {
        builder.ToTable("FAQs");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Question)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(f => f.QuestionKorean)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(f => f.Answer)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(f => f.AnswerKorean)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(f => f.Category)
            .HasMaxLength(100);

        builder.HasIndex(f => f.Category);

        builder.Property(f => f.Keywords)
            .HasMaxLength(500);

        builder.Property(f => f.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(f => f.IsActive);

        builder.Property(f => f.CreatedAt)
            .IsRequired();
    }
}
