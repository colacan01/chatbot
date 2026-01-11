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
                    id, product_code, name, name_korean, category, brand, price,
                    description, description_korean, detailed_description,
                    specifications, stock_quantity, is_available, image_url,
                    embedding, searchable_text, created_at, updated_at,
                    (1 - (embedding <=> @queryVector::vector)) as similarity
                FROM product_embeddings
                WHERE embedding IS NOT NULL
                  AND is_available = true
                  AND (1 - (embedding <=> @queryVector::vector)) >= @threshold
                ORDER BY embedding <=> @queryVector::vector
                LIMIT @maxResults";

            var results = new List<VectorSearchResult>();

            await using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("@queryVector", vectorString));
            command.Parameters.Add(new NpgsqlParameter("@threshold", similarityThreshold));
            command.Parameters.Add(new NpgsqlParameter("@maxResults", maxResults));

            await _context.Database.OpenConnectionAsync(cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var product = new Product
                {
                    Id = reader.GetInt32(0),
                    ProductCode = reader.GetString(1),
                    Name = reader.GetString(2),
                    NameKorean = reader.GetString(3),
                    Category = reader.GetString(4),
                    Brand = reader.GetString(5),
                    Price = reader.GetDecimal(6),
                    Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                    DescriptionKorean = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Specifications = reader.GetString(10),
                    StockQuantity = reader.GetInt32(11),
                    IsAvailable = reader.GetBoolean(12),
                    ImageUrl = reader.IsDBNull(13) ? null : reader.GetString(13),
                    CreatedAt = reader.GetDateTime(16),
                    UpdatedAt = reader.GetDateTime(17)
                };

                var similarity = reader.GetDouble(18);

                results.Add(new VectorSearchResult
                {
                    Product = product,
                    Similarity = similarity
                });
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
        var existing = await GetByProductCodeAsync(
            productEmbedding.ProductCode,
            cancellationToken);

        if (existing != null)
        {
            // Update existing
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
}
