using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IProductContextService
{
    Task<List<Product>> SearchProductsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetProductsByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);
}
