namespace BicycleShopChatbot.Application.DTOs;

public class ChatStreamChunk
{
    public string SessionId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = false;
    public DateTime Timestamp { get; set; }
    public string? Category { get; set; }
}
