#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel;
using System.ComponentModel;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Plugins;

public class ProductSearchPlugin
{
    private readonly IVectorProductRepository _vectorProductRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRerankingService _rerankingService;
    private readonly ILogger<ProductSearchPlugin> _logger;

    public ProductSearchPlugin(
        IVectorProductRepository vectorProductRepository,
        IEmbeddingService embeddingService,
        IRerankingService rerankingService,
        ILogger<ProductSearchPlugin> logger)
    {
        _vectorProductRepository = vectorProductRepository ?? throw new ArgumentNullException(nameof(vectorProductRepository));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _rerankingService = rerankingService ?? throw new ArgumentNullException(nameof(rerankingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("search_products")]
    [Description("Searches for bicycle products using semantic search and re-ranking")]
    public async Task<string> SearchProductsAsync(
        [Description("User's product search query in Korean")] string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching products for query: {Query}", query);

        try
        {
            // Step 1: Generate embedding
            var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            if (embedding == null || embedding.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                return "검색 중 오류가 발생했습니다.";
            }

            // Step 2: Retrieve top-20 candidates from vector search
            var candidates = await _vectorProductRepository.SearchBySimilarityAsync(
                embedding,
                maxResults: 20,
                similarityThreshold: 0.3, // Lower threshold for broader recall
                cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("No products found for query: {Query}", query);
                return "검색 결과가 없습니다.";
            }

            var products = candidates.Select(c => c.Product).ToList();
            _logger.LogInformation("Found {Count} candidates before re-ranking", products.Count);

            // Step 3: Re-rank to top-5
            var reranked = await _rerankingService.RerankProductsAsync(
                query,
                products,
                topK: 5,
                cancellationToken);

            if (reranked.Count == 0)
            {
                return "검색 결과가 없습니다.";
            }

            // Step 4: Format results
            var results = reranked.Select((r, idx) =>
                $"{idx + 1}. {r.Item.NameKorean} (제품코드: {r.Item.ProductCode})\n" +
                $"   - 카테고리: {r.Item.Category}\n" +
                $"   - 브랜드: {r.Item.Brand}\n" +
                $"   - 가격: {r.Item.Price:N0}원\n" +
                $"   - 재고: {r.Item.StockQuantity}개\n" +
                $"   - 설명: {r.Item.DescriptionKorean}\n" +
                $"   - 관련도 점수: {r.RelevanceScore:F3}\n");

            var resultText = string.Join("\n", results);
            _logger.LogInformation("Returned {Count} re-ranked products", reranked.Count);

            return resultText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products for query: {Query}", query);
            return "검색 중 오류가 발생했습니다.";
        }
    }

    [KernelFunction("get_product_details")]
    [Description("Gets detailed information about a specific product by product code")]
    public async Task<string> GetProductDetailsAsync(
        [Description("Product code (e.g., ROAD001)")] string productCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting product details for code: {ProductCode}", productCode);

        try
        {
            var product = await _vectorProductRepository.GetByProductCodeAsync(productCode, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found with code: {ProductCode}", productCode);
                return $"제품코드 '{productCode}'를 찾을 수 없습니다.";
            }

            return $"### {product.NameKorean} ({product.Name})\n" +
                   $"- **제품코드**: {product.ProductCode}\n" +
                   $"- **카테고리**: {product.Category}\n" +
                   $"- **브랜드**: {product.Brand}\n" +
                   $"- **가격**: {product.Price:N0}원\n" +
                   $"- **재고**: {product.StockQuantity}개\n" +
                   $"- **구매 가능**: {(product.IsAvailable ? "예" : "아니오")}\n" +
                   $"- **설명**: {product.DescriptionKorean}\n" +
                   $"- **상세 사양**: {product.Specifications ?? "정보 없음"}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product details for code: {ProductCode}", productCode);
            return "제품 정보를 가져오는 중 오류가 발생했습니다.";
        }
    }
}
