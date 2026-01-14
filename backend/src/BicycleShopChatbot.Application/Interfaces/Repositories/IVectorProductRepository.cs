using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IVectorProductRepository : IRepository<Product>
{
    /// <summary>
    /// Performs vector similarity search using cosine distance
    /// </summary>
    Task<List<VectorSearchResult>> SearchBySimilarityAsync(
        float[] queryEmbedding,
        int maxResults = 10,
        double similarityThreshold = 0.6,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Keyword search fallback (for when vector search fails or returns no results)
    /// </summary>
    Task<List<Product>> SearchByKeywordAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get product by product code
    /// </summary>
    Task<Product?> GetByProductCodeAsync(
        string productCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert product embedding (used for syncing Product → ProductEmbedding)
    /// </summary>
    Task UpsertProductEmbeddingAsync(
        Product productEmbedding,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products by price range
    /// </summary>
    Task<List<Product>> GetByPriceRangeAsync(
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search products by name (Korean or English)
    /// </summary>
    Task<List<Product>> SearchByProductNameAsync(
        string productName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search products with combined filters (price, category, product name)
    /// </summary>
    Task<List<Product>> SearchWithFiltersAsync(
        decimal? minPrice,
        decimal? maxPrice,
        string? category,
        string? productNameQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products by multiple product codes (batch query for validation)
    /// </summary>
    Task<List<Product>> GetByProductCodesAsync(
        IEnumerable<string> productCodes,
        CancellationToken cancellationToken = default);
}

public class VectorSearchResult
{
    public Product Product { get; set; } = null!;
    public double Similarity { get; set; } // Cosine similarity (0-1)
}
