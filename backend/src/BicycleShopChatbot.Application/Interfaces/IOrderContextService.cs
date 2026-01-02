using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IOrderContextService
{
    Task<Order?> GetOrderByNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetOrdersByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
