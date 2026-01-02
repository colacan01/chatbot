using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BicycleShopChatbot.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.ChatSessionId)
            .IsRequired();

        builder.HasIndex(cm => cm.ChatSessionId);

        builder.Property(cm => cm.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(cm => cm.Content)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(cm => cm.Timestamp)
            .IsRequired();

        builder.HasIndex(cm => cm.Timestamp);

        builder.Property(cm => cm.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(cm => cm.Category);

        builder.Property(cm => cm.IntentDetected)
            .HasMaxLength(100);

        builder.Property(cm => cm.Metadata)
            .HasColumnType("TEXT");

        builder.HasOne(cm => cm.ChatSession)
            .WithMany(cs => cs.Messages)
            .HasForeignKey(cm => cm.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cm => cm.Product)
            .WithMany(p => p.ChatMessages)
            .HasForeignKey(cm => cm.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(cm => cm.Order)
            .WithMany(o => o.ChatMessages)
            .HasForeignKey(cm => cm.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
