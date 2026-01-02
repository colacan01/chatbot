using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Infrastructure.Seed;

public static class FaqSeedData
{
    public static List<FAQ> GetSeedFaqs()
    {
        var now = DateTime.UtcNow;

        return new List<FAQ>
        {
            new FAQ
            {
                Question = "What is your return policy?",
                QuestionKorean = "반품 정책이 어떻게 되나요?",
                Answer = "You can return unopened products within 30 days of purchase.",
                AnswerKorean = "구매 후 30일 이내 미개봉 제품에 한해 반품이 가능합니다. 개봉한 제품은 하자가 있는 경우에만 교환 가능합니다.",
                Category = "반품/교환",
                Keywords = "반품,환불,교환,30일,정책",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "How long does delivery take?",
                QuestionKorean = "배송은 얼마나 걸리나요?",
                Answer = "Standard delivery takes 2-3 business days.",
                AnswerKorean = "일반 배송은 주문 후 2-3 영업일 소요됩니다. 도서산간 지역은 1-2일 추가 소요될 수 있습니다.",
                Category = "배송",
                Keywords = "배송,택배,기간,얼마,언제",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you offer assembly service?",
                QuestionKorean = "조립 서비스를 제공하나요?",
                Answer = "Yes, we offer professional assembly service for an additional fee.",
                AnswerKorean = "네, 전문 조립 서비스를 유료로 제공합니다. 자전거 구매 시 조립 서비스를 선택하시면 완조립 상태로 배송됩니다. 비용은 5만원입니다.",
                Category = "서비스",
                Keywords = "조립,설치,서비스,완조립",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "What is the warranty period?",
                QuestionKorean = "보증 기간은 얼마나 되나요?",
                Answer = "All bikes come with a 2-year warranty on the frame and 1-year on components.",
                AnswerKorean = "모든 자전거는 프레임 2년, 부품 1년 무상 보증이 제공됩니다. 정상적인 사용 중 발생한 제조 결함에 한해 보증 서비스를 받으실 수 있습니다.",
                Category = "보증/AS",
                Keywords = "보증,워런티,as,기간,2년",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Can I test ride before buying?",
                QuestionKorean = "구매 전에 시승이 가능한가요?",
                Answer = "Yes, you can test ride at our showroom by appointment.",
                AnswerKorean = "네, 쇼룸에서 시승이 가능합니다. 사전 예약을 하시면 전문 상담사와 함께 시승하실 수 있습니다. 예약은 고객센터(1588-0000)로 연락주세요.",
                Category = "구매",
                Keywords = "시승,테스트,체험,쇼룸",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you ship nationwide?",
                QuestionKorean = "전국 배송이 가능한가요?",
                Answer = "Yes, we ship to all regions in South Korea.",
                AnswerKorean = "네, 전국 어디든 배송 가능합니다. 제주도 및 도서산간 지역은 추가 배송비가 발생할 수 있습니다.",
                Category = "배송",
                Keywords = "배송,전국,제주도,지역",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "What payment methods do you accept?",
                QuestionKorean = "어떤 결제 수단을 사용할 수 있나요?",
                Answer = "We accept credit cards, bank transfer, and installment plans.",
                AnswerKorean = "신용카드, 계좌이체, 무이자 할부를 지원합니다. 카드 무이자 할부는 2-12개월까지 가능하며, 카드사별로 할부 조건이 다를 수 있습니다.",
                Category = "결제",
                Keywords = "결제,카드,할부,계좌이체",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "How do I track my order?",
                QuestionKorean = "주문 배송 조회는 어떻게 하나요?",
                Answer = "You can track your order using the tracking number sent via email.",
                AnswerKorean = "주문 시 등록하신 이메일로 발송된 송장번호로 배송 조회가 가능합니다. 또는 마이페이지에서 주문 내역을 확인하실 수 있습니다.",
                Category = "배송",
                Keywords = "조회,추적,송장번호,배송",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you offer bike maintenance services?",
                QuestionKorean = "자전거 정비 서비스를 제공하나요?",
                Answer = "Yes, we have a service center for repairs and maintenance.",
                AnswerKorean = "네, 자체 서비스센터에서 수리 및 정기 점검 서비스를 제공합니다. 구매 후 첫 1년간은 무상 점검 서비스를 받으실 수 있습니다.",
                Category = "서비스",
                Keywords = "정비,수리,점검,서비스센터",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "What is included in the bike package?",
                QuestionKorean = "자전거 구매 시 기본 포함 품목은 무엇인가요?",
                Answer = "Basic package includes the bike, pedals, user manual, and basic tools.",
                AnswerKorean = "자전거 본체, 페달, 사용 설명서, 기본 공구 세트가 포함됩니다. 별도로 라이트, 자물쇠, 헬멧 등의 액세서리를 추가 구매하실 수 있습니다.",
                Category = "구매",
                Keywords = "포함,구성품,패키지,기본",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Can I cancel my order?",
                QuestionKorean = "주문 취소가 가능한가요?",
                Answer = "You can cancel your order before shipping.",
                AnswerKorean = "배송 시작 전까지 주문 취소가 가능합니다. 이미 배송이 시작된 경우에는 제품 수령 후 반품 절차를 진행하셔야 합니다.",
                Category = "주문",
                Keywords = "취소,주문취소,환불",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you sell spare parts?",
                QuestionKorean = "부품만 따로 구매할 수 있나요?",
                Answer = "Yes, we sell various spare parts and accessories.",
                AnswerKorean = "네, 타이어, 브레이크 패드, 체인 등 다양한 부품과 액세서리를 판매합니다. 필요하신 부품은 고객센터로 문의해주세요.",
                Category = "부품",
                Keywords = "부품,교체,액세서리,구매",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Is there a size guide?",
                QuestionKorean = "자전거 사이즈 가이드가 있나요?",
                Answer = "Yes, each product page has a detailed size chart.",
                AnswerKorean = "네, 각 제품 페이지에 상세한 사이즈 차트가 제공됩니다. 신장에 맞는 프레임 사이즈를 선택하시면 됩니다. 궁금하신 점은 상담사에게 문의해주세요.",
                Category = "구매",
                Keywords = "사이즈,크기,프레임,선택",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "What if the product is defective?",
                QuestionKorean = "제품에 하자가 있으면 어떻게 하나요?",
                Answer = "Contact customer service immediately for a replacement or refund.",
                AnswerKorean = "즉시 고객센터(1588-0000)로 연락주시면 교환 또는 환불 처리해드립니다. 제품 하자로 인한 교환/환불 시 왕복 배송비는 당사가 부담합니다.",
                Category = "보증/AS",
                Keywords = "하자,불량,교환,환불",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you offer gift wrapping?",
                QuestionKorean = "선물 포장 서비스가 있나요?",
                Answer = "Yes, we offer free gift wrapping upon request.",
                AnswerKorean = "네, 무료 선물 포장 서비스를 제공합니다. 주문 시 선물 포장 옵션을 선택해주시면 됩니다.",
                Category = "서비스",
                Keywords = "선물,포장,gift",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "How do I contact customer service?",
                QuestionKorean = "고객센터 연락처가 어떻게 되나요?",
                Answer = "Call 1588-0000 or email support@bicycleshop.com",
                AnswerKorean = "전화: 1588-0000 (평일 09:00-18:00), 이메일: support@bicycleshop.com으로 연락주세요. 카카오톡 채널에서도 상담 가능합니다.",
                Category = "고객지원",
                Keywords = "연락처,전화,이메일,고객센터",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Can I modify my order after placing it?",
                QuestionKorean = "주문 후 수정이 가능한가요?",
                Answer = "You can modify your order before shipping starts.",
                AnswerKorean = "배송 시작 전까지는 주문 내역 수정이 가능합니다. 마이페이지에서 직접 수정하시거나 고객센터로 연락주세요.",
                Category = "주문",
                Keywords = "수정,변경,주문변경",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you offer student discounts?",
                QuestionKorean = "학생 할인이 있나요?",
                Answer = "Yes, we offer 10% discount for students with valid ID.",
                AnswerKorean = "네, 학생증 제시 시 10% 할인을 제공합니다. 온라인 구매 시에는 고객센터로 학생증 사본을 보내주시면 할인 쿠폰을 발급해드립니다.",
                Category = "할인/이벤트",
                Keywords = "학생,할인,쿠폰",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Is bicycle insurance available?",
                QuestionKorean = "자전거 보험 가입이 가능한가요?",
                Answer = "Yes, we partner with insurance companies for bicycle insurance.",
                AnswerKorean = "네, 제휴 보험사를 통해 자전거 보험 가입이 가능합니다. 도난 및 사고에 대비할 수 있으며, 자세한 내용은 상담사에게 문의해주세요.",
                Category = "서비스",
                Keywords = "보험,도난,사고",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            },
            new FAQ
            {
                Question = "Do you have a loyalty program?",
                QuestionKorean = "멤버십이나 포인트 적립 제도가 있나요?",
                Answer = "Yes, you earn points with every purchase that can be used for future orders.",
                AnswerKorean = "네, 구매 금액의 3%를 포인트로 적립해드립니다. 적립된 포인트는 다음 구매 시 현금처럼 사용하실 수 있습니다.",
                Category = "할인/이벤트",
                Keywords = "포인트,적립,멤버십,혜택",
                ViewCount = 0,
                IsActive = true,
                CreatedAt = now
            }
        };
    }
}
