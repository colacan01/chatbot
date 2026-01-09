namespace BicycleShopChatbot.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public string? SessionMetadata { get; set; }
    public int TotalMessages { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
