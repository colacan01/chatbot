using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Application.Interfaces.Repositories;

namespace BicycleShopChatbot.Application.Services;

public class ProductContextService : IProductContextService
{
    private readonly IProductRepository _productRepository;

    public ProductContextService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<Product>> SearchProductsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        return await _productRepository.SearchProductsAsync(query, maxResults, cancellationToken);
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
