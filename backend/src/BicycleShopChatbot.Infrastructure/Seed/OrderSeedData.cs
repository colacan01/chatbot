using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Infrastructure.Seed;

public static class OrderSeedData
{
    public static List<Order> GetSeedOrders()
    {
        var now = DateTime.UtcNow;

        return new List<Order>
        {
            new Order
            {
                OrderNumber = "ORD20250102001",
                CustomerEmail = "customer1@example.com",
                CustomerPhone = "010-1234-5678",
                Status = "Shipped",
                OrderDate = now.AddDays(-3),
                TotalAmount = 3500000,
                ShippingAddress = "서울시 강남구 테헤란로 123",
                TrackingNumber = "CJ1234567890",
                EstimatedDelivery = now.AddDays(1),
                UpdatedAt = now
            },
            new Order
            {
                OrderNumber = "ORD20250101002",
                CustomerEmail = "customer2@example.com",
                CustomerPhone = "010-9876-5432",
                Status = "Processing",
                OrderDate = now.AddDays(-1),
                TotalAmount = 2800000,
                ShippingAddress = "부산시 해운대구 마린시티 456",
                TrackingNumber = null,
                EstimatedDelivery = now.AddDays(3),
                UpdatedAt = now
            },
            new Order
            {
                OrderNumber = "ORD20241230003",
                CustomerEmail = "customer3@example.com",
                CustomerPhone = "010-5555-6666",
                Status = "Delivered",
                OrderDate = now.AddDays(-5),
                TotalAmount = 890000,
                ShippingAddress = "대전시 유성구 대학로 789",
                TrackingNumber = "CJ0987654321",
                EstimatedDelivery = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            }
        };
    }
}
