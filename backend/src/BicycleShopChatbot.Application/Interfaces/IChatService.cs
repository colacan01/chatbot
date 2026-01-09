using BicycleShopChatbot.Application.DTOs;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IChatService
{
    Task<ChatMessageDto> ProcessUserMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatStreamChunk> ProcessUserMessageStreamAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<List<ChatMessageDto>> GetConversationHistoryAsync(
        string sessionId,
        int messageCount = 50,
        CancellationToken cancellationToken = default);

    Task<ChatSessionDto> GetOrCreateSessionAsync(
        string sessionId,
        int? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    // Session management methods
    Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ChatSessionDto> LoadSessionHistoryAsync(
        string sessionId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(
        string sessionId,
        int userId,
        CancellationToken cancellationToken = default);
}
