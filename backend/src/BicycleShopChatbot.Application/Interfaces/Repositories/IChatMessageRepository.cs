using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IChatMessageRepository : IRepository<ChatMessage>
{
    Task<List<ChatMessage>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int count = 50,
        CancellationToken cancellationToken = default);
}
