using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Domain.Enums;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IPromptService
{
    string GetSystemPrompt(ChatCategory category);

    string GetProductSearchPrompt(string query, List<Product> products);

    string GetOrderStatusPrompt(string orderNumber, Order? order);

    string GetFaqPrompt(string query, List<FAQ> relevantFaqs);

    string GetNoProductsFoundPrompt(string query);

    ChatCategory DetectIntent(string userMessage);

    string? ExtractProductCategory(string userMessage);

    (decimal? MinPrice, decimal? MaxPrice) ExtractPriceRange(string userMessage);

    string? ExtractProductName(string userMessage);

    double GetTemperatureForCategory(ChatCategory category);
}
