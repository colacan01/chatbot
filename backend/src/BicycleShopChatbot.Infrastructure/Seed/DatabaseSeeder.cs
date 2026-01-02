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
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Seed Products
            if (!await _context.Products.AnyAsync())
            {
                _logger.LogInformation("Seeding products...");
                var products = ProductSeedData.GetSeedProducts();
                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} products", products.Count);
            }

            // Seed FAQs
            if (!await _context.FAQs.AnyAsync())
            {
                _logger.LogInformation("Seeding FAQs...");
                var faqs = FaqSeedData.GetSeedFaqs();
                await _context.FAQs.AddRangeAsync(faqs);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} FAQs", faqs.Count);
            }

            // Seed Orders
            if (!await _context.Orders.AnyAsync())
            {
                _logger.LogInformation("Seeding orders...");
                var orders = OrderSeedData.GetSeedOrders();
                await _context.Orders.AddRangeAsync(orders);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} orders", orders.Count);
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
