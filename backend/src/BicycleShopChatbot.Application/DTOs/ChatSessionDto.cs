namespace BicycleShopChatbot.Application.DTOs;

public class ChatSessionDto
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalMessages { get; set; }
    public List<ChatMessageDto> RecentMessages { get; set; } = new();
}
