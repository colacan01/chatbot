using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Application.Interfaces.Repositories;

namespace BicycleShopChatbot.Application.Services;

public class OrderContextService : IOrderContextService
{
    private readonly IOrderRepository _orderRepository;

    public OrderContextService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order?> GetOrderByNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
    }

    public async Task<List<Order>> GetOrdersByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GetByCustomerEmailAsync(email, cancellationToken);
    }
}
