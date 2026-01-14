using BicycleShopChatbot.Application.Interfaces.Repositories;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class VectorProductRepository : Repository<Product>, IVectorProductRepository
{
    private readonly ILogger<VectorProductRepository> _logger;

    public VectorProductRepository(
        ApplicationDbContext context,
        ILogger<VectorProductRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<List<VectorSearchResult>> SearchBySimilarityAsync(
        float[] queryEmbedding,
        int maxResults = 10,
        double similarityThreshold = 0.6,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding == null || queryEmbedding.Length != 768)
        {
            _logger.LogWarning("Invalid query embedding dimension: {Length}", queryEmbedding?.Length ?? 0);
            return new List<VectorSearchResult>();
        }

        try
        {
            // PostgreSQL vector similarity search using <=> (cosine distance operator)
            // Similarity = 1 - distance (so higher is better)
            var vectorString = $"[{string.Join(",", queryEmbedding)}]";

            var sql = @"
                SELECT
                    id,
                    (1 - (embedding <=> @queryVector::vector)) as similarity
                FROM product_embeddings
                WHERE embedding IS NOT NULL
                  AND is_available = true
                  AND (1 - (embedding <=> @queryVector::vector)) >= @threshold
                ORDER BY embedding <=> @queryVector::vector
                LIMIT @maxResults";

            var productIdsSimilarities = new List<(int Id, double Similarity)>();

            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("@queryVector", vectorString));
            command.Parameters.Add(new NpgsqlParameter("@threshold", similarityThreshold));
            command.Parameters.Add(new NpgsqlParameter("@maxResults", maxResults));

            await _context.Database.OpenConnectionAsync(cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt32(0);
                var similarity = reader.GetDouble(1);
                productIdsSimilarities.Add((id, similarity));
            }

            await reader.CloseAsync();

            // If no results from vector search, return empty list
            if (!productIdsSimilarities.Any())
            {
                _logger.LogInformation(
                    "Vector search returned 0 results (threshold: {Threshold})",
                    similarityThreshold);
                return new List<VectorSearchResult>();
            }

            // Reload Products from EF Core with AsNoTracking to ensure fresh data
            var productIds = productIdsSimilarities.Select(x => x.Id).ToList();
            var products = await _dbSet
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // Combine Products with similarity scores, preserving order from vector search
            var results = new List<VectorSearchResult>();
            foreach (var (id, similarity) in productIdsSimilarities)
            {
                var product = products.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    results.Add(new VectorSearchResult
                    {
                        Product = product,
                        Similarity = similarity
                    });
                }
            }

            _logger.LogInformation(
                "Vector search returned {Count} results (threshold: {Threshold})",
                results.Count, similarityThreshold);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing vector similarity search");
            return new List<VectorSearchResult>();
        }
    }

    public async Task<List<Product>> SearchByKeywordAsync(
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

    public async Task<Product?> GetByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ProductCode == productCode, cancellationToken);
    }

    public async Task UpsertProductEmbeddingAsync(
        Product productEmbedding,
        CancellationToken cancellationToken = default)
    {
        // Use tracking query for update operations (not AsNoTracking)
        var existing = await _dbSet
            .FirstOrDefaultAsync(p => p.ProductCode == productEmbedding.ProductCode, cancellationToken);

        if (existing != null)
        {
            // Update existing (tracked entity)
            existing.Name = productEmbedding.Name;
            existing.NameKorean = productEmbedding.NameKorean;
            existing.Category = productEmbedding.Category;
            existing.Brand = productEmbedding.Brand;
            existing.Price = productEmbedding.Price;
            existing.Description = productEmbedding.Description;
            existing.DescriptionKorean = productEmbedding.DescriptionKorean;
            existing.Specifications = productEmbedding.Specifications;
            existing.StockQuantity = productEmbedding.StockQuantity;
            existing.IsAvailable = productEmbedding.IsAvailable;
            existing.ImageUrl = productEmbedding.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Insert new
            productEmbedding.CreatedAt = DateTime.UtcNow;
            productEmbedding.UpdatedAt = DateTime.UtcNow;
            await _dbSet.AddAsync(productEmbedding, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
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

    public async Task<List<Product>> GetByProductCodesAsync(
        IEnumerable<string> productCodes,
        CancellationToken cancellationToken = default)
    {
        var codeList = productCodes.ToList();

        if (!codeList.Any())
        {
            return new List<Product>();
        }

        // Case-insensitive comparison using ToUpper
        var upperCodes = codeList.Select(c => c.ToUpperInvariant()).ToList();

        return await _dbSet
            .AsNoTracking()
            .Where(p => upperCodes.Contains(p.ProductCode.ToUpper()))
            .ToListAsync(cancellationToken);
    }
}
