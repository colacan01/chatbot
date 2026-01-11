using BicycleShopChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Infrastructure.Seed;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Product table - using existing product_embeddings table from VectorDataLoader
            _logger.LogInformation("Product data is available in existing product_embeddings table");

            // Seed FAQs
            if (!await _context.FAQs.AnyAsync())
            {
                _logger.LogInformation("Seeding FAQs...");
                var faqs = FaqSeedData.GetSeedFaqs();
                await _context.FAQs.AddRangeAsync(faqs);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {faqs.Count} FAQs");
            }
            else
            {
                _logger.LogInformation("FAQs already exist, skipping seed");
            }

            // Seed Orders
            if (!await _context.Orders.AnyAsync())
            {
                _logger.LogInformation("Seeding Orders...");
                var orders = OrderSeedData.GetSeedOrders();
                await _context.Orders.AddRangeAsync(orders);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Seeded {orders.Count} Orders");
            }
            else
            {
                _logger.LogInformation("Orders already exist, skipping seed");
            }

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
