using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Services;

public class ProductContextService : IProductContextService
{
    private readonly IProductRepository _productRepository;
    private readonly IVectorProductRepository _vectorProductRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductContextService> _logger;

    private readonly double _similarityThreshold;
    private readonly int _maxResults;
    private readonly bool _fallbackToKeywordSearch;

    public ProductContextService(
        IProductRepository productRepository,
        IVectorProductRepository vectorProductRepository,
        IEmbeddingService embeddingService,
        IConfiguration configuration,
        ILogger<ProductContextService> logger)
    {
        _productRepository = productRepository;
        _vectorProductRepository = vectorProductRepository;
        _embeddingService = embeddingService;
        _configuration = configuration;
        _logger = logger;

        _similarityThreshold = double.Parse(
            configuration["VectorSearch:SimilarityThreshold"] ?? "0.6");
        _maxResults = int.Parse(
            configuration["VectorSearch:MaxResults"] ?? "5");
        _fallbackToKeywordSearch = bool.Parse(
            configuration["VectorSearch:FallbackToKeywordSearch"] ?? "true");
    }

    public async Task<List<Product>> SearchProductsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Product search request: {Query}", query);

        // Step 1: Try vector search
        var vectorResults = await PerformVectorSearchAsync(query, maxResults, cancellationToken);

        if (vectorResults.Count >= 1)
        {
            _logger.LogInformation(
                "Vector search successful: {Count} results with avg similarity {AvgSimilarity:F3}",
                vectorResults.Count,
                vectorResults.Average(r => r.Similarity));

            // Convert ProductEmbedding → Product for backward compatibility
            return await ConvertToProductsAsync(
                vectorResults.Select(r => r.Product).ToList(),
                cancellationToken);
        }

        // Step 2: Fallback to keyword search
        if (_fallbackToKeywordSearch)
        {
            _logger.LogInformation("Vector search returned no results, falling back to keyword search");
            return await _productRepository.SearchProductsAsync(query, maxResults, cancellationToken);
        }

        _logger.LogWarning("Vector search failed and fallback disabled, returning empty results");
        return new List<Product>();
    }

    private async Task<List<VectorSearchResult>> PerformVectorSearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate embedding for search query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                query,
                cancellationToken);

            if (queryEmbedding == null)
            {
                _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                return new List<VectorSearchResult>();
            }

            // Perform vector similarity search
            return await _vectorProductRepository.SearchBySimilarityAsync(
                queryEmbedding,
                maxResults,
                _similarityThreshold,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search for query: {Query}", query);
            return new List<VectorSearchResult>();
        }
    }

    private Task<List<Product>> ConvertToProductsAsync(
        List<Product> products,
        CancellationToken cancellationToken)
    {
        // No conversion needed - Product entities are already from product_embeddings table
        return Task.FromResult(products);
    }

    public async Task<Product?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetByIdAsync(productId, cancellationToken);
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _productRepository.GetByCategoryAsync(category, cancellationToken);
    }
}
