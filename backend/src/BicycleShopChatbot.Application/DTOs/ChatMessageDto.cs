namespace BicycleShopChatbot.Application.DTOs;

public class ChatMessageDto
{
    public long Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Category { get; set; }
    public ProductDto? RelatedProduct { get; set; }
    public OrderDto? RelatedOrder { get; set; }
}
