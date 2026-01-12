using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        sb.AppendLine("========================================");
        sb.AppendLine("[ 판매 가능한 제품 목록 - 이 목록에만 있는 제품을 추천하세요 ]");
        sb.AppendLine("========================================");

        // Add numbered product list with product codes
        for (int i = 0; i < products.Count; i++)
        {
            var product = products[i];
            sb.AppendLine($"{i + 1}. {product.NameKorean} (제품코드: {product.ProductCode})");
        }

        sb.AppendLine();
        sb.AppendLine("⚠️ 경고 1: 위 목록의 제품 번호와 제품코드를 정확히 확인하세요");
        sb.AppendLine("⚠️ 경고 2: 목록에 없는 제품명은 절대 언급하지 마세요");
        sb.AppendLine("⚠️ 경고 3: 제품을 추천할 때 반드시 제품코드를 함께 표시하세요");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine("[ 제품 상세 정보 ]");
        sb.AppendLine("========================================");
        sb.AppendLine();

        foreach (var product in products)
        {
            sb.AppendLine($"### {product.NameKorean} ({product.Name})");
            sb.AppendLine($"- **제품코드**: {product.ProductCode}");
            sb.AppendLine($"- **카테고리**: {product.Category}");
            sb.AppendLine($"- **브랜드**: {product.Brand}");
            sb.AppendLine($"- **가격**: {product.Price:N0}원");
            sb.AppendLine($"- **재고**: {product.StockQuantity}개");
            sb.AppendLine($"- **설명**: {product.DescriptionKorean}");
            sb.AppendLine();
        }

        sb.AppendLine("========================================");
        sb.AppendLine("[ 필수 준수사항 ]");
        sb.AppendLine("========================================");
        sb.AppendLine("- 추천 시 반드시 \"제품코드: XXX\"를 표시할 것");
        sb.AppendLine($"- 위 번호 목록(1~{products.Count})에 없는 제품은 언급 금지");
        sb.AppendLine("- \"기본 모델\", \"엔트리 모델\" 등 목록에 없는 변형 제품 언급 금지");
        sb.AppendLine("- 제품이 부족하더라도 존재하지 않는 제품을 만들어내지 마세요");
        sb.AppendLine("========================================");

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

        // 주문/배송 관련 (가장 구체적 - 최우선)
        if (lower.Contains("주문") || lower.Contains("배송") ||
            lower.Contains("송장") || lower.Contains("운송장") ||
            lower.Contains("택배") ||
            (lower.Contains("언제") && (lower.Contains("도착") || lower.Contains("배달"))))
        {
            return ChatCategory.OrderStatus;
        }

        // FAQ 관련 (두 번째 우선순위)
        if (lower.Contains("환불") || lower.Contains("교환") ||
            lower.Contains("반품") || lower.Contains("정책") ||
            lower.Contains("as") || lower.Contains("a/s") ||
            lower.Contains("보증") || lower.Contains("취소") ||
            lower.Contains("조립") || lower.Contains("서비스"))
        {
            return ChatCategory.FAQ;
        }

        // 제품 상세 스펙 (ProductSearch보다 우선)
        if (lower.Contains("스펙") || lower.Contains("사양") ||
            lower.Contains("무게") || lower.Contains("크기") ||
            lower.Contains("재질") || lower.Contains("프레임") ||
            lower.Contains("변속") || lower.Contains("기어"))
        {
            return ChatCategory.ProductDetails;
        }

        // 제품 검색/추천 (가장 포괄적 - 많은 케이스 커버)
        if (lower.Contains("추천") || lower.Contains("찾") ||
            lower.Contains("검색") || lower.Contains("구매") ||
            lower.Contains("살") || lower.Contains("구입") ||
            lower.Contains("어떤") || lower.Contains("어느") ||
            lower.Contains("가격") || lower.Contains("얼마") ||
            lower.Contains("재고") || lower.Contains("있나") ||
            lower.Contains("있어") || lower.Contains("판매") ||
            lower.Contains("자전거") || lower.Contains("바이크") ||
            lower.Contains("로드") || lower.Contains("산악") ||
            lower.Contains("mtb") || lower.Contains("하이브리드") ||
            lower.Contains("출퇴근") || lower.Contains("레저") ||
            lower.Contains("예산") || lower.Contains("만원") ||
            lower.Contains("원대") || lower.Contains("원 이하"))
        {
            return ChatCategory.ProductSearch;
        }

        return ChatCategory.General;
    }

    public string? ExtractProductCategory(string userMessage)
    {
        var lower = userMessage.ToLower();

        // 산악 / Mountain
        if (lower.Contains("산악") || lower.Contains("mtb") ||
            lower.Contains("mountain") || lower.Contains("마운틴"))
        {
            return "Mountain";
        }

        // 로드 / Road
        if (lower.Contains("로드") || lower.Contains("road") ||
            lower.Contains("로드바이크") || lower.Contains("경주"))
        {
            return "Road";
        }

        // 하이브리드 / Hybrid
        if (lower.Contains("하이브리드") || lower.Contains("hybrid") ||
            lower.Contains("출퇴근"))
        {
            return "Hybrid";
        }

        // 전기 / Electric
        if (lower.Contains("전기") || lower.Contains("electric") ||
            lower.Contains("이-바이크") || lower.Contains("e-bike") ||
            lower.Contains("전동"))
        {
            return "Electric";
        }

        // 접이식 / Folding
        if (lower.Contains("접이식") || lower.Contains("폴딩") ||
            lower.Contains("folding") || lower.Contains("휴대"))
        {
            return "Folding";
        }

        // 그래블 / Gravel
        if (lower.Contains("그래블") || lower.Contains("gravel") ||
            lower.Contains("어드벤처"))
        {
            return "Gravel";
        }

        // 키즈 / Kids
        if (lower.Contains("어린이") || lower.Contains("키즈") ||
            lower.Contains("kids") || lower.Contains("주니어") ||
            lower.Contains("아이"))
        {
            return "Kids";
        }

        return null; // 카테고리 명시 없음
    }

    public (decimal? MinPrice, decimal? MaxPrice) ExtractPriceRange(string userMessage)
    {
        var lower = userMessage.ToLower();
        decimal? minPrice = null;
        decimal? maxPrice = null;

        // Pattern 1: "X만원 이하" / "X만원 이내" / "X만원 미만"
        var belowMatch = Regex.Match(lower, @"(\d+)만원\s*(이하|이내|미만)");
        if (belowMatch.Success)
        {
            maxPrice = decimal.Parse(belowMatch.Groups[1].Value) * 10000;
            if (belowMatch.Groups[2].Value == "미만")
            {
                maxPrice -= 1; // 미만은 해당 값 미포함
            }
            return (null, maxPrice);
        }

        // Pattern 2: "X만원 이상" / "X만원 초과"
        var aboveMatch = Regex.Match(lower, @"(\d+)만원\s*(이상|초과)");
        if (aboveMatch.Success)
        {
            minPrice = decimal.Parse(aboveMatch.Groups[1].Value) * 10000;
            if (aboveMatch.Groups[2].Value == "초과")
            {
                minPrice += 1; // 초과는 해당 값 미포함
            }
            return (minPrice, null);
        }

        // Pattern 3: "X만원~Y만원" / "X만원에서 Y만원" / "X만원부터 Y만원"
        var rangeMatch = Regex.Match(lower, @"(\d+)만원\s*[~\-에서부터]+\s*(\d+)만원");
        if (rangeMatch.Success)
        {
            minPrice = decimal.Parse(rangeMatch.Groups[1].Value) * 10000;
            maxPrice = decimal.Parse(rangeMatch.Groups[2].Value) * 10000;
            return (minPrice, maxPrice);
        }

        // Pattern 4: "X만원대" (예: 100만원대 = 1,000,000 ~ 1,999,999)
        var approximateMatch = Regex.Match(lower, @"(\d+)만원대");
        if (approximateMatch.Success)
        {
            minPrice = decimal.Parse(approximateMatch.Groups[1].Value) * 10000;
            maxPrice = minPrice + 999999; // 다음 백만 미만
            return (minPrice, maxPrice);
        }

        return (null, null); // 가격 범위 명시 없음
    }

    public string? ExtractProductName(string userMessage)
    {
        var lower = userMessage.ToLower();

        // Korean product names
        var koreanProducts = new Dictionary<string, string>
        {
            { "스피드스터", "스피드스터 프로 카본" },
            { "시티 커뮤터", "시티 커뮤터 디럭스" },
            { "마운틴 익스플로러", "마운틴 익스플로러 XT" },
            { "트레일 블레이저", "트레일 블레이저 프로" },
            { "어드벤처 시커", "어드벤처 시커" },
            { "컴팩트 폴더", "컴팩트 폴더" },
            { "시티 이-커뮤터", "시티 이-커뮤터" },
            { "이커뮤터", "시티 이-커뮤터" }, // 띄어쓰기 없는 경우
            { "주니어 레이서", "주니어 레이서" },
            { "에어로 스프린트", "에어로 스프린트 엘리트" },
            { "컴포트 크루저", "컴포트 크루저" }
        };

        // English product names
        var englishProducts = new Dictionary<string, string>
        {
            { "speedster", "스피드스터 프로 카본" },
            { "city commuter", "시티 커뮤터 디럭스" },
            { "mountain explorer", "마운틴 익스플로러 XT" },
            { "trail blazer", "트레일 블레이저 프로" },
            { "adventure seeker", "어드벤처 시커" },
            { "compact folder", "컴팩트 폴더" },
            { "city e-commuter", "시티 이-커뮤터" },
            { "junior racer", "주니어 레이서" },
            { "aero sprint", "에어로 스프린트 엘리트" },
            { "comfort cruiser", "컴포트 크루저" }
        };

        // Check Korean names first
        foreach (var (keyword, productName) in koreanProducts)
        {
            if (lower.Contains(keyword.ToLower()))
            {
                return productName;
            }
        }

        // Check English names
        foreach (var (keyword, productName) in englishProducts)
        {
            if (lower.Contains(keyword))
            {
                return productName;
            }
        }

        return null; // 상품명 명시 없음
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
3. 제품 목록 엄수: 절대로 위에 제공된 제품 목록에 없는 제품을 언급하거나 추천하지 마세요.
   - 제공된 제품만 소개할 수 있습니다
   - 존재하지 않는 제품명을 만들어내지 마세요
   - 제품이 목록에 없으면 ""해당 제품은 현재 재고가 없습니다""라고 명확히 말하세요
4. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내 금액의 상품만 소개하세요.
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

    public string GetNoProductsFoundPrompt(string query)
    {
        return $@"당신은 대한민국의 자전거 전문 온라인 쇼핑몰 AI 상담원입니다.

고객이 문의한 제품: ""{query}""

========================================
[ 중요 안내 ]
========================================
현재 해당 제품은 재고가 없거나 판매하지 않는 제품입니다.

고객에게 다음과 같이 안내하세요:
1. ""죄송합니다. '{query}'는 현재 재고가 없거나 취급하지 않는 제품입니다.""
2. 고객의 용도(출퇴근, 운동, 레저 등)를 물어보세요
3. 비슷한 제품이 필요하면 고객센터(1588-0000)로 안내하세요

절대로 존재하지 않는 제품명을 언급하지 마세요!
========================================";
    }

    public double GetTemperatureForCategory(ChatCategory category)
    {
        return category switch
        {
            ChatCategory.ProductSearch => 0.3,    // Factual, less hallucination
            ChatCategory.ProductDetails => 0.3,   // Factual specs only
            ChatCategory.OrderStatus => 0.2,      // Extremely factual
            ChatCategory.FAQ => 0.4,              // Slightly creative for explanations
            _ => 0.7                               // Default for general chat
        };
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
