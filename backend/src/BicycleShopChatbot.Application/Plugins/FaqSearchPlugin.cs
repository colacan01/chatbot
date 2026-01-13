#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel;
using System.ComponentModel;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Plugins;

public class FaqSearchPlugin
{
    private readonly IFAQRepository _faqRepository;
    private readonly IRerankingService _rerankingService;
    private readonly ILogger<FaqSearchPlugin> _logger;

    public FaqSearchPlugin(
        IFAQRepository faqRepository,
        IRerankingService rerankingService,
        ILogger<FaqSearchPlugin> logger)
    {
        _faqRepository = faqRepository ?? throw new ArgumentNullException(nameof(faqRepository));
        _rerankingService = rerankingService ?? throw new ArgumentNullException(nameof(rerankingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("search_faqs")]
    [Description("Searches frequently asked questions using keyword search and re-ranking")]
    public async Task<string> SearchFaqsAsync(
        [Description("User's question in Korean")] string question,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching FAQs for question: {Question}", question);

        try
        {
            // Step 1: Keyword search for top-20 candidates
            var candidates = await _faqRepository.SearchFAQsAsync(
                question,
                maxResults: 20,
                cancellationToken);

            if (candidates.Count == 0)
            {
                _logger.LogInformation("No FAQs found for question: {Question}", question);
                return "관련 FAQ를 찾을 수 없습니다.";
            }

            _logger.LogInformation("Found {Count} FAQ candidates before re-ranking", candidates.Count);

            // Step 2: Re-rank to top-5
            var reranked = await _rerankingService.RerankFaqsAsync(
                question,
                candidates,
                topK: 5,
                cancellationToken);

            if (reranked.Count == 0)
            {
                return "관련 FAQ를 찾을 수 없습니다.";
            }

            // Step 3: Format results
            var results = reranked.Select((r, idx) =>
                $"**Q{idx + 1}: {r.Item.QuestionKorean}**\n" +
                $"A: {r.Item.AnswerKorean}\n" +
                $"(카테고리: {r.Item.Category}, 관련도: {r.RelevanceScore:F3})\n");

            var resultText = string.Join("\n", results);
            _logger.LogInformation("Returned {Count} re-ranked FAQs", reranked.Count);

            return resultText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching FAQs for question: {Question}", question);
            return "FAQ 검색 중 오류가 발생했습니다.";
        }
    }
}
