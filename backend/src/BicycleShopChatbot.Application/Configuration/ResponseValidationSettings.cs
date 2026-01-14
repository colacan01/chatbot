namespace BicycleShopChatbot.Application.Configuration;

public class ResponseValidationSettings
{
    public bool Enabled { get; set; } = true;
    public ValidationStrategy Strategy { get; set; } = ValidationStrategy.Remove;
    public bool LogInvalidCodes { get; set; } = true;
}

public enum ValidationStrategy
{
    Remove,
    Warn
}
