using System.Text;
using System.Text.Json;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Domain.Enums;

namespace BicycleShopChatbot.Application.Services;

public class PromptService : IPromptService
{
    public string GetSystemPrompt(ChatCategory category)
    {
        return category switch
        {
            ChatCategory.ProductSearch => GetProductSearchSystemPrompt(),
            ChatCategory.ProductDetails => GetProductDetailsSystemPrompt(),
            ChatCategory.FAQ => GetFaqSystemPrompt(),
            ChatCategory.OrderStatus => GetOrderStatusSystemPrompt(),
            ChatCategory.CustomerSupport => GetCustomerSupportSystemPrompt(),
            _ => GetGeneralSystemPrompt()
        };
    }

    public string GetProductSearchPrompt(string query, List<Product> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GetProductSearchSystemPrompt());
        sb.AppendLine();
        sb.AppendLine("## 현재 판매 중인 제품:");
        sb.AppendLine();

        foreach (var product in products)
        {
            sb.AppendLine($"### {product.NameKorean} ({product.Name})");
            sb.AppendLine($"- **카테고리**: {product.Category}");
            sb.AppendLine($"- **브랜드**: {product.Brand}");
            sb.AppendLine($"- **가격**: {product.Price:N0}원");
            sb.AppendLine($"- **재고**: {product.StockQuantity}개");
            sb.AppendLine($"- **설명**: {product.DescriptionKorean}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string GetOrderStatusPrompt(string orderNumber, Order? order)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GetOrderStatusSystemPrompt());
        sb.AppendLine();

        if (order != null)
        {
            sb.AppendLine("## 주문 정보:");
            sb.AppendLine($"- **주문번호**: {order.OrderNumber}");
            sb.AppendLine($"- **주문일**: {order.OrderDate:yyyy년 MM월 dd일}");
            sb.AppendLine($"- **상태**: {GetOrderStatusKorean(order.Status)}");
            sb.AppendLine($"- **금액**: {order.TotalAmount:N0}원");

            if (!string.IsNullOrEmpty(order.TrackingNumber))
            {
                sb.AppendLine($"- **송장번호**: {order.TrackingNumber}");
            }

            if (order.EstimatedDelivery.HasValue)
            {
                sb.AppendLine($"- **배송 예정일**: {order.EstimatedDelivery.Value:yyyy년 MM월 dd일}");
            }
        }
        else
        {
            sb.AppendLine($"주문번호 '{orderNumber}'에 해당하는 주문을 찾을 수 없습니다.");
            sb.AppendLine("주문번호를 다시 확인해주시거나, 고객센터(1588-0000)로 문의해주세요.");
        }

        return sb.ToString();
    }

    public string GetFaqPrompt(string query, List<FAQ> relevantFaqs)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GetFaqSystemPrompt());
        sb.AppendLine();

        if (relevantFaqs.Any())
        {
            sb.AppendLine("## 관련 FAQ:");
            sb.AppendLine();

            foreach (var faq in relevantFaqs)
            {
                sb.AppendLine($"**Q: {faq.QuestionKorean}**");
                sb.AppendLine($"A: {faq.AnswerKorean}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("관련 FAQ를 찾을 수 없습니다. 일반적인 정보로 답변해주세요.");
        }

        return sb.ToString();
    }

    public ChatCategory DetectIntent(string userMessage)
    {
        var lower = userMessage.ToLower();

        if (lower.Contains("주문") || lower.Contains("배송") ||
            lower.Contains("송장") || lower.Contains("언제") && (lower.Contains("도착") || lower.Contains("배달")))
        {
            return ChatCategory.OrderStatus;
        }

        if (lower.Contains("추천") || lower.Contains("찾") ||
            lower.Contains("어떤") || lower.Contains("자전거") ||
            lower.Contains("얼마") && lower.Contains("?"))
        {
            return ChatCategory.ProductSearch;
        }

        if (lower.Contains("환불") || lower.Contains("교환") ||
            lower.Contains("반품") || lower.Contains("정책") ||
            lower.Contains("as") || lower.Contains("보증") ||
            lower.Contains("취소"))
        {
            return ChatCategory.FAQ;
        }

        if (lower.Contains("스펙") || lower.Contains("사양") ||
            lower.Contains("무게") || lower.Contains("크기") ||
            lower.Contains("재질"))
        {
            return ChatCategory.ProductDetails;
        }

        return ChatCategory.General;
    }

    private string GetProductSearchSystemPrompt()
    {
        return @"당신은 대한민국의 자전거 전문 온라인 쇼핑몰 AI 상담원입니다.
고객이 원하는 자전거를 찾도록 도와주는 것이 목표입니다.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
   - 예시: 예산 100만원 → 최대 110만원까지만 추천 가능
   - 예산을 초과하는 상품은 절대 추천하지 마세요
========================================

답변 규칙:
1. 제품의 특징과 장점을 구체적으로 설명하세요
2. 가격과 재고 상태를 정확히 전달하세요
3. 고객의 용도(출퇴근, 운동, 레저)를 고려하여 추천하세요
4. 2~3개 제품을 비교하여 제시하면 좋습니다
5. 고객이 예산을 명시한 경우:
   - 예산 범위 내(+10% 이내) 상품만 추천하세요
   - 예산 초과 상품은 절대 언급하지 마세요
   - 적절한 상품이 없다면 ""예산 범위 내 적합한 상품이 없습니다""라고 솔직히 말하세요
6. 제품이 없는 경우, 대안을 제시하거나 고객센터(1588-0000) 안내를 해주세요
7. 모든 답변은 친절하고 전문적인 한국어로 작성하세요";
    }

    private string GetProductDetailsSystemPrompt()
    {
        return @"당신은 대한민국의 자전거 제품 전문가입니다.
제품의 상세 스펙과 기술적인 정보를 명확하게 설명해주세요.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
========================================

답변 규칙:
1. 정확한 스펙 정보를 한국어로 제공하세요
2. 기술 용어는 일반인도 이해할 수 있게 설명하세요
3. 다른 제품과의 차이점을 명확히 하세요
4. 가격이 언급될 경우 고객의 예산을 고려하세요";
    }

    private string GetFaqSystemPrompt()
    {
        return @"당신은 대한민국 자전거 쇼핑몰의 고객 지원 AI입니다.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
========================================

답변 규칙:
1. 정확한 정보만 한국어로 제공하세요
2. FAQ에 없는 내용은 ""고객센터(1588-0000)로 문의해주세요""라고 안내하세요
3. 배송, 반품, 교환 정책을 명확히 설명하세요
4. 친절하고 공손한 톤을 유지하세요";
    }

    private string GetOrderStatusSystemPrompt()
    {
        return @"당신은 대한민국 자전거 쇼핑몰의 주문 조회 전문 AI 상담원입니다.
고객의 주문번호를 확인하여 배송 상태를 안내해주세요.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
========================================

답변 규칙:
1. 주문 정보를 명확하고 간결하게 한국어로 전달하세요
2. 배송 지연 등의 문제가 있으면 정중히 사과하고 대응 방안을 안내하세요
3. 추가 문의사항이 있으면 고객센터 연락처(1588-0000)를 안내하세요";
    }

    private string GetCustomerSupportSystemPrompt()
    {
        return @"당신은 대한민국 자전거 쇼핑몰의 고객 지원 AI입니다.
고객의 문제를 친절하게 해결해주세요.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
========================================

답변 규칙:
1. 고객의 불편사항에 공감하고 정중히 대응하세요
2. 해결 방법을 단계별로 한국어로 안내하세요
3. 복잡한 문제는 고객센터(1588-0000)로 안내하세요";
    }

    private string GetGeneralSystemPrompt()
    {
        return @"당신은 대한민국 자전거 온라인 쇼핑몰의 AI 상담원입니다.
고객을 친절하게 맞이하고, 필요한 도움을 제공해주세요.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요. 중국어, 영어, 일본어 등 다른 언어는 절대 사용하지 마세요.
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
========================================

답변 규칙:
1. 친절하고 전문적인 한국어 톤을 유지하세요
2. 고객의 질문 의도를 파악하고 적절히 대응하세요
3. 예산이 언급되면 반드시 예산 범위 내에서만 상품을 추천하세요";
    }

    private string GetOrderStatusKorean(string status)
    {
        return status.ToLower() switch
        {
            "pending" => "주문 접수",
            "processing" => "처리 중",
            "shipped" => "배송 중",
            "delivered" => "배송 완료",
            "cancelled" => "주문 취소",
            _ => status
        };
    }
}
