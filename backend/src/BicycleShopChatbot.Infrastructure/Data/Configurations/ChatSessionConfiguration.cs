using BicycleShopChatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BicycleShopChatbot.Infrastructure.Data.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("ChatSessions");

        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(cs => cs.SessionId)
            .IsUnique();

        builder.Property(cs => cs.UserId)
            .HasMaxLength(100);

        builder.HasIndex(cs => cs.UserId);

        builder.Property(cs => cs.UserName)
            .HasMaxLength(200);

        builder.Property(cs => cs.Title)
            .HasMaxLength(200);

        builder.Property(cs => cs.CreatedAt)
            .IsRequired();

        builder.Property(cs => cs.LastActivityAt)
            .IsRequired();

        builder.HasIndex(cs => cs.LastActivityAt);

        builder.Property(cs => cs.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(cs => cs.SessionMetadata)
            .HasColumnType("TEXT");

        builder.Property(cs => cs.TotalMessages)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasMany(cs => cs.Messages)
            .WithOne(cm => cm.ChatSession)
            .HasForeignKey(cm => cm.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
