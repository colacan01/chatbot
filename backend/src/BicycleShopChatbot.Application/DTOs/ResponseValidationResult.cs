namespace BicycleShopChatbot.Application.DTOs;

public class ResponseValidationResult
{
    public string OriginalResponse { get; set; } = string.Empty;
    public string CleanedResponse { get; set; } = string.Empty;
    public List<string> ValidCodes { get; set; } = new();
    public List<string> InvalidCodes { get; set; } = new();
    public bool HasModifications => InvalidCodes.Any();
}
