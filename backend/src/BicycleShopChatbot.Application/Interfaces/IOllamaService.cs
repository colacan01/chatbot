using BicycleShopChatbot.Application.DTOs;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IOllamaService
{
    Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessageDto> conversationHistory,
        string systemPrompt,
        double temperature,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> GenerateResponseStreamAsync(
        string userMessage,
        List<ChatMessageDto> conversationHistory,
        string systemPrompt,
        double temperature,
        CancellationToken cancellationToken = default);

    Task<bool> IsModelAvailableAsync(CancellationToken cancellationToken = default);
}
