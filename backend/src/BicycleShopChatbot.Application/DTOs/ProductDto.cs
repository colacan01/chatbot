namespace BicycleShopChatbot.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameKorean { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKorean { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public string? ImageUrl { get; set; }
}
