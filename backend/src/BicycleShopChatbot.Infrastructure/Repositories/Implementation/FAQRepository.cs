using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Infrastructure.Repositories.Implementation;

public class FAQRepository : Repository<FAQ>, IFAQRepository
{
    public FAQRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<FAQ>> SearchFAQsAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(f => f.IsActive &&
                       (f.QuestionKorean.ToLower().Contains(lowerSearchTerm) ||
                        f.AnswerKorean.ToLower().Contains(lowerSearchTerm) ||
                        (f.Keywords != null && f.Keywords.ToLower().Contains(lowerSearchTerm))))
            .OrderByDescending(f => f.ViewCount)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FAQ>> GetActiveFAQsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.ViewCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FAQ>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.IsActive && f.Category != null && f.Category.ToLower() == category.ToLower())
            .OrderByDescending(f => f.ViewCount)
            .ToListAsync(cancellationToken);
    }
}
