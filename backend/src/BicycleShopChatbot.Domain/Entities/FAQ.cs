namespace BicycleShopChatbot.Domain.Entities;

public class FAQ
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string QuestionKorean { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string AnswerKorean { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Keywords { get; set; }
    public int ViewCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
