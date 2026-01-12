namespace VectorDataLoader.Models;

public class Product
{
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameKorean { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DescriptionKorean { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    // AI가 생성할 상세 설명
    public string DetailedDescription { get; set; } = string.Empty;
}
