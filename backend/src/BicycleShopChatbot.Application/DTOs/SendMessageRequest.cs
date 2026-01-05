namespace BicycleShopChatbot.Application.DTOs;

public class SendMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
}
