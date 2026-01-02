using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class ChatMessageRepository : Repository<ChatMessage>, IChatMessageRepository
{
    public ChatMessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<ChatMessage>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cm => cm.ChatSessionId == sessionId)
            .OrderByDescending(cm => cm.Timestamp)
            .Take(count)
            .OrderBy(cm => cm.Timestamp)
            .Include(cm => cm.Product)
            .Include(cm => cm.Order)
            .ToListAsync(cancellationToken);
    }
}
