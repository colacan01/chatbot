using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> SearchProductsAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);
}
