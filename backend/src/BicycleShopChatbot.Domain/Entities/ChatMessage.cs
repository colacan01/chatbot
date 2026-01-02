using BicycleShopChatbot.Domain.Enums;

namespace BicycleShopChatbot.Domain.Entities;

public class ChatMessage
{
    public long Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public ChatCategory? Category { get; set; }
    public string? IntentDetected { get; set; }
    public int? ProductId { get; set; }
    public int? OrderId { get; set; }
    public string? Metadata { get; set; }
    public int? TokensUsed { get; set; }
    public int? ProcessingTimeMs { get; set; }

    public ChatSession ChatSession { get; set; } = null!;
    public Product? Product { get; set; }
    public Order? Order { get; set; }
}
