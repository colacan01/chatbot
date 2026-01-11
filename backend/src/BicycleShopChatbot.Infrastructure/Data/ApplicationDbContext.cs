using BicycleShopChatbot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Product> Products => Set<Product>();
    // ProductEmbedding removed - Product entity now maps to product_embeddings table
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<FAQ> FAQs => Set<FAQ>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Exclude Product table from migrations (using existing product_embeddings table)
        modelBuilder.Entity<Product>().ToTable("product_embeddings", t => t.ExcludeFromMigrations());
    }
}
