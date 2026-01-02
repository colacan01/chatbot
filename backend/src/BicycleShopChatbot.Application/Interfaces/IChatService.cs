using BicycleShopChatbot.Application.DTOs;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IChatService
{
    Task<ChatMessageDto> ProcessUserMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetConversationHistoryAsync(
        string sessionId,
        int messageCount = 50,
        CancellationToken cancellationToken = default);

    Task<ChatSessionDto> GetOrCreateSessionAsync(
        string sessionId,
        string? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);
}
