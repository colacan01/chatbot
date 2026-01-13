using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IRerankingService
{
    Task<List<RerankResult<Product>>> RerankProductsAsync(
        string query,
        List<Product> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default);

    Task<List<RerankResult<FAQ>>> RerankFaqsAsync(
        string query,
        List<FAQ> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

public class RerankResult<T>
{
    public required T Item { get; set; }
    public double RelevanceScore { get; set; }
    public int OriginalIndex { get; set; }
}
