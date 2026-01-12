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
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(p => p.Category.ToLower() == category.ToLower() && p.IsAvailable)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.IsAvailable && p.StockQuantity > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetByPriceRangeAsync(
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(p => p.IsAvailable);

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> SearchByProductNameAsync(
        string productName,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.IsAvailable &&
                       (p.NameKorean.ToLower().Contains(productName.ToLower()) ||
                        p.Name.ToLower().Contains(productName.ToLower())))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> SearchWithFiltersAsync(
        decimal? minPrice,
        decimal? maxPrice,
        string? category,
        string? productNameQuery,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(p => p.IsAvailable);

        // Apply price filter
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        // Apply product name filter
        if (!string.IsNullOrEmpty(productNameQuery))
        {
            query = query.Where(p =>
                p.NameKorean.ToLower().Contains(productNameQuery.ToLower()) ||
                p.Name.ToLower().Contains(productNameQuery.ToLower()));
        }

        return await query.ToListAsync(cancellationToken);
    }
}
