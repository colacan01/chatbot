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

    public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(
        int userId,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUserSessionCountAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task DeleteOldUserSessionsAsync(
        int userId,
        int keepCount = 30,
        CancellationToken cancellationToken = default)
    {
        var sessionsToDelete = await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .Skip(keepCount)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (sessionsToDelete.Any())
        {
            var sessions = await _dbSet
                .Where(s => sessionsToDelete.Contains(s.Id))
                .ToListAsync(cancellationToken);

            _dbSet.RemoveRange(sessions);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbSet.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null)
        {
            session.Title = title;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
