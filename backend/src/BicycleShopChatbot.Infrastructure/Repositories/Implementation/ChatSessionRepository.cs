using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ChatSession?> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(cs => cs.SessionId == sessionId, cancellationToken);
    }

    public async Task<ChatSession?> GetWithMessagesAsync(
        Guid id,
        int messageCount = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cs => cs.Messages.OrderByDescending(m => m.Timestamp).Take(messageCount))
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }
}
