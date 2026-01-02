using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces.Repositories;

public interface IFAQRepository : IRepository<FAQ>
{
    Task<List<FAQ>> SearchFAQsAsync(
        string searchTerm,
        int maxResults = 10,
        CancellationToken cancellationToken = default);

    Task<List<FAQ>> GetActiveFAQsAsync(CancellationToken cancellationToken = default);

    Task<List<FAQ>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default);
}
