using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
}
