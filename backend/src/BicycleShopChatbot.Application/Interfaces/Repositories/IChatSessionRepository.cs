using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IChatSessionRepository : IRepository<ChatSession>
{
    Task<ChatSession?> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetWithMessagesAsync(Guid id, int messageCount = 50, CancellationToken cancellationToken = default);

    // Session management methods
    Task<IEnumerable<ChatSession>> GetUserSessionsAsync(int userId, int limit = 30, CancellationToken cancellationToken = default);
    Task<int> GetUserSessionCountAsync(int userId, CancellationToken cancellationToken = default);
    Task DeleteOldUserSessionsAsync(int userId, int keepCount = 30, CancellationToken cancellationToken = default);
    Task UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default);
}
