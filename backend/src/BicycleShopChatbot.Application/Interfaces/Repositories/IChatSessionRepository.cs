using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetWithMessagesAsync(Guid id, int messageCount = 50, CancellationToken cancellationToken = default);
}
