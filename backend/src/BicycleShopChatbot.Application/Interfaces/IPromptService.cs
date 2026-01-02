using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Domain.Enums;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IPromptService
{
    string GetSystemPrompt(ChatCategory category);

    string GetProductSearchPrompt(string query, List<Product> products);

    string GetOrderStatusPrompt(string orderNumber, Order? order);

    string GetFaqPrompt(string query, List<FAQ> relevantFaqs);

    ChatCategory DetectIntent(string userMessage);
}
