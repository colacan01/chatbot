using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Infrastructure.Seed;

public static class ProductSeedData
{
    public static List<Product> GetSeedProducts()
    {
        var now = DateTime.UtcNow;

        return new List<Product>
        {
            new Product
            {
                ProductCode = "ROAD-001",
                Name = "Speedster Pro Carbon",
                NameKorean = "스피드스터 프로 카본",
                Category = "Road",
                Brand = "VeloTech",
                Price = 3500000,
                Description = "Professional racing road bike with carbon frame",
                DescriptionKorean = "프로페셔널 레이싱을 위한 카본 프레임 로드 바이크입니다. 초경량 설계로 최고의 속도를 자랑합니다.",
                Specifications = "{\"프레임\":\"카본 섬유\",\"무게\":\"6.8kg\",\"기어\":\"시마노 105 22단\",\"휠\":\"700c 카본\"}",
                StockQuantity = 15,
                IsAvailable = true,
                ImageUrl = "/images/speedster-pro.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "MTB-001",
                Name = "Mountain Explorer XT",
                NameKorean = "마운틴 익스플로러 XT",
                Category = "Mountain",
                Brand = "TrailMaster",
                Price = 2800000,
                Description = "Full suspension mountain bike for extreme trails",
                DescriptionKorean = "풀 서스펜션 장착 산악용 자전거입니다. 험난한 산악 지형도 거뜬히 주행 가능합니다.",
                Specifications = "{\"프레임\":\"알루미늄 합금\",\"무게\":\"13.5kg\",\"기어\":\"시마노 Deore XT 24단\",\"서스펜션\":\"RockShox 전후 120mm\"}",
                StockQuantity = 8,
                IsAvailable = true,
                ImageUrl = "/images/mountain-explorer.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "HYBRID-001",
                Name = "City Commuter Deluxe",
                NameKorean = "시티 커뮤터 디럭스",
                Category = "Hybrid",
                Brand = "UrbanRide",
                Price = 890000,
                Description = "Comfortable hybrid bike for city commuting",
                DescriptionKorean = "출퇴근용 하이브리드 자전거입니다. 편안한 승차감과 실용성을 겸비했습니다.",
                Specifications = "{\"프레임\":\"알루미늄\",\"무게\":\"12.0kg\",\"기어\":\"시마노 Altus 21단\",\"타이어\":\"700x35c\"}",
                StockQuantity = 25,
                IsAvailable = true,
                ImageUrl = "/images/city-commuter.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "EBIKE-001",
                Name = "E-Power Cruiser",
                NameKorean = "이파워 크루저",
                Category = "Electric",
                Brand = "EcoRide",
                Price = 4200000,
                Description = "Electric bike with 80km range and pedal assist",
                DescriptionKorean = "80km 주행 가능한 전기 자전거입니다. 페달 어시스트 기능으로 편하게 장거리 이동이 가능합니다.",
                Specifications = "{\"프레임\":\"알루미늄\",\"무게\":\"22kg\",\"배터리\":\"500Wh 리튬이온\",\"모터\":\"250W 중앙구동\",\"최대속도\":\"25km/h\"}",
                StockQuantity = 5,
                IsAvailable = true,
                ImageUrl = "/images/epower-cruiser.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "ROAD-002",
                Name = "Aero Sprint Elite",
                NameKorean = "에어로 스프린트 엘리트",
                Category = "Road",
                Brand = "VeloTech",
                Price = 4500000,
                Description = "Aerodynamic racing bike for professional cyclists",
                DescriptionKorean = "공기역학적 설계의 프로 사이클리스트용 레이싱 바이크입니다.",
                Specifications = "{\"프레임\":\"초경량 카본\",\"무게\":\"6.5kg\",\"기어\":\"시마노 Ultegra Di2 22단\",\"휠\":\"디스크 브레이크 카본\"}",
                StockQuantity = 3,
                IsAvailable = true,
                ImageUrl = "/images/aero-sprint.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "MTB-002",
                Name = "Trail Blazer Pro",
                NameKorean = "트레일 블레이저 프로",
                Category = "Mountain",
                Brand = "TrailMaster",
                Price = 1950000,
                Description = "Entry-level mountain bike with front suspension",
                DescriptionKorean = "입문자용 산악 자전거입니다. 프론트 서스펜션으로 편안한 주행을 제공합니다.",
                Specifications = "{\"프레임\":\"알루미늄\",\"무게\":\"14.2kg\",\"기어\":\"시마노 Alivio 24단\",\"서스펜션\":\"전방 100mm\"}",
                StockQuantity = 12,
                IsAvailable = true,
                ImageUrl = "/images/trail-blazer.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "FOLD-001",
                Name = "Compact Folder",
                NameKorean = "컴팩트 폴더",
                Category = "Folding",
                Brand = "UrbanRide",
                Price = 650000,
                Description = "Compact folding bike perfect for public transport",
                DescriptionKorean = "대중교통 이용에 최적화된 접이식 자전거입니다. 간편하게 접어서 휴대 가능합니다.",
                Specifications = "{\"프레임\":\"알루미늄\",\"무게\":\"11.5kg\",\"기어\":\"시마노 7단\",\"휠\":\"20인치\",\"접이크기\":\"65x35x60cm\"}",
                StockQuantity = 30,
                IsAvailable = true,
                ImageUrl = "/images/compact-folder.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "KIDS-001",
                Name = "Junior Racer",
                NameKorean = "주니어 레이서",
                Category = "Kids",
                Brand = "KidsCycle",
                Price = 350000,
                Description = "Safe and fun bike for children aged 6-10",
                DescriptionKorean = "6-10세 어린이용 자전거입니다. 안전하고 재미있게 라이딩을 배울 수 있습니다.",
                Specifications = "{\"프레임\":\"강화 스틸\",\"무게\":\"9.5kg\",\"기어\":\"싱글스피드\",\"휠\":\"20인치\",\"보조바퀴\":\"탈부착 가능\"}",
                StockQuantity = 20,
                IsAvailable = true,
                ImageUrl = "/images/junior-racer.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "GRAVEL-001",
                Name = "Adventure Seeker",
                NameKorean = "어드벤처 시커",
                Category = "Gravel",
                Brand = "VeloTech",
                Price = 3200000,
                Description = "Versatile gravel bike for road and off-road adventures",
                DescriptionKorean = "도로와 비포장 도로 모두 주행 가능한 그래블 바이크입니다. 다양한 지형에서 최고의 성능을 발휘합니다.",
                Specifications = "{\"프레임\":\"카본\",\"무게\":\"9.2kg\",\"기어\":\"시마노 GRX 22단\",\"타이어\":\"700x40c\",\"브레이크\":\"유압 디스크\"}",
                StockQuantity = 7,
                IsAvailable = true,
                ImageUrl = "/images/adventure-seeker.jpg",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                ProductCode = "EBIKE-002",
                Name = "City E-Commuter",
                NameKorean = "시티 이-커뮤터",
                Category = "Electric",
                Brand = "EcoRide",
                Price = 2800000,
                Description = "Affordable electric bike for daily commuting",
                DescriptionKorean = "출퇴근용 전기 자전거입니다. 합리적인 가격으로 편안한 통근이 가능합니다.",
                Specifications = "{\"프레임\":\"알루미늄\",\"무게\":\"20kg\",\"배터리\":\"400Wh\",\"모터\":\"250W 후륜구동\",\"주행거리\":\"60km\"}",
                StockQuantity = 10,
                IsAvailable = true,
                ImageUrl = "/images/city-ecommuter.jpg",
                CreatedAt = now,
                UpdatedAt = now
            }
        };
    }
}
