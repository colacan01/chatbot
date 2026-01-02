using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<List<Order>> GetByCustomerEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.CustomerEmail.ToLower() == email.ToLower())
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
}
