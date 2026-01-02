using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Product>> SearchProductsAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(p => p.IsAvailable &&
                       (p.Name.ToLower().Contains(lowerSearchTerm) ||
                        p.NameKorean.ToLower().Contains(lowerSearchTerm) ||
                        p.Category.ToLower().Contains(lowerSearchTerm) ||
                        p.Brand.ToLower().Contains(lowerSearchTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(lowerSearchTerm)) ||
                        (p.DescriptionKorean != null && p.DescriptionKorean.ToLower().Contains(lowerSearchTerm))))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.Category.ToLower() == category.ToLower() && p.IsAvailable)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync(cancellationToken);
    }
}
