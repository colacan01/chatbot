# 자전거 온라인 샵 AI 챗봇

Ollama (qwen2.5:14b)를 활용한 자전거 온라인 쇼핑몰 AI 챗봇 시스템

## 📋 프로젝트 개요

- **백엔드**: ASP.NET Core 9.0
- **프론트엔드**: Angular 18
- **AI 모델**: Ollama (qwen2.5:14b)
- **실시간 통신**: SignalR
- **데이터베이스**: SQLite (개발용)
- **아키텍처**: Clean Architecture (Onion Architecture)

## 🏗️ 아키텍처

### Clean Architecture 구조

```
┌─────────────────────────────────────┐
│   API Layer (Controllers, Hubs)    │  ← 외부 인터페이스
├─────────────────────────────────────┤
│  Infrastructure (DB, HTTP Client)  │  ← 외부 서비스 통합
├─────────────────────────────────────┤
│  Application (Services, DTOs)      │  ← 비즈니스 로직
├─────────────────────────────────────┤
│  Domain (Entities, Enums)          │  ← 핵심 도메인
└─────────────────────────────────────┘
```

### 프로젝트 구조

```
backend/
├── BicycleShopChatbot.Domain/              # 도메인 계층
│   ├── Entities/
│   │   ├── ChatSession.cs                  # 채팅 세션
│   │   ├── ChatMessage.cs                  # 채팅 메시지
│   │   ├── Product.cs                      # 제품 정보
│   │   ├── Order.cs                        # 주문 정보
│   │   └── FAQ.cs                          # FAQ
│   └── Enums/
│       ├── MessageRole.cs                  # 메시지 역할 (User, Assistant, System)
│       ├── ChatCategory.cs                 # 채팅 카테고리
│       └── MessageStatus.cs                # 메시지 상태
│
├── BicycleShopChatbot.Application/         # 애플리케이션 계층
│   ├── DTOs/
│   │   ├── ChatMessageDto.cs
│   │   ├── SendMessageRequest.cs
│   │   └── ChatSessionDto.cs
│   ├── Interfaces/
│   │   ├── Repositories/                   # Repository 인터페이스
│   │   ├── IChatService.cs
│   │   ├── IOllamaService.cs
│   │   ├── IPromptService.cs
│   │   ├── IProductContextService.cs
│   │   └── IOrderContextService.cs
│   └── Services/
│       ├── ChatService.cs                  # 🔥 채팅 오케스트레이션
│       ├── OllamaService.cs                # 🔥 Ollama API 통합
│       ├── PromptService.cs                # 🔥 프롬프트 엔지니어링
│       ├── ProductContextService.cs        # 제품 컨텍스트 관리
│       └── OrderContextService.cs          # 주문 컨텍스트 관리
│
├── BicycleShopChatbot.Infrastructure/      # 인프라 계층
│   ├── Data/
│   │   ├── ApplicationDbContext.cs         # EF Core DbContext
│   │   └── Configurations/                 # Entity 설정
│   ├── Repositories/
│   │   └── Implementation/                 # Repository 구현
│   └── Seed/
│       ├── ProductSeedData.cs              # 제품 시드 데이터
│       ├── FaqSeedData.cs                  # FAQ 시드 데이터
│       ├── OrderSeedData.cs                # 주문 시드 데이터
│       └── DatabaseSeeder.cs               # 시드 오케스트레이터
│
└── BicycleShopChatbot.Api/                 # API 계층
    ├── Hubs/
    │   └── ChatHub.cs                      # 🔥 SignalR 허브
    ├── Controllers/
    │   └── HealthController.cs             # 헬스 체크
    ├── Program.cs                          # 앱 진입점 & DI 설정
    └── appsettings.json                    # 설정 파일

frontend/
└── src/
    └── app/
        ├── core/
        │   ├── services/
        │   │   ├── signalr.service.ts      # SignalR 연결 관리
        │   │   └── chat.service.ts         # 채팅 상태 관리
        │   └── models/
        │       ├── chat-message.model.ts
        │       └── chat-session.model.ts
        ├── features/chat/
        │   └── components/
        │       ├── chat-window/            # 메인 채팅 컨테이너
        │       ├── message-list/           # 메시지 목록
        │       ├── message-item/           # 개별 메시지
        │       └── chat-input/             # 입력 창
        └── shared/
            └── components/
```

## 🗄️ 데이터베이스 스키마

### ChatSessions - 대화 세션
```sql
- Id (UUID, PK)
- SessionId (VARCHAR, UNIQUE)
- UserId (VARCHAR, NULL)
- UserName (VARCHAR, NULL)
- CreatedAt (TIMESTAMP)
- LastActivityAt (TIMESTAMP)
- IsActive (BOOLEAN)
- SessionMetadata (TEXT/JSON)
- TotalMessages (INT)
```

### ChatMessages - 메시지
```sql
- Id (BIGINT, PK)
- ChatSessionId (UUID, FK)
- Role (VARCHAR) - 'User', 'Assistant', 'System'
- Content (TEXT)
- Timestamp (TIMESTAMP)
- Category (VARCHAR) - 'ProductSearch', 'FAQ', 'OrderStatus', 'General'
- IntentDetected (VARCHAR)
- ProductId (INT, FK, NULL)
- OrderId (INT, FK, NULL)
- Metadata (TEXT/JSON)
- TokensUsed (INT, NULL)
- ProcessingTimeMs (INT, NULL)
```

### Products - 제품 카탈로그
```sql
- Id (INT, PK)
- ProductCode (VARCHAR, UNIQUE)
- Name (VARCHAR)
- NameKorean (VARCHAR)
- Category (VARCHAR) - 'Road', 'Mountain', 'Hybrid', 'Electric', etc.
- Brand (VARCHAR)
- Price (DECIMAL)
- Description (TEXT)
- DescriptionKorean (TEXT)
- Specifications (TEXT/JSON)
- StockQuantity (INT)
- IsAvailable (BOOLEAN)
- ImageUrl (VARCHAR)
- CreatedAt, UpdatedAt (TIMESTAMP)
```

### Orders - 주문
```sql
- Id (INT, PK)
- OrderNumber (VARCHAR, UNIQUE)
- CustomerEmail (VARCHAR)
- CustomerPhone (VARCHAR)
- Status (VARCHAR) - 'Pending', 'Processing', 'Shipped', 'Delivered'
- OrderDate (TIMESTAMP)
- TotalAmount (DECIMAL)
- ShippingAddress (VARCHAR)
- TrackingNumber (VARCHAR)
- EstimatedDelivery (DATE)
- UpdatedAt (TIMESTAMP)
```

### FAQs - 자주 묻는 질문
```sql
- Id (INT, PK)
- Question (TEXT)
- QuestionKorean (TEXT)
- Answer (TEXT)
- AnswerKorean (TEXT)
- Category (VARCHAR)
- Keywords (VARCHAR)
- ViewCount (INT)
- IsActive (BOOLEAN)
- CreatedAt (TIMESTAMP)
```

## 🔑 핵심 기능

### 1. 인텐트 감지 (Intent Detection)

`PromptService.cs`에서 사용자 메시지 분석:

```csharp
public ChatCategory DetectIntent(string userMessage)
{
    var lower = userMessage.ToLower();

    if (lower.Contains("주문") || lower.Contains("배송") || lower.Contains("송장"))
        return ChatCategory.OrderStatus;

    if (lower.Contains("추천") || lower.Contains("찾아") || lower.Contains("자전거"))
        return ChatCategory.ProductSearch;

    if (lower.Contains("환불") || lower.Contains("교환") || lower.Contains("반품"))
        return ChatCategory.FAQ;

    return ChatCategory.General;
}
```

### 2. Ollama API 통합

`OllamaService.cs`:

```csharp
public async Task<string> GenerateResponseAsync(
    string userMessage,
    List<ChatMessageDto> conversationHistory,
    string systemPrompt,
    CancellationToken cancellationToken)
{
    var messages = BuildMessagePayload(userMessage, conversationHistory, systemPrompt);

    var request = new
    {
        model = "qwen2.5:14b",
        messages = messages,
        stream = false,
        options = new { temperature = 0.7, top_p = 0.9, top_k = 40 }
    };

    var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
    // ... 응답 처리
}
```

### 3. SignalR 실시간 통신

`ChatHub.cs`:

```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(SendMessageRequest request)
    {
        var response = await _chatService.ProcessUserMessageAsync(request);
        await Clients.Caller.SendAsync("ReceiveMessage", response);
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }
}
```

### 4. 프롬프트 엔지니어링

한국어 최적화 시스템 프롬프트:

```
당신은 자전거 온라인 쇼핑몰의 전문 AI 상담원입니다.
고객이 원하는 자전거를 찾도록 도와주는 것이 목표입니다.

현재 판매 중인 제품:
{products_json}

답변 규칙:
1. 항상 한국어로 친절하게 답변
2. 제품의 특징과 장점을 구체적으로 설명
3. 가격과 재고 상태를 정확히 전달
4. 고객의 용도와 예산을 고려하여 추천
5. 2~3개 제품을 비교하여 제시
```

## 🚀 시작하기

### 사전 요구사항

- .NET 9.0 SDK
- Node.js 18+ (Angular 프론트엔드용)
- Ollama 설치 및 qwen2.5:14b 모델 다운로드

### Ollama 설정

```bash
# Ollama 설치 (Linux/Mac)
curl -fsSL https://ollama.com/install.sh | sh

# 모델 다운로드
ollama pull qwen2.5:14b

# 또는 더 빠른 모델 (권장)
ollama pull qwen2.5:7b
```

### 백엔드 실행

```bash
cd backend
dotnet restore
dotnet build

# API 서버 실행
cd src/BicycleShopChatbot.Api
dotnet run
```

서버가 `http://localhost:5069`에서 실행됩니다.

### 프론트엔드 실행

```bash
cd frontend
npm install
npm start
```

프론트엔드가 `http://localhost:4200`에서 실행됩니다.

## 📡 API 엔드포인트

### REST API

- `GET /health` - 헬스 체크 (데이터베이스 및 Ollama 상태)

### SignalR Hub

- **URL**: `/hubs/chat`
- **메서드**:
  - `SendMessage(SendMessageRequest)` - 메시지 전송
  - `JoinSession(string sessionId)` - 세션 참여
  - `LeaveSession(string sessionId)` - 세션 나가기

- **클라이언트 이벤트**:
  - `ReceiveMessage(ChatMessageDto)` - 메시지 수신

## ⚙️ 설정

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bicycleshop.db"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "qwen2.5:14b",
    "TimeoutSeconds": "120",
    "MaxRetries": "3",
    "DefaultTemperature": "0.7"
  },
  "ChatSettings": {
    "MaxConversationHistory": "20",
    "MaxMessageLength": "2000",
    "SessionTimeoutMinutes": "60"
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:4200"
  }
}
```

## 📊 시드 데이터

### 제품 (10개)
- **로드 바이크**: Speedster Pro Carbon, Aero Sprint Elite
- **산악 바이크**: Mountain Explorer XT, Trail Blazer Pro
- **하이브리드**: City Commuter Deluxe
- **전기 자전거**: E-Power Cruiser, City E-Commuter
- **접이식**: Compact Folder
- **어린이용**: Junior Racer
- **그래블**: Adventure Seeker

### FAQ (20개)
- 반품/교환 정책
- 배송 정보
- 조립 서비스
- 보증/AS
- 결제 방법
- 고객 지원
- 할인/이벤트
- 기타

### 주문 (3개)
- 배송 중, 처리 중, 배송 완료 샘플 데이터

## 🧪 테스트

### 헬스 체크

```bash
curl http://localhost:5069/health
```

예상 응답:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-02T...",
  "database": {
    "status": "connected",
    "provider": "Microsoft.EntityFrameworkCore.Sqlite"
  },
  "ollama": {
    "status": "available"
  }
}
```

### SignalR 테스트 (Node.js)

```javascript
const signalR = require('@microsoft/signalr');

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5069/hubs/chat')
    .build();

connection.on('ReceiveMessage', (message) => {
    console.log('받은 메시지:', message.content);
});

await connection.start();
await connection.invoke('SendMessage', {
    sessionId: 'test-session',
    message: '로드 바이크 추천해주세요',
    userId: 'test-user'
});
```

## ⚡ 성능 고려사항

### 응답 시간

- **qwen2.5:14b** (9GB 모델):
  - 첫 요청: 2-3분 (모델 로딩)
  - 후속 요청: 1.5-2분

- **qwen2.5:7b** (권장, 4GB 모델):
  - 첫 요청: 30-60초
  - 후속 요청: 20-40초

### 최적화 방안

1. **더 작은 모델 사용**:
   ```bash
   ollama pull qwen2.5:7b
   ollama pull llama3.2:3b
   ```

2. **컨텍스트 축소**:
   - 상위 3-5개 제품만 전송
   - 대화 기록 10개로 제한

3. **스트리밍 활성화**:
   - `stream: true` 옵션 사용
   - 실시간으로 응답 표시

4. **타임아웃 증가** (임시 해결책):
   ```json
   "Ollama": {
     "TimeoutSeconds": "300"
   }
   ```

## 🔒 보안

- ✅ 입력 검증 (FluentValidation 준비됨)
- ✅ CORS 엄격 설정
- ✅ XSS 방지 (응답 새니타이즈 필요)
- ⚠️ Rate Limiting (추후 추가 권장)
- ⚠️ 인증/인가 (추후 추가 권장)

## 📝 TODO

### 백엔드
- [ ] 응답 스트리밍 구현
- [ ] Rate Limiting 추가
- [ ] 사용자 인증 구현
- [ ] 단위 테스트 작성
- [ ] 통합 테스트 작성
- [ ] 에러 처리 개선
- [ ] 로깅 강화

### 프론트엔드
- [ ] Angular 프로젝트 설정
- [ ] SignalR 클라이언트 구현
- [ ] 채팅 UI 컴포넌트
- [ ] 타이핑 인디케이터
- [ ] 마크다운 렌더링
- [ ] 반응형 디자인
- [ ] E2E 테스트

## 📚 기술 스택

### 백엔드
- ASP.NET Core 9.0
- Entity Framework Core 9.0
- SignalR
- SQLite (개발) / PostgreSQL (프로덕션 권장)

### AI
- Ollama
- qwen2.5:14b (또는 7b)

### 프론트엔드
- Angular 18
- @microsoft/signalr
- Angular Material
- ngx-markdown
- RxJS

## 🤝 기여

이 프로젝트는 자전거 온라인 쇼핑몰을 위한 AI 챗봇 시스템입니다.

## 📄 라이선스

MIT

## 🐛 알려진 이슈

1. **Ollama 응답 시간**: qwen2.5:14b 모델은 전체 제품 컨텍스트에서 120초 이상 소요
   - **해결책**: qwen2.5:7b 또는 llama3.2:3b 사용 권장

2. **첫 요청 지연**: Ollama 모델이 메모리에 로드되는 동안 긴 지연
   - **해결책**: 서버 시작 시 웜업 요청 전송

3. **SignalR 타임아웃**: 긴 Ollama 응답 시 연결 끊김
   - **해결책**: 스트리밍 응답 구현 또는 타임아웃 증가

## 📞 문의

프로젝트 관련 문의사항은 이슈를 생성해주세요.
