# 자전거 쇼핑몰 챗봇 백엔드 아키텍처 문서

## 📋 목차
1. [아키텍처 개요](#1-아키텍처-개요)
2. [Ollama 서비스 연동](#2-ollama-서비스-연동)
3. [RAG 기능 구현 현황](#3-rag-기능-구현-현황)
4. [레이어 아키텍처](#4-레이어-아키텍처)
5. [데이터 흐름](#5-데이터-흐름)
6. [데이터베이스 설계](#6-데이터베이스-설계)
7. [보안 및 인증](#7-보안-및-인증)
8. [성능 최적화](#8-성능-최적화)

---

## 🚀 실제 구현 요약 (Quick Overview)

### 기술 스택
- **프레임워크**: ASP.NET Core 9.0
- **데이터베이스**: PostgreSQL 16 + pgvector
- **AI 모델**: Ollama (qwen2.5:14b + nomic-embed-text)
- **실시간 통신**: SignalR WebSocket
- **인증**: JWT (Access + Refresh Token)
- **ORM**: Entity Framework Core 9.0

### 핵심 기능
✅ 벡터 기반 RAG (pgvector)  
✅ 실시간 스트리밍 응답 (IAsyncEnumerable)  
✅ 의도 감지 및 자동 라우팅  
✅ 제품 검색 (벡터 유사도 + 키워드 Fallback)  
✅ 대화 히스토리 관리  
✅ 세션 기반 컨텍스트 유지  

### 주요 API 엔드포인트
- `POST /api/auth/login` - JWT 로그인
- `GET /api/chat/sessions` - 사용자 세션 목록
- `SignalR /hubs/chat` - 실시간 채팅
  - `SendMessageStream` - 스트리밍 메시지 전송
  - `ReceiveMessageChunk` - 청크 단위 수신

### 구현된 엔티티
- **User**: 사용자 (JWT 인증)
- **ChatSession**: 대화 세션 (Guid ID)
- **ChatMessage**: 메시지 (Role, Content, Category)
- **Product**: 제품 (product_embeddings 테이블 매핑)
- **ProductEmbedding**: 벡터 임베딩 (768차원)
- **Order**: 주문
- **FAQ**: 자주 묻는 질문

### 구현된 서비스
- **ChatService**: 대화 흐름 관리
- **OllamaService**: AI 모델 통신 (HTTP Client)
- **EmbeddingService**: 벡터 임베딩 생성
- **PromptService**: 프롬프트 생성 및 의도 감지
- **ProductContextService**: 제품 검색 (벡터 + 키워드)
- **VectorProductRepository**: pgvector 기반 유사도 검색
- **AuthService**: JWT 인증
- **JwtTokenService**: 토큰 생성/검증
- **BCryptPasswordHasher**: 비밀번호 해싱

---

## 1. 아키텍처 개요

### 1.1 Clean Architecture 적용

본 프로젝트는 **Clean Architecture (Onion Architecture)** 패턴을 완벽하게 적용하여 구현되었습니다.

```
┌─────────────────────────────────────────────────────────┐
│                    API Layer (최외곽)                   │
│  ┌───────────────────────────────────────────────────┐  │
│  │          Infrastructure Layer                     │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │      Application Layer                      │  │  │
│  │  │  ┌───────────────────────────────────────┐  │  │  │
│  │  │  │    Domain Layer (핵심)                │  │  │  │
│  │  │  │  • Entities                           │  │  │  │
│  │  │  │  • Enums                              │  │  │  │
│  │  │  │  • Business Rules                     │  │  │  │
│  │  │  └───────────────────────────────────────┘  │  │  │
│  │  │  • Services                                 │  │  │
│  │  │  • DTOs                                     │  │  │
│  │  │  • Interfaces                               │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  │  • Repositories                                   │  │
│  │  • DbContext                                      │  │
│  │  • Auth (JWT, BCrypt)                             │  │
│  └───────────────────────────────────────────────────┘  │
│  • Controllers                                          │
│  • SignalR Hubs                                         │
│  • DI Configuration                                     │
└─────────────────────────────────────────────────────────┘

의존성 방향: API → Infrastructure → Application → Domain
```

### 1.2 프로젝트 구조

```
backend/
├── src/
│   ├── BicycleShopChatbot.Domain/          # 도메인 계층
│   │   ├── Entities/                       # 비즈니스 엔티티
│   │   │   ├── ChatSession.cs
│   │   │   ├── ChatMessage.cs
│   │   │   ├── Product.cs
│   │   │   ├── Order.cs
│   │   │   ├── FAQ.cs
│   │   │   └── User.cs
│   │   └── Enums/                          # 열거형
│   │       ├── ChatCategory.cs             # General/ProductSearch/FAQ/OrderStatus
│   │       ├── MessageRole.cs              # User/Assistant/System
│   │       ├── MessageStatus.cs
│   │       └── UserRole.cs                 # Admin/Customer
│   │
│   ├── BicycleShopChatbot.Application/     # 애플리케이션 계층
│   │   ├── DTOs/                           # 데이터 전송 객체
│   │   │   ├── ChatMessageDto.cs
│   │   │   ├── ChatSessionDto.cs
│   │   │   ├── ChatStreamChunk.cs          # 스트리밍 청크
│   │   │   ├── SendMessageRequest.cs
│   │   │   ├── JwtSettings.cs
│   │   │   ├── AuthDtos.cs
│   │   │   ├── ProductDto.cs
│   │   │   └── OrderDto.cs
│   │   ├── Interfaces/                     # 서비스/레포지토리 인터페이스
│   │   │   ├── Repositories/
│   │   │   │   ├── IRepository<T>.cs       # Generic Repository
│   │   │   │   ├── IChatSessionRepository.cs
│   │   │   │   ├── IChatMessageRepository.cs
│   │   │   │   ├── IProductRepository.cs
│   │   │   │   ├── IOrderRepository.cs
│   │   │   │   ├── IFAQRepository.cs
│   │   │   │   └── IUserRepository.cs
│   │   │   ├── IChatService.cs
│   │   │   ├── IOllamaService.cs
│   │   │   ├── IPromptService.cs
│   │   │   ├── IProductContextService.cs
│   │   │   ├── IOrderContextService.cs
│   │   │   ├── IAuthService.cs
│   │   │   ├── IJwtTokenService.cs
│   │   │   └── IPasswordHasher.cs
│   │   └── Services/                       # 비즈니스 로직 구현
│   │       ├── ChatService.cs              # 🔥 핵심 채팅 서비스
│   │       ├── OllamaService.cs            # 🔥 AI 통합 서비스
│   │       ├── PromptService.cs            # 🔥 프롬프트 관리
│   │       ├── ProductContextService.cs    # 제품 검색
│   │       ├── OrderContextService.cs      # 주문 조회
│   │       └── AuthService.cs              # 인증 서비스
│   │
│   ├── BicycleShopChatbot.Infrastructure/  # 인프라 계층
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs     # EF Core DbContext
│   │   │   └── Configurations/             # Fluent API 설정
│   │   │       ├── ChatSessionConfiguration.cs
│   │   │       ├── ChatMessageConfiguration.cs
│   │   │       ├── ProductConfiguration.cs
│   │   │       ├── OrderConfiguration.cs
│   │   │       ├── FAQConfiguration.cs
│   │   │       └── UserConfiguration.cs
│   │   ├── Repositories/Implementation/    # Repository 구현
│   │   │   ├── Repository<T>.cs            # Generic Base
│   │   │   ├── ChatSessionRepository.cs
│   │   │   ├── ChatMessageRepository.cs
│   │   │   ├── ProductRepository.cs
│   │   │   ├── OrderRepository.cs
│   │   │   ├── FAQRepository.cs
│   │   │   └── UserRepository.cs
│   │   ├── Auth/                           # 인증 구현
│   │   │   ├── JwtTokenService.cs          # JWT 토큰 생성/검증
│   │   │   ├── JwtSettings.cs
│   │   │   └── BCryptPasswordHasher.cs     # BCrypt 해싱
│   │   └── Seed/                           # 시드 데이터
│   │       ├── DatabaseSeeder.cs
│   │       ├── ProductSeedData.cs          # 10개 자전거
│   │       ├── FaqSeedData.cs              # 20개 FAQ
│   │       └── OrderSeedData.cs            # 3개 주문
│   │
│   └── BicycleShopChatbot.Api/             # API 계층
│       ├── Controllers/
│       │   ├── ChatController.cs           # REST API
│       │   ├── AuthController.cs           # 인증 API
│       │   └── HealthController.cs         # 헬스 체크
│       ├── Hubs/
│       │   └── ChatHub.cs                  # 🔥 SignalR 실시간 통신
│       ├── Program.cs                      # 🔥 DI 설정 및 진입점
│       ├── appsettings.json                # 환경 설정
│       └── bicycleshop.db                  # SQLite 데이터베이스
```

### 1.3 기술 스택

| 항목 | 기술 | 버전 |
|------|------|------|
| **프레임워크** | ASP.NET Core | 9.0 |
| **ORM** | Entity Framework Core | 9.0 |
| **데이터베이스** | PostgreSQL (with pgvector) | 16.x |
| **실시간 통신** | SignalR + WebSocket | 9.0 |
| **AI 모델** | Ollama (qwen2.5:14b) | - |
| **임베딩 모델** | nomic-embed-text | - |
| **인증** | JWT Bearer + BCrypt | - |
| **언어** | C# | 13.0 |

### 1.4 실제 구현된 주요 컴포넌트

#### Controllers
- **ChatController**: REST API 엔드포인트 (세션 관리)
- **AuthController**: 인증/회원가입 API
- **HealthController**: 헬스 체크 엔드포인트

#### SignalR Hubs
- **ChatHub**: 실시간 채팅 통신 (`/hubs/chat`)
  - `SendMessage`: 비스트리밍 메시지 전송
  - `SendMessageStream`: 스트리밍 메시지 전송
  - `JoinSession`: 세션 그룹 참여
  - `LeaveSession`: 세션 그룹 퇴장
  - `LoadSessionHistory`: 세션 히스토리 로드

#### Services (Application Layer)
- **ChatService**: 대화 처리 및 흐름 관리
- **OllamaService**: AI 모델 통신 (스트리밍/비스트리밍)
- **EmbeddingService**: 벡터 임베딩 생성
- **PromptService**: 프롬프트 생성 및 의도 감지
- **ProductContextService**: 제품 검색 (벡터 유사도)
- **OrderContextService**: 주문 조회
- **AuthService**: 인증 및 회원가입
- **JwtTokenService**: JWT 토큰 생성/검증
- **BCryptPasswordHasher**: 비밀번호 해싱

#### Repositories (Infrastructure Layer)
- **Generic Repository<T>**: 기본 CRUD 구현
- **ChatSessionRepository**: 세션 관리
- **ChatMessageRepository**: 메시지 CRUD
- **ProductRepository**: 제품 조회
- **VectorProductRepository**: 벡터 기반 제품 검색
- **OrderRepository**: 주문 조회
- **FAQRepository**: FAQ 검색
- **UserRepository**: 사용자 관리

#### Domain Entities
- **ChatSession**: 대화 세션 (Guid Id, SessionId, UserId, Title)
- **ChatMessage**: 메시지 (Role, Content, Timestamp, Category)
- **Product**: 제품 정보 + 벡터 임베딩 (product_embeddings 테이블 매핑)
- **ProductEmbedding**: 제품 벡터 데이터 (pgvector)
- **Order**: 주문 정보
- **FAQ**: 자주 묻는 질문
- **User**: 사용자 (Email, PasswordHash, Role)

#### Enums
- **MessageRole**: User, Assistant, System
- **ChatCategory**: General, ProductSearch, FAQ, OrderStatus
- **MessageStatus**: Pending, Completed, Failed
- **UserRole**: Admin, Customer

---

## 2. Ollama 서비스 연동

### 2.1 OllamaService 개요

**파일 위치**: `BicycleShopChatbot.Application/Services/OllamaService.cs`

OllamaService는 로컬 Ollama 서버와 통신하여 AI 응답을 생성하는 핵심 서비스입니다.

```csharp
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly string _modelName;           // "qwen2.5:7b"
    private readonly double _temperature;         // 0.7
}
```

### 2.2 연동 방식 상세

### 2.2 연동 방식 상세

#### 2.2.1 설정 파일 (`appsettings.json`)

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "qwen2.5:14b",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": "120",
    "MaxRetries": "3",
    "RetryDelaySeconds": "2",
    "DefaultTemperature": "0.7"
  }
}
```

**설정 항목 설명**:
- **BaseUrl**: Ollama 서버 주소 (기본: localhost:11434)
- **ModelName**: 사용할 AI 모델
  - `qwen2.5:7b`: 권장 (4GB VRAM, 20-40초 응답)
  - `qwen2.5:14b`: 고성능 (9GB VRAM, 1-2분 응답) - **현재 사용**
  - `llama3.2:1b`: 경량 (1GB VRAM, 5-10초 응답)
- **EmbeddingModel**: 벡터 임베딩 모델 (nomic-embed-text)
- **TimeoutSeconds**: HTTP 요청 타임아웃 (2분)
- **MaxRetries**: 재시도 횟수
- **RetryDelaySeconds**: 재시도 대기 시간
- **DefaultTemperature**: 응답 다양성 (0.0~2.0)
  - 0.0: 결정론적, 일관됨
  - 0.7: 균형 (권장) - **현재 사용**
  - 1.5+: 창의적, 변동성 높음

#### 2.2.2 HttpClient Factory 패턴 (Program.cs)

**OllamaService HTTP 클라이언트 설정**:
```csharp
builder.Services.AddHttpClient<IOllamaService, OllamaService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new SocketsHttpHandler
        {
            // 연결 풀링: 오래된 연결 재사용 방지
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,

            // Keep-alive: 유휴 연결 종료 방지
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,

            // 연결 타임아웃 (요청 타임아웃과 별개)
            ConnectTimeout = TimeSpan.FromSeconds(30),
            MaxResponseHeadersLength = 128
        };
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(30));
```

**EmbeddingService HTTP 클라이언트 설정**:
```csharp
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            ConnectTimeout = TimeSpan.FromSeconds(30)
        };
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(30));
```

**핵심 최적화 포인트**:
- **PooledConnectionLifetime**: 15분마다 연결 재생성 (메모리 누수 방지)
- **KeepAlivePingDelay**: 60초마다 ping 전송 (연결 유지)
- **MaxConnectionsPerServer**: 최대 10개 동시 연결
- **SetHandlerLifetime**: 30분마다 핸들러 재생성

**생성자 주입**:
```csharp
public OllamaService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OllamaService> logger)
{
    _httpClient = httpClient;
    _logger = logger;
    _modelName = configuration["Ollama:ModelName"] ?? "qwen2.5:14b";
    _temperature = double.Parse(configuration["Ollama:DefaultTemperature"] ?? "0.7");

    var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    _httpClient.BaseAddress = new Uri(baseUrl);
    _httpClient.Timeout = TimeSpan.FromSeconds(
        int.Parse(configuration["Ollama:TimeoutSeconds"] ?? "120"));
}
```

### 2.3 API 엔드포인트 및 통신

#### 2.3.1 Ollama Chat API 호출

**엔드포인트**: `POST /api/chat`

**요청 포맷**:
```json
{
  "model": "qwen2.5:7b",
  "messages": [
    {
      "role": "system",
      "content": "당신은 대한민국의 자전거 전문 온라인 쇼핑몰 AI 상담원입니다..."
    },
    {
      "role": "user",
      "content": "안녕하세요"
    },
    {
      "role": "assistant",
      "content": "안녕하세요! 자전거 전문 온라인 쇼핑몰입니다."
    },
    {
      "role": "user",
      "content": "100만원 예산으로 출퇴근용 자전거 추천해주세요"
    }
  ],
  "stream": true,
  "options": {
    "temperature": 0.7,
    "top_p": 0.9,
    "top_k": 40
  }
}
```

**응답 포맷 (스트리밍)**:
```json
{"message":{"role":"assistant","content":"100"},"done":false}
{"message":{"role":"assistant","content":"만원"},"done":false}
{"message":{"role":"assistant","content":" 예산"},"done":false}
...
{"message":{"role":"assistant","content":""},"done":true}
```

#### 2.3.2 메시지 구성 로직

```csharp
private List<object> BuildMessagePayload(
    string userMessage,
    List<ChatMessageDto> history,
    string systemPrompt)
{
    var messages = new List<object>
    {
        // 1. 시스템 프롬프트 (항상 첫 번째)
        new { role = "system", content = systemPrompt }
    };

    // 2. 대화 히스토리 (최대 10개)
    foreach (var msg in history.TakeLast(10))
    {
        messages.Add(new
        {
            role = msg.Role.ToLowerInvariant(),  // "user" 또는 "assistant"
            content = msg.Content
        });
    }

    // 3. 사용자 메시지 (항상 마지막)
    messages.Add(new { role = "user", content = userMessage });

    return messages;
}
```

**대화 히스토리 제한 이유**:
- 컨텍스트 윈도우 최적화 (Qwen 2.5는 32K 토큰 지원)
- 응답 시간 단축
- 관련성 높은 최근 대화에 집중

### 2.4 스트리밍 처리 구현

#### 2.4.1 비스트리밍 vs 스트리밍

| 방식 | 장점 | 단점 | 용도 |
|------|------|------|------|
| **비스트리밍** | 구현 간단, 전체 응답 한 번에 | 응답 대기 시간 김 | 짧은 답변 |
| **스트리밍** | 실시간 응답, UX 향상 | 구현 복잡 | 긴 답변 (권장) |

#### 2.4.2 스트리밍 구현 (`GenerateResponseStreamAsync`)

```csharp
public async IAsyncEnumerable<string> GenerateResponseStreamAsync(
    string userMessage,
    List<ChatMessageDto> conversationHistory,
    string systemPrompt,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // 1. 메시지 페이로드 구성
    var messages = BuildMessagePayload(userMessage, conversationHistory, systemPrompt);

    // 2. 요청 객체 생성
    var request = new
    {
        model = _modelName,
        messages = messages,
        stream = true,  // ← 스트리밍 활성화
        options = new
        {
            temperature = _temperature,
            top_p = 0.9,
            top_k = 40
        }
    };

    // 3. HTTP 요청 전송
    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
    httpRequest.Content = JsonContent.Create(request);

    using var response = await _httpClient.SendAsync(
        httpRequest,
        HttpCompletionOption.ResponseHeadersRead,  // ← 헤더만 먼저 수신
        cancellationToken);

    response.EnsureSuccessStatusCode();

    // 4. 스트림 읽기
    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    using var reader = new StreamReader(stream);

    int lineCount = 0;
    int chunkCount = 0;

    while (!reader.EndOfStream)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 5. 한 줄씩 읽기 (JSON Lines 형식)
        var line = await reader.ReadLineAsync();
        lineCount++;

        if (string.IsNullOrWhiteSpace(line))
        {
            continue;  // 빈 라인 스킵
        }

        // 6. JSON 역직렬화
        var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // 7. 컨텐츠 추출 및 반환
        if (chunk?.Message?.Content != null)
        {
            chunkCount++;
            yield return chunk.Message.Content;  // ← IAsyncEnumerable yield
        }

        // 8. 완료 감지
        if (chunk?.Done == true)
        {
            _logger.LogInformation("Streaming completed. Chunks: {ChunkCount}", chunkCount);
            break;
        }
    }
}
```

**핵심 기술**:
- **IAsyncEnumerable<T>**: C# 8.0+ 비동기 스트림
- **yield return**: 지연 실행 (lazy evaluation)
- **HttpCompletionOption.ResponseHeadersRead**: 응답 본문을 기다리지 않고 즉시 처리 시작
- **JsonSerializer**: System.Text.Json 사용 (빠름)

#### 2.4.3 스트리밍 흐름도

```
클라이언트 (Angular)
    ↓ SignalR.SendMessageStream()
ChatHub.SendMessageStream()
    ↓
ChatService.ProcessUserMessageStreamAsync()
    ↓
OllamaService.GenerateResponseStreamAsync()
    ↓
await foreach (var chunk in stream)
    ├─ Ollama 서버 → JSON Line 수신
    ├─ 역직렬화 → OllamaStreamResponse
    ├─ chunk.Message.Content 추출
    └─ yield return chunk.Content
        ↓
ChatService
    ├─ 청크 누적 (StringBuilder)
    └─ ChatStreamChunk DTO 생성
        ↓
SignalR Hub
    └─ Clients.Caller.SendAsync("ReceiveMessageChunk", chunk)
        ↓
클라이언트 (Angular)
    └─ 실시간 UI 업데이트
```

### 2.5 에러 처리 및 재시도

```csharp
try
{
    var response = await _httpClient.PostAsJsonAsync("/api/chat", request, cancellationToken);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);

    if (result?.Message?.Content == null)
    {
        _logger.LogWarning("Ollama returned empty response");
        return "죄송합니다. 응답을 생성하는 중 문제가 발생했습니다.";
    }

    return result.Message.Content;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "HTTP error while communicating with Ollama");
    return "죄송합니다. AI 서비스와 통신 중 오류가 발생했습니다.";
}
catch (TaskCanceledException ex)
{
    _logger.LogError(ex, "Request to Ollama timed out");
    return "죄송합니다. 요청 시간이 초과되었습니다. 잠시 후 다시 시도해주세요.";
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error while calling Ollama");
    return "죄송합니다. 예기치 않은 오류가 발생했습니다.";
}
```

### 2.6 모델 가용성 확인

```csharp
public async Task<bool> IsModelAvailableAsync(CancellationToken cancellationToken = default)
{
    try
    {
        var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken);

        // 모델 이름의 접두사로 검색 (qwen2.5:7b → qwen2.5)
        return result?.Models?.Any(m => m.Name?.Contains(_modelName.Split(':')[0]) == true) ?? false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking if Ollama model is available");
        return false;
    }
}
```

**사용 예시**:
```csharp
// Program.cs에서 시작 시 확인
var ollamaService = app.Services.GetRequiredService<IOllamaService>();
if (!await ollamaService.IsModelAvailableAsync())
{
    Console.WriteLine("⚠️ Warning: Ollama model not available. Please start Ollama server.");
}
```

---

## 3. RAG 기능 구현 현황

### 3.1 RAG (Retrieval-Augmented Generation) 개요

**RAG란?**
- LLM의 응답에 **외부 지식 검색 결과를 주입**하여 정확도를 높이는 기법
- 일반적 구성:
  1. **문서 임베딩**: 텍스트를 벡터로 변환
  2. **벡터 데이터베이스**: 유사도 검색 지원 (Pinecone, Weaviate, Pgvector 등)
  3. **의미적 검색**: 쿼리와 유사한 문서 검색
  4. **컨텍스트 주입**: 검색 결과를 프롬프트에 추가

### 3.2 현재 구현 상태: ✅ **벡터 기반 RAG 구현 완료**

**구현된 기능**:
- ✅ 벡터 데이터베이스 사용 (PostgreSQL + pgvector)
- ✅ 문서 임베딩 기능 (nomic-embed-text)
- ✅ 의미적 검색 지원 (코사인 유사도)
- ✅ Ollama Embeddings API 연동
- ✅ ProductEmbedding 엔티티 구현
- ✅ VectorProductRepository 구현

**데이터베이스 구조**:
```sql
-- product_embeddings 테이블 (기존 데이터 재사용)
CREATE TABLE product_embeddings (
    id SERIAL PRIMARY KEY,
    product_code VARCHAR(50) UNIQUE,
    name VARCHAR(200),
    name_korean VARCHAR(200),
    category VARCHAR(100),
    brand VARCHAR(100),
    price DECIMAL(10,2),
    description TEXT,
    description_korean TEXT,
    specifications TEXT,
    stock_quantity INTEGER,
    is_available BOOLEAN,
    image_url TEXT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    embedding vector(768)  -- nomic-embed-text 차원
);

-- 벡터 유사도 검색 인덱스
CREATE INDEX idx_product_embeddings_vector 
ON product_embeddings 
USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);
```

### 3.3 벡터 기반 제품 검색 구현

#### 3.3.1 EmbeddingService (임베딩 생성)

**파일**: `BicycleShopChatbot.Application/Services/EmbeddingService.cs`

```csharp
public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _embeddingModel = "nomic-embed-text";

    public async Task<float[]?> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _embeddingModel,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/embed",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        if (result.TryGetProperty("embeddings", out var embeddings))
        {
            var vectorList = new List<float>();
            foreach (var value in embeddings[0].EnumerateArray())
            {
                vectorList.Add((float)value.GetDouble());
            }
            return vectorList.ToArray(); // 768차원 벡터
        }

        return null;
    }

    public string BuildSearchableText(Product product)
    {
        var parts = new List<string>
        {
            product.NameKorean,
            product.Name,
            product.Category,
            product.Brand,
            product.DescriptionKorean ?? string.Empty,
            product.Specifications
        };
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
```

**재시도 로직 포함**:
- MaxRetries: 3회
- Exponential Backoff: 2초 → 4초 → 8초
- Rate Limiting: 각 임베딩 간 100ms 딜레이

#### 3.3.2 VectorProductRepository (벡터 검색)

**파일**: `BicycleShopChatbot.Infrastructure/Repositories/Implementation/VectorProductRepository.cs`

```csharp
public class VectorProductRepository : IVectorProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<VectorProductRepository> _logger;

    public async Task<List<Product>> SearchByVectorAsync(
        string queryText,
        int maxResults = 10,
        double similarityThreshold = 0.5,
        CancellationToken cancellationToken = default)
    {
        // 1. 쿼리 텍스트를 벡터로 변환
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            queryText,
            cancellationToken);

        if (queryEmbedding == null)
        {
            _logger.LogWarning("Failed to generate embedding for query: {Query}", queryText);
            return new List<Product>();
        }

        // 2. 벡터 유사도 검색 (코사인 유사도)
        var vectorString = $"[{string.Join(",", queryEmbedding)}]";

        var products = await _context.Products
            .FromSqlRaw(@"
                SELECT * 
                FROM product_embeddings 
                WHERE is_available = true 
                  AND embedding IS NOT NULL
                  AND 1 - (embedding <=> {0}::vector) >= {1}
                ORDER BY embedding <=> {0}::vector
                LIMIT {2}",
                vectorString,
                similarityThreshold,
                maxResults)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Vector search for '{Query}' found {Count} products (threshold: {Threshold})",
            queryText, products.Count, similarityThreshold);

        return products;
    }
}
```

**핵심 개념**:
- **<=>**: pgvector의 코사인 거리 연산자
- **1 - 거리 = 유사도**: 0~1 범위 (1에 가까울수록 유사)
- **similarityThreshold**: 최소 유사도 임계값 (기본 0.5)

#### 3.3.3 ProductContextService (벡터 검색 통합)

```csharp
public class ProductContextService : IProductContextService
{
    private readonly IVectorProductRepository _vectorRepository;
    private readonly IProductRepository _productRepository;

    public async Task<List<Product>> SearchProductsAsync(
        string query,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        // 우선 벡터 검색 시도
        var vectorResults = await _vectorRepository.SearchByVectorAsync(
            query,
            maxResults,
            similarityThreshold: 0.5,
            cancellationToken);

        if (vectorResults.Any())
        {
            _logger.LogInformation(
                "Vector search returned {Count} results for query: {Query}",
                vectorResults.Count, query);
            return vectorResults;
        }

        // Fallback: 키워드 검색
        _logger.LogInformation(
            "Vector search returned no results, falling back to keyword search");
        return await _productRepository.SearchProductsAsync(
            query,
            maxResults,
            cancellationToken);
    }
}
```

**검색 전략**:
1. **1차**: 벡터 유사도 검색 (의미적 검색)
2. **2차**: 키워드 검색 (Fallback)

### 3.4 벡터 검색의 장점

**기존 키워드 검색 문제점**:
```csharp
// LIKE 연산 기반 - 정확한 일치만 검색
p.NameKorean.Contains("출퇴근")  // "통근용"은 못 찾음
```

**벡터 검색 장점**:
```sql
-- 의미적 유사도 기반 검색
"출퇴근용 자전거 추천" 
→ "도시형 하이브리드 바이크" (유사도: 0.85)
→ "통근용 전기자전거" (유사도: 0.82)
→ "가벼운 알루미늄 로드바이크" (유사도: 0.78)
```

**실제 효과**:
- ✅ 동의어 인식: "출퇴근" ≈ "통근"
- ✅ 문맥 이해: "예산 100만원" → 가격대 필터링
- ✅ 다국어 지원: "road bike" ≈ "로드바이크"
- ✅ 타이포 허용: "로드 바이크" ≈ "로드바이크"

### 3.5 컨텍스트 주입 방식

**제품 검색 프롬프트 생성**:
```csharp
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
```

**생성된 프롬프트 예시**:
```
당신은 대한민국의 자전거 전문 온라인 쇼핑몰 AI 상담원입니다.
고객이 원하는 자전거를 찾도록 도와주는 것이 목표입니다.

========================================
[ 절대 규칙 - 반드시 준수하세요 ]
========================================
1. 언어: 반드시 한국어로만 답변하세요...
2. 역할: 당신은 대한민국의 자전거 전문 온라인 쇼핑몰 상담원입니다.
3. 예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내...
========================================

## 현재 판매 중인 제품:

### 트렉 도미네 AL 2 (Trek Domane AL 2)
- **카테고리**: Road
- **브랜드**: Trek
- **가격**: 1,250,000원
- **재고**: 15개
- **설명**: 가볍고 편안한 알루미늄 로드바이크...

### 자이언트 TCR 어드밴스드 2 (Giant TCR Advanced 2)
- **카테고리**: Road
- **브랜드**: Giant
- **가격**: 3,500,000원
- **재고**: 8개
- **설명**: 카본 프레임의 경량 레이싱 바이크...
```

#### 3.4.2 ChatService의 컨텍스트 빌드

```csharp
private async Task<string> BuildContextualPromptAsync(
    ChatCategory intent,
    string userMessage,
    CancellationToken cancellationToken)
{
    return intent switch
    {
        ChatCategory.ProductSearch =>
            await BuildProductSearchPromptAsync(userMessage, cancellationToken),
        ChatCategory.OrderStatus =>
            await BuildOrderStatusPromptAsync(userMessage, cancellationToken),
        ChatCategory.FAQ =>
            await BuildFaqPromptAsync(userMessage, cancellationToken),
        _ => _promptService.GetSystemPrompt(intent)
    };
}

private async Task<string> BuildProductSearchPromptAsync(
    string userMessage,
    CancellationToken cancellationToken)
{
    // 1. 사용자 메시지에서 키워드 추출
    var products = await _productContextService.SearchProductsAsync(
        userMessage,
        maxResults: 10,
        cancellationToken);

    // 2. 제품 정보를 포함한 프롬프트 생성
    return _promptService.GetProductSearchPrompt(userMessage, products);
}
```

### 3.5 진정한 RAG 구현을 위한 로드맵

#### 단계 1: 벡터 데이터베이스 도입
```
선택지:
1. Pgvector (PostgreSQL 확장)
   - 장점: PostgreSQL과 통합, 무료
   - 단점: 성능 제한

2. Pinecone
   - 장점: 관리형 서비스, 고성능
   - 단점: 유료

3. Weaviate
   - 장점: 오픈소스, 다양한 기능
   - 단점: 인프라 관리 필요

4. Qdrant
   - 장점: Rust 기반 고성능, 오픈소스
   - 단점: 상대적으로 신규
```

#### 단계 2: 임베딩 생성
```csharp
// Ollama Embeddings API 사용 예시
public async Task<float[]> GenerateEmbeddingAsync(string text)
{
    var request = new
    {
        model = "nomic-embed-text",  // Ollama 임베딩 모델
        prompt = text
    };

    var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);
    var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();

    return result.Embedding;
}
```

#### 단계 3: 유사도 검색
```csharp
// Pgvector 예시
public async Task<List<Product>> SemanticSearchAsync(float[] queryEmbedding, int topK = 10)
{
    return await _context.Products
        .OrderBy(p => EF.Functions.VectorDistance(p.Embedding, queryEmbedding))
        .Take(topK)
        .ToListAsync();
}
```

### 3.6 현재 vs 진정한 RAG 비교

| 항목 | 현재 구현 | 진정한 RAG |
|------|---------|----------|
| **검색 방식** | LIKE 연산 (문자열 포함) | 벡터 유사도 검색 |
| **의미 이해** | ❌ 불가 | ✅ 가능 |
| **다국어 지원** | ❌ 제한적 | ✅ 우수 |
| **타이포 허용** | ❌ 불가 | ✅ 가능 |
| **성능** | O(n) | O(log n) |
| **정확도** | 중간 | 높음 |
| **구현 복잡도** | 낮음 | 높음 |
| **인프라 요구사항** | SQLite만 | 벡터 DB 필요 |

---

## 4. 레이어 아키텍처

### 4.1 레이어 의존성 그래프

```
┌───────────────────────────────────────────────────────────┐
│                      API Layer                            │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Controllers/                                        │  │
│  │  ├─ ChatController → IChatService                   │  │
│  │  ├─ AuthController → IAuthService                   │  │
│  │  └─ HealthController                                │  │
│  │                                                     │  │
│  │ Hubs/                                               │  │
│  │  └─ ChatHub → IChatService                          │  │
│  │                                                     │  │
│  │ Program.cs (DI Configuration)                       │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
                          ↓
┌───────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                      │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Repositories/Implementation/                        │  │
│  │  ├─ Repository<T> → ApplicationDbContext            │  │
│  │  ├─ ChatSessionRepository → IRepository<ChatSession>│  │
│  │  ├─ ChatMessageRepository                           │  │
│  │  ├─ ProductRepository                               │  │
│  │  ├─ OrderRepository                                 │  │
│  │  ├─ FAQRepository                                   │  │
│  │  └─ UserRepository                                  │  │
│  │                                                     │  │
│  │ Data/                                               │  │
│  │  ├─ ApplicationDbContext (EF Core)                  │  │
│  │  └─ Configurations/ (Fluent API)                    │  │
│  │                                                     │  │
│  │ Auth/                                               │  │
│  │  ├─ JwtTokenService → IJwtTokenService              │  │
│  │  └─ BCryptPasswordHasher → IPasswordHasher          │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
                          ↓
┌───────────────────────────────────────────────────────────┐
│                 Application Layer                         │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Services/                                           │  │
│  │  ├─ ChatService → IChatService                      │  │
│  │  │   └→ IOllamaService, IPromptService,             │  │
│  │  │      IProductContextService, IOrderContextService│  │
│  │  │      IChatSessionRepository, IChatMessageRepository│ │
│  │  │                                                  │  │
│  │  ├─ OllamaService → IOllamaService                  │  │
│  │  │   └→ HttpClient (DI)                             │  │
│  │  │                                                  │  │
│  │  ├─ PromptService → IPromptService                  │  │
│  │  │                                                  │  │
│  │  ├─ ProductContextService → IProductContextService  │  │
│  │  │   └→ IProductRepository                          │  │
│  │  │                                                  │  │
│  │  ├─ OrderContextService → IOrderContextService      │  │
│  │  │   └→ IOrderRepository                            │  │
│  │  │                                                  │  │
│  │  └─ AuthService → IAuthService                      │  │
│  │      └→ IUserRepository, IJwtTokenService,          │  │
│  │         IPasswordHasher                             │  │
│  │                                                     │  │
│  │ DTOs/                                               │  │
│  │  ├─ ChatMessageDto, ChatSessionDto                  │  │
│  │  ├─ ChatStreamChunk                                 │  │
│  │  ├─ SendMessageRequest                              │  │
│  │  ├─ AuthDtos (LoginRequest/Response)                │  │
│  │  └─ ProductDto, OrderDto                            │  │
│  │                                                     │  │
│  │ Interfaces/ (Abstraction)                           │  │
│  │  ├─ Repositories/ (IRepository<T>, IChatSession...) │  │
│  │  └─ Services (IChatService, IOllama...)             │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
                          ↓
┌───────────────────────────────────────────────────────────┐
│                    Domain Layer                           │
│  ┌─────────────────────────────────────────────────────┐  │
│  │ Entities/                                           │  │
│  │  ├─ ChatSession                                     │  │
│  │  ├─ ChatMessage                                     │  │
│  │  ├─ Product                                         │  │
│  │  ├─ Order                                           │  │
│  │  ├─ FAQ                                             │  │
│  │  └─ User                                            │  │
│  │                                                     │  │
│  │ Enums/                                              │  │
│  │  ├─ ChatCategory (General/ProductSearch/FAQ...)     │  │
│  │  ├─ MessageRole (User/Assistant/System)             │  │
│  │  ├─ MessageStatus                                   │  │
│  │  └─ UserRole (Admin/Customer)                       │  │
│  │                                                     │  │
│  │ ⚠️ 외부 의존성 없음 (순수 C# 클래스)                │  │
│  └─────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────┘
```

### 4.2 의존성 주입(DI) 구성 상세

**파일**: `BicycleShopChatbot.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. 데이터베이스 등록 (PostgreSQL + pgvector)
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();  // pgvector 확장 활성화
    });
});

// ============================================
// 2. JWT 설정 등록 (Singleton)
// ============================================
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = new BicycleShopChatbot.Application.DTOs.JwtSettings();
jwtSettingsSection.Bind(jwtSettings);

// Application.DTOs.JwtSettings
builder.Services.AddSingleton<BicycleShopChatbot.Application.DTOs.JwtSettings>(jwtSettings);

// Infrastructure.Auth.JwtSettings (하위 호환성)
builder.Services.AddSingleton<BicycleShopChatbot.Infrastructure.Auth.JwtSettings>(sp =>
{
    var appSettings = sp.GetRequiredService<BicycleShopChatbot.Application.DTOs.JwtSettings>();
    return new BicycleShopChatbot.Infrastructure.Auth.JwtSettings
    {
        Secret = appSettings.Secret,
        Issuer = appSettings.Issuer,
        Audience = appSettings.Audience,
        AccessTokenExpirationMinutes = appSettings.AccessTokenExpirationMinutes,
        RefreshTokenExpirationDays = appSettings.RefreshTokenExpirationDays
    };
});

// ============================================
// 3. CORS 정책 설정
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ??
                new[] { "http://localhost:4200" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();  // SignalR을 위해 필수
    });
});

// ============================================
// 4. 인증/권한 부여 (Authentication & Authorization)
// ============================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };

    // SignalR에서 JWT 토큰 사용 설정
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ============================================
// 5. SignalR 등록
// ============================================
builder.Services.AddSignalR();

// ============================================
// 6. Repository 패턴 등록 (Scoped)
// ============================================
// Generic Repository
builder.Services.AddScoped<IRepository<ChatSession>, Repository<ChatSession>>();
builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
builder.Services.AddScoped<IRepository<ProductEmbedding>, Repository<ProductEmbedding>>();
builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
builder.Services.AddScoped<IRepository<FAQ>, Repository<FAQ>>();
builder.Services.AddScoped<IRepository<User>, Repository<User>>();

// Specialized Repository
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IVectorProductRepository, VectorProductRepository>();  // 벡터 검색
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ============================================
// 7. Application Services 등록 (Scoped)
// ============================================
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<IProductContextService, ProductContextService>();
builder.Services.AddScoped<IOrderContextService, OrderContextService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// ============================================
// 8. Embedding Service (전용 HttpClient)
// ============================================
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            ConnectTimeout = TimeSpan.FromSeconds(30)
        };
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(30));

// ============================================
// 9. Ollama Service (전용 HttpClient)
// ============================================
builder.Services.AddHttpClient<IOllamaService, OllamaService>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            MaxResponseHeadersLength = 128
        };
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(30));

// ============================================
// 10. 데이터베이스 시드 (Database Seeding)
// ============================================
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

    var seeder = new DatabaseSeeder(context, logger);
    await seeder.SeedAsync();
}

// ============================================
// 11. 미들웨어 파이프라인
// ============================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
builder.Services.AddScoped<IRepository<FAQ>, Repository<FAQ>>();
builder.Services.AddScoped<IRepository<User>, Repository<User>>();

builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// ============================================
// 7. 애플리케이션 서비스 등록 (Scoped)
// ============================================
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<IProductContextService, ProductContextService>();
builder.Services.AddScoped<IOrderContextService, OrderContextService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// ============================================
// 8. HttpClient Factory 패턴 (OllamaService)
// ============================================
builder.Services.AddHttpClient<IOllamaService, OllamaService>();

// ============================================
// 9. 로깅 설정
// ============================================
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// ============================================
// 10. Controllers 및 JSON 설정
// ============================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// 11. 앱 빌드 및 미들웨어 파이프라인
// ============================================
var app = builder.Build();

// 개발 환경 설정
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 데이터베이스 초기화 및 시드
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    context.Database.EnsureCreated();
    await DatabaseSeeder.SeedAsync(context);
}

// 미들웨어 순서 중요!
app.UseCors("AllowAngularApp");
app.UseAuthentication();    // ← JWT 인증
app.UseAuthorization();     // ← 권한 부여
app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");  // ← SignalR Hub 매핑

app.Run();
```

### 4.3 Lifetime 정책

| Lifetime | 설명 | 사용 예시 |
|----------|------|---------|
| **Singleton** | 앱 전체에서 하나의 인스턴스 | JwtSettings, IConfiguration |
| **Scoped** | HTTP 요청당 하나의 인스턴스 | DbContext, Repositories, Services |
| **Transient** | 호출마다 새 인스턴스 | 가벼운 유틸리티 |

**Scoped를 사용하는 이유**:
- **DbContext**: EF Core는 Scoped 권장 (동시성 문제 방지)
- **Repositories**: DbContext와 같은 Lifetime
- **Services**: Repository를 주입받으므로 Scoped

### 4.4 인터페이스 및 구현체 매핑 테이블

| 인터페이스 | 구현체 | 레이어 | Lifetime |
|-----------|--------|--------|---------|
| `IRepository<T>` | `Repository<T>` | Infrastructure | Scoped |
| `IChatSessionRepository` | `ChatSessionRepository` | Infrastructure | Scoped |
| `IChatMessageRepository` | `ChatMessageRepository` | Infrastructure | Scoped |
| `IProductRepository` | `ProductRepository` | Infrastructure | Scoped |
| `IOrderRepository` | `OrderRepository` | Infrastructure | Scoped |
| `IFAQRepository` | `FAQRepository` | Infrastructure | Scoped |
| `IUserRepository` | `UserRepository` | Infrastructure | Scoped |
| `IChatService` | `ChatService` | Application | Scoped |
| `IOllamaService` | `OllamaService` | Application | HttpClient Factory |
| `IPromptService` | `PromptService` | Application | Scoped |
| `IProductContextService` | `ProductContextService` | Application | Scoped |
| `IOrderContextService` | `OrderContextService` | Application | Scoped |
| `IAuthService` | `AuthService` | Application | Scoped |
| `IJwtTokenService` | `JwtTokenService` | Infrastructure | Scoped |
| `IPasswordHasher` | `BCryptPasswordHasher` | Infrastructure | Scoped |

---

## 5. 데이터 흐름

### 5.1 전체 데이터 흐름도

```
┌─────────────────────────────────────────────────────────────┐
│               클라이언트 (Angular Frontend)                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ChatComponent                                        │   │
│  │  ├─ SignalR Connection                               │   │
│  │  ├─ sendMessage()                                    │   │
│  │  └─ onReceiveMessageChunk()                          │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓ WebSocket (SignalR)
┌─────────────────────────────────────────────────────────────┐
│                      SignalR Hub                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ChatHub.SendMessageStream(SendMessageRequest)        │   │
│  │  ├─ 1️⃣ JWT 인증 확인 (Context.User)                  │   │
│  │  ├─ 2️⃣ UserId 강제 설정 (보안)                       │   │
│  │  ├─ 3️⃣ await foreach (chunk in ProcessStream())      │   │
│  │  └─ 4️⃣ SendAsync("ReceiveMessageChunk", chunk)       │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                     ChatService                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ProcessUserMessageStreamAsync()                      │   │
│  │  ├─ 1️⃣ 세션 조회/생성 (GetOrCreateSessionAsync)      │   │
│  │  ├─ 2️⃣ 사용자 메시지 저장 (MessageRepository)        │   │
│  │  ├─ 3️⃣ 대화 히스토리 조회 (최대 20개)                │   │
│  │  ├─ 4️⃣ Intent 감지 (PromptService.DetectIntent)      │   │
│  │  ├─ 5️⃣ 컨텍스트 생성 (BuildContextualPrompt)         │   │
│  │  ├─ 6️⃣ Ollama 스트리밍 호출                          │   │
│  │  ├─ 7️⃣ 청크 yield 반환 (IAsyncEnumerable)            │   │
│  │  ├─ 8️⃣ 응답 전체 누적 (StringBuilder)                │   │
│  │  ├─ 9️⃣ 어시스턴트 메시지 DB 저장                     │   │
│  │  └─ 🔟 세션 업데이트 (LastActivityAt, TotalMessages) │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                   PromptService (Intent 감지)               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ DetectIntent(userMessage)                            │   │
│  │  ├─ Contains("주문") → OrderStatus                   │   │
│  │  ├─ Contains("추천") → ProductSearch                 │   │
│  │  ├─ Contains("환불") → FAQ                           │   │
│  │  ├─ Contains("스펙") → ProductDetails                │   │
│  │  └─ Default → General                                │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│           컨텍스트 생성 (Intent별 분기)                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ProductSearch:                                       │   │
│  │  └→ ProductContextService.SearchProductsAsync()      │   │
│  │     └→ ProductRepository.SearchProducts (LIKE 검색)  │   │
│  │        └→ 제품 정보를 프롬프트에 주입                │   │
│  │                                                      │   │
│  │ OrderStatus:                                         │   │
│  │  └→ OrderContextService.GetByOrderNumber()           │   │
│  │     └→ OrderRepository (정규식으로 주문번호 추출)    │   │
│  │        └→ 주문 정보를 프롬프트에 주입                │   │
│  │                                                      │   │
│  │ FAQ:                                                 │   │
│  │  └→ FAQRepository.SearchFAQsAsync()                  │   │
│  │     └→ 관련 FAQ를 프롬프트에 주입                    │   │
│  │                                                      │   │
│  │ General/ProductDetails/CustomerSupport:              │   │
│  │  └→ GetSystemPrompt(category)                        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                   OllamaService                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ GenerateResponseStreamAsync()                        │   │
│  │  ├─ 1️⃣ 메시지 페이로드 구성                         │   │
│  │  │  ├─ role: "system" → systemPrompt                 │   │
│  │  │  ├─ role: "user/assistant" → history (최대 10개)  │   │
│  │  │  └─ role: "user" → userMessage                    │   │
│  │  ├─ 2️⃣ HTTP POST /api/chat (stream=true)            │   │
│  │  ├─ 3️⃣ JSON Lines 스트림 읽기                       │   │
│  │  ├─ 4️⃣ 각 청크 역직렬화                             │   │
│  │  └─ 5️⃣ yield return chunk.Content                   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓ HTTP
┌─────────────────────────────────────────────────────────────┐
│              Ollama 서버 (localhost:11434)                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ qwen2.5:7b 모델                                      │   │
│  │  ├─ 프롬프트 처리                                    │   │
│  │  ├─ 토큰 생성 (스트리밍)                             │   │
│  │  └─ JSON Lines 반환                                  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ↓ 청크 반환
┌─────────────────────────────────────────────────────────────┐
│                   클라이언트 수신                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ SignalR.on("ReceiveMessageChunk")                    │   │
│  │  └─ UI 업데이트 (실시간 타이핑 효과)                 │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 시퀀스 다이어그램

```
클라이언트      ChatHub       ChatService    PromptService   OllamaService    Ollama서버
   │              │               │                 │               │             │
   │ SendMessage  │               │                 │               │             │
   ├─────────────>│               │                 │               │             │
   │              │ JWT 검증      │                 │               │             │
   │              ├───────────────┤                 │               │             │
   │              │ ProcessStream │                 │               │             │
   │              ├──────────────>│                 │               │             │
   │              │               │ GetOrCreate     │               │             │
   │              │               │ Session         │               │             │
   │              │               ├─────────────────┤               │             │
   │              │               │ DetectIntent    │               │             │
   │              │               ├────────────────>│               │             │
   │              │               │ ProductSearch   │               │             │
   │              │               │<────────────────┤               │             │
   │              │               │ SearchProducts  │               │             │
   │              │               ├─────────────────┤               │             │
   │              │               │ BuildPrompt     │               │             │
   │              │               ├────────────────>│               │             │
   │              │               │ GenerateStream  │               │             │
   │              │               ├────────────────────────────────>│             │
   │              │               │                 │ POST /api/chat│             │
   │              │               │                 │               ├────────────>│
   │              │               │                 │               │ 청크1       │
   │              │               │                 │               │<────────────┤
   │              │               │ yield chunk1    │               │             │
   │              │               │<────────────────────────────────┤             │
   │              │ chunk1        │                 │               │             │
   │              │<──────────────┤                 │               │             │
   │ ReceiveChunk │               │                 │               │             │
   │<─────────────┤               │                 │               │             │
   │              │               │                 │               │ 청크2       │
   │              │               │                 │               │<────────────┤
   │              │               │ yield chunk2    │               │             │
   │              │               │<────────────────────────────────┤             │
   │              │ chunk2        │                 │               │             │
   │              │<──────────────┤                 │               │             │
   │ ReceiveChunk │               │                 │               │             │
   │<─────────────┤               │                 │               │             │
   │              │               │                 │               │ ...         │
   │              │               │                 │               │ Done=true   │
   │              │               │                 │               │<────────────┤
   │              │               │ SaveMessage     │               │             │
   │              │               ├─────────────────┤               │             │
   │              │               │ UpdateSession   │               │             │
   │              │               ├─────────────────┤               │             │
   │              │ Complete      │                 │               │             │
   │              │<──────────────┤                 │               │             │
   │ Complete     │               │                 │               │             │
   │<─────────────┤               │                 │               │             │
```

### 5.3 Intent 감지 흐름

```
사용자 메시지: "100만원 예산으로 출퇴근용 자전거 추천해주세요"
                          ↓
┌─────────────────────────────────────────────────────────────────┐
│ PromptService.DetectIntent(userMessage)                         │
│  ├─ ToLower() → "100만원 예산으로 출퇴근용 자전거 추천해주세요" │
│  ├─ Contains("주문") → ❌                                      │
│  ├─ Contains("추천") → ✅ ProductSearch                        │
│  └─ Return ChatCategory.ProductSearch                           │
└─────────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ ChatService.BuildContextualPromptAsync()                │
│  └─ intent == ProductSearch                             │
│     └─ BuildProductSearchPromptAsync()                  │
│        ├─ ProductContextService.SearchProductsAsync()   │
│        │  └─ Repository: Contains("출퇴근") 검색        │
│        │     → 하이브리드/도시형 자전거 반환            │
│        └─ PromptService.GetProductSearchPrompt()        │
│           └─ 시스템 프롬프트 + 제품 정보 결합           │
└─────────────────────────────────────────────────────────┘
                          ↓
생성된 프롬프트:
"""
당신은 대한민국의 자전거 전문 온라인 쇼핑몰 AI 상담원입니다.
...
예산 준수: 고객이 예산을 제시한 경우, 예산 이하 또는 예산의 +10% 이내...

## 현재 판매 중인 제품:

### 트렉 FX 3 디스크 (Trek FX 3 Disc)
- **가격**: 950,000원
- **카테고리**: Hybrid
- **설명**: 출퇴근과 운동을 겸할 수 있는 다목적 하이브리드...

### 자이언트 이스케이프 3 (Giant Escape 3)
- **가격**: 580,000원
- **카테고리**: Hybrid
- **설명**: 가성비 좋은 입문용 하이브리드...
"""
```

---

## 6. 데이터베이스 설계

### 6.1 ERD (Entity Relationship Diagram)

```
┌─────────────────────────┐
│        User              │
│─────────────────────────│
│ Id (PK)                 │───┐
│ Email (Unique)          │   │
│ UserName                │   │
│ PasswordHash            │   │
│ Role (Admin/Customer)   │   │
│ CreatedAt               │   │
│ LastLoginAt             │   │
│ RefreshToken            │   │
│ RefreshTokenExpiryTime  │   │
│ IsActive                │   │
└─────────────────────────┘   │
                              │ 1
                              │
                              │ N
┌─────────────────────────────────────┐
│        ChatSession                   │
│─────────────────────────────────────│
│ Id (PK, GUID)                       │───┐
│ SessionId (Client ID)               │   │
│ UserId (FK) ────────────────────────┘   │
│ UserName                                │
│ Title                                   │
│ CreatedAt                               │
│ LastActivityAt                          │
│ IsActive                                │
│ SessionMetadata (JSON)                  │
│ TotalMessages                           │
└─────────────────────────────────────┘   │
                                          │ 1
                                          │
                                          │ N
┌──────────────────────────────────────────────────┐
│              ChatMessage                          │
│──────────────────────────────────────────────────│
│ Id (PK, Auto-increment)                          │
│ ChatSessionId (FK) ──────────────────────────────┘
│ Role (User/Assistant/System)                     │
│ Content (Text)                                   │
│ Timestamp                                        │
│ Category (General/ProductSearch/FAQ...)          │
│ IntentDetected                                   │
│ ProductId (FK, Nullable) ────────────────────────┐
│ OrderId (FK, Nullable) ──────────────────────────┼─┐
│ Metadata (JSON)                                  │ │
│ TokensUsed                                       │ │
│ ProcessingTimeMs                                 │ │
└──────────────────────────────────────────────────┘ │
                                                     │
     ┌───────────────────────────────────────────────┘
     │                                               │
     │ N                                             │ N
     │ 1                                             │ 1
┌────▼──────────────────────┐   ┌──────────────────▼────┐
│   Product (Product)       │   │        Order           │
│ (product_embeddings 매핑) │   │───────────────────────│
│──────────────────────────│   │ Id (PK)               │
│ Id (PK)                  │   │ OrderNumber (Unique)  │
│ ProductCode (Unique)     │   │ CustomerEmail         │
│ Name                     │   │ CustomerPhone         │
│ NameKorean               │   │ Status                │
│ Category                 │   │ OrderDate             │
│ Brand                    │   │ TotalAmount           │
│ Price                    │   │ ShippingAddress       │
│ Description              │   │ TrackingNumber        │
│ DescriptionKorean        │   │ EstimatedDelivery     │
│ Specifications (JSON)    │   │ UpdatedAt             │
│ StockQuantity            │   └───────────────────────┘
│ IsAvailable              │
│ ImageUrl                 │
│ CreatedAt                │
│ UpdatedAt                │
└──────────────────────────┘
     ↕ (매핑)
┌──────────────────────────────┐
│   ProductEmbedding           │
│   (product_embeddings 테이블)│
│──────────────────────────────│
│ * Product와 동일 스키마      │
│ + embedding vector(768)      │
└──────────────────────────────┘

┌──────────────────────────┐
│         FAQ              │
│─────────────────────────│
│ Id (PK)                 │
│ Question                │
│ QuestionKorean          │
│ Answer                  │
│ AnswerKorean            │
│ Category                │
│ Keywords                │
│ ViewCount               │
│ IsActive                │
│ CreatedAt               │
└──────────────────────────┘
```

**중요**: Product 엔티티는 기존 `product_embeddings` 테이블을 매핑하여 사용합니다. 
ProductEmbedding은 벡터 검색 전용 엔티티입니다.

### 6.2 엔티티 상세 설명

#### User 엔티티

```csharp
public class User
{
    public int Id { get; set; }                          // Primary Key

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; }                    // 이메일 (Unique Index)

    [Required, MinLength(3), MaxLength(100)]
    public string UserName { get; set; }                 // 사용자명

    [Required]
    public string PasswordHash { get; set; }             // BCrypt 해시

    public UserRole Role { get; set; }                   // Admin = 0, Customer = 1

    public DateTime CreatedAt { get; set; }              // 생성일
    public DateTime? LastLoginAt { get; set; }           // 마지막 로그인
    public string? RefreshToken { get; set; }            // JWT Refresh Token
    public DateTime? RefreshTokenExpiryTime { get; set; }// Refresh Token 만료일
    public bool IsActive { get; set; } = true;           // 활성화 상태

    // Navigation Properties
    public ICollection<ChatSession> ChatSessions { get; set; }
}
```

**Fluent API 설정**:
```csharp
builder.HasIndex(u => u.Email).IsUnique();
builder.Property(u => u.PasswordHash).HasMaxLength(512);
```

#### ChatSession 엔티티

```csharp
public class ChatSession
{
    public Guid Id { get; set; }                         // Primary Key (GUID)

    [Required, MaxLength(255)]
    public string SessionId { get; set; }                // 클라이언트 세션 ID

    public int? UserId { get; set; }                     // Foreign Key (User)

    [MaxLength(100)]
    public string? UserName { get; set; }                // 비로그인 사용자명

    [MaxLength(500)]
    public string? Title { get; set; }                   // 세션 제목 (자동 생성)

    public DateTime CreatedAt { get; set; }              // 생성일
    public DateTime LastActivityAt { get; set; }         // 마지막 활동
    public bool IsActive { get; set; } = true;           // 활성화 상태
    public string? SessionMetadata { get; set; }         // JSON 메타데이터
    public int TotalMessages { get; set; } = 0;          // 총 메시지 수

    // Navigation Properties
    public User? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; }
}
```

**Fluent API 설정**:
```csharp
builder.HasIndex(s => s.SessionId);
builder.HasIndex(s => s.UserId);
builder.HasIndex(s => s.LastActivityAt);
builder.HasOne(s => s.User)
       .WithMany(u => u.ChatSessions)
       .HasForeignKey(s => s.UserId)
       .OnDelete(DeleteBehavior.Cascade);
```

#### ChatMessage 엔티티

```csharp
public class ChatMessage
{
    public long Id { get; set; }                         // Primary Key (Auto-increment)

    public Guid ChatSessionId { get; set; }              // Foreign Key (ChatSession)

    public MessageRole Role { get; set; }                // User=0/Assistant=1/System=2

    [Required]
    public string Content { get; set; }                  // 메시지 본문

    public DateTime Timestamp { get; set; }              // 타임스탬프
    public ChatCategory? Category { get; set; }          // 카테고리 (Nullable)

    [MaxLength(100)]
    public string? IntentDetected { get; set; }          // 감지된 의도

    public int? ProductId { get; set; }                  // Foreign Key (Product)
    public int? OrderId { get; set; }                    // Foreign Key (Order)
    public string? Metadata { get; set; }                // JSON 메타데이터
    public int? TokensUsed { get; set; }                 // 사용된 토큰 (향후)
    public int? ProcessingTimeMs { get; set; }           // 처리 시간 (ms)

    // Navigation Properties
    public ChatSession ChatSession { get; set; }
    public Product? Product { get; set; }
    public Order? Order { get; set; }
}
```

**Fluent API 설정**:
```csharp
builder.HasIndex(m => m.ChatSessionId);
builder.HasIndex(m => m.Timestamp);
builder.HasOne(m => m.ChatSession)
       .WithMany(s => s.Messages)
       .HasForeignKey(m => m.ChatSessionId)
       .OnDelete(DeleteBehavior.Cascade);
builder.HasOne(m => m.Product)
       .WithMany(p => p.ChatMessages)
       .HasForeignKey(m => m.ProductId)
       .OnDelete(DeleteBehavior.SetNull);
```

#### Product 엔티티

```csharp
public class Product
{
    public int Id { get; set; }                          // Primary Key

    [Required, MaxLength(50)]
    public string ProductCode { get; set; }              // SKU (Unique)

    [Required, MaxLength(200)]
    public string Name { get; set; }                     // 영문명

    [Required, MaxLength(200)]
    public string NameKorean { get; set; }               // 한글명

    [Required, MaxLength(50)]
    public string Category { get; set; }                 // Road/Mountain/Hybrid/Electric

    [Required, MaxLength(100)]
    public string Brand { get; set; }                    // 브랜드

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }                   // 가격

    [MaxLength(1000)]
    public string? Description { get; set; }             // 영문 설명

    [MaxLength(1000)]
    public string? DescriptionKorean { get; set; }       // 한글 설명

    [Required]
    public string Specifications { get; set; }           // JSON 스펙

    public int StockQuantity { get; set; }               // 재고
    public bool IsAvailable { get; set; } = true;        // 판매 가능

    [MaxLength(500)]
    public string? ImageUrl { get; set; }                // 이미지 URL

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<ChatMessage> ChatMessages { get; set; }
}
```

**Fluent API 설정**:
```csharp
builder.HasIndex(p => p.ProductCode).IsUnique();
builder.HasIndex(p => p.Category);
builder.HasIndex(p => p.Brand);
builder.HasIndex(p => p.IsAvailable);
```

### 6.3 Repository 패턴 구현

#### Generic Repository

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

#### 특화 Repository 예시 (ChatSessionRepository)

```csharp
public class ChatSessionRepository : Repository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ChatSession?> GetBySessionIdAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Messages.OrderBy(m => m.Timestamp).Take(100))
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(
        int userId,
        int maxSessions = 30,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .Take(maxSessions)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteOldUserSessionsAsync(
        int userId,
        int keepCount = 30,
        CancellationToken cancellationToken = default)
    {
        var sessionsToDelete = await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .Skip(keepCount)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(sessionsToDelete);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## 7. 보안 및 인증

### 7.1 JWT 인증 시스템

#### JWT 토큰 구조

**Access Token (15분 만료)**:
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "nameid": "1",                          // User ID
    "email": "user@example.com",
    "name": "username",
    "role": "Customer",
    "iss": "BicycleShopChatbot",
    "aud": "BicycleShopChatbotUsers",
    "exp": 1704283440,                      // 15분 후
    "iat": 1704282840
  },
  "signature": "HMAC-SHA256(secret)"
}
```

**Refresh Token (7일 만료)**:
- Base64 인코딩된 64바이트 랜덤 문자열
- 데이터베이스에 저장 (User.RefreshToken)
- Access Token 갱신 시 사용

#### JwtTokenService 구현

```csharp
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

### 7.2 BCrypt 비밀번호 해싱

```csharp
public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // BCrypt.Net 사용 (WorkFactor = 11)
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

**WorkFactor**: 11 (2^11 = 2048 iterations)
- 낮음 (4-6): 빠르지만 덜 안전
- 권장 (10-12): 균형
- 높음 (13+): 매우 안전하지만 느림

### 7.3 인증 흐름

#### 로그인 흐름

```
클라이언트
    ↓ POST /api/auth/login
    Body: { email, password }
AuthController
    ↓
AuthService.LoginAsync()
    ├─ 1. UserRepository.GetByEmailAsync(email)
    ├─ 2. PasswordHasher.VerifyPassword(password, user.PasswordHash)
    ├─ 3. JwtTokenService.GenerateAccessToken(user)
    ├─ 4. JwtTokenService.GenerateRefreshToken()
    ├─ 5. User.RefreshToken 저장
    └─ 6. LoginResponse 반환
        ├─ AccessToken
        ├─ RefreshToken
        ├─ ExpiresAt
        └─ User Info
    ↓
클라이언트
    ├─ localStorage.setItem("accessToken", token)
    ├─ localStorage.setItem("refreshToken", refreshToken)
    └─ HTTP Interceptor에서 Authorization 헤더 추가
```

#### SignalR 인증

```csharp
// Program.cs
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;

        if (!string.IsNullOrEmpty(accessToken) &&
            path.StartsWithSegments("/hub/chat"))
        {
            context.Token = accessToken;
        }

        return Task.CompletedTask;
    }
};
```

**클라이언트 연결 예시 (Angular)**:
```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5042/hub/chat', {
    accessTokenFactory: () => localStorage.getItem('accessToken')
  })
  .build();
```

### 7.4 권한 부여 (Authorization)

```csharp
[Authorize]  // ← 인증 필수
public class ChatController : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetUserSessions()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        // ...
    }

    [HttpDelete("sessions/{sessionId}")]
    [Authorize(Roles = "Admin")]  // ← Admin 권한 필요
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        // ...
    }
}
```

---

## 8. 성능 최적화

### 8.1 스트리밍 최적화

**IAsyncEnumerable의 장점**:

| 항목 | 비스트리밍 | 스트리밍 |
|------|----------|---------|
| **메모리 사용** | 전체 응답 로드 | 청크 단위 처리 |
| **첫 응답 시간** | 전체 완료 후 | 즉시 시작 |
| **사용자 경험** | 대기 필요 | 실시간 타이핑 |
| **대용량 응답** | OOM 위험 | 안전 |

**예시**:
```
응답 크기: 10KB (2000 토큰)
생성 시간: 30초

비스트리밍:
├─ 사용자 대기: 30초
└─ 첫 글자 표시: 30초 후

스트리밍:
├─ 첫 글자 표시: 2초 후
└─ 완료: 30초 (동일)
```

### 8.2 데이터베이스 최적화

#### 인덱스 전략

```csharp
// ChatSession
builder.HasIndex(s => s.SessionId);          // 세션 ID 조회
builder.HasIndex(s => s.UserId);             // 사용자별 세션
builder.HasIndex(s => s.LastActivityAt);     // 최근 활동 정렬

// ChatMessage
builder.HasIndex(m => m.ChatSessionId);      // 세션별 메시지
builder.HasIndex(m => m.Timestamp);          // 시간 정렬

// Product
builder.HasIndex(p => p.ProductCode).IsUnique();
builder.HasIndex(p => p.Category);           // 카테고리 필터
builder.HasIndex(p => p.Brand);              // 브랜드 필터
builder.HasIndex(p => p.IsAvailable);        // 판매 가능 필터
```

#### Eager/Lazy Loading

```csharp
// Eager Loading (권장 - N+1 문제 방지)
var session = await _dbSet
    .Include(s => s.Messages.OrderBy(m => m.Timestamp).Take(100))
    .Include(s => s.User)
    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

// Lazy Loading (지양)
var session = await _dbSet.FirstOrDefaultAsync(s => s.SessionId == sessionId);
// session.Messages 접근 시 추가 쿼리 발생 (N+1 문제)
```

### 8.3 쿼리 최적화 전략

#### 페이지네이션

```csharp
public async Task<List<Product>> GetProductsAsync(
    int page = 1,
    int pageSize = 10,
    CancellationToken cancellationToken = default)
{
    return await _dbSet
        .Where(p => p.IsAvailable)
        .OrderBy(p => p.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);
}
```

#### 프로젝션 (필요한 컬럼만 조회)

```csharp
// ❌ 나쁨: 전체 엔티티 조회
var products = await _dbSet.ToListAsync();

// ✅ 좋음: 필요한 컬럼만 조회
var products = await _dbSet
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.NameKorean,
        Price = p.Price
    })
    .ToListAsync();
```

### 8.4 캐싱 전략 (향후 구현)

```csharp
// In-Memory Cache 예시
public class ProductService
{
    private readonly IMemoryCache _cache;

    public async Task<List<Product>> GetPopularProductsAsync()
    {
        const string cacheKey = "popular_products";

        if (!_cache.TryGetValue(cacheKey, out List<Product> products))
        {
            products = await _productRepository.GetPopularProductsAsync();

            _cache.Set(cacheKey, products, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }

        return products;
    }
}
```

---

## 부록

### A. 환경 설정

#### A.1 Ollama 설치

```bash
# macOS
brew install ollama

# Linux
curl -fsSL https://ollama.com/install.sh | sh

# Windows
# https://ollama.com/download/windows에서 설치

# 모델 다운로드
ollama pull qwen2.5:7b

# 서버 실행
ollama serve
```

#### A.2 데이터베이스 마이그레이션

```bash
cd backend/src/BicycleShopChatbot.Api

# 마이그레이션 생성
dotnet ef migrations add InitialCreate --project ../BicycleShopChatbot.Infrastructure

# 데이터베이스 업데이트
dotnet ef database update --project ../BicycleShopChatbot.Infrastructure
```

#### A.3 환경 변수

**appsettings.json** (실제 구성):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=postgres;Username=postgres;Password=postgres"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "qwen2.5:14b",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": "120",
    "MaxRetries": "3",
    "RetryDelaySeconds": "2",
    "DefaultTemperature": "0.7"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-at-least-32-characters-long-change-in-production",
    "Issuer": "BicycleShopChatbot",
    "Audience": "BicycleShopChatbotUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": "http://localhost:4200"
  }
}
```

**PostgreSQL 설정**:
```bash
# PostgreSQL + pgvector 설치 (Ubuntu/Debian)
sudo apt install postgresql postgresql-contrib
sudo apt install postgresql-16-pgvector

# PostgreSQL 시작
sudo systemctl start postgresql

# pgvector 확장 활성화
psql -U postgres
CREATE EXTENSION vector;
```

#### A.4 시드 데이터

**DatabaseSeeder.cs** 실행 시 자동 생성:
- **Users**: 2명 (admin, customer)
- **Products**: 10개 (자전거 제품)
- **ProductEmbeddings**: 10개 (벡터 임베딩)
- **FAQs**: 20개 (자주 묻는 질문)
- **Orders**: 3개 (샘플 주문)

**시드 실행**:
```csharp
// Program.cs에서 자동 실행
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    var seeder = new DatabaseSeeder(context, logger);
    await seeder.SeedAsync();
}
```

### B. API 엔드포인트 목록

#### B.1 인증 API

| 메서드 | 경로 | 설명 | 인증 |
|--------|------|------|------|
| POST | /api/auth/register | 회원가입 | ❌ |
| POST | /api/auth/login | 로그인 | ❌ |
| POST | /api/auth/refresh | 토큰 갱신 | ❌ |
| GET | /api/auth/me | 현재 사용자 정보 | ✅ |

#### B.2 채팅 API

| 메서드 | 경로 | 설명 | 인증 |
|--------|------|------|------|
| GET | /api/chat/sessions | 사용자 세션 목록 | ✅ |
| GET | /api/chat/sessions/{id} | 세션 상세 | ✅ |
| DELETE | /api/chat/sessions/{id} | 세션 삭제 | ✅ |

#### B.3 SignalR Hub

| 이벤트 | 설명 | 방향 |
|--------|------|------|
| SendMessage | 메시지 전송 (비스트리밍) | Client → Server |
| SendMessageStream | 메시지 전송 (스트리밍) | Client → Server |
| ReceiveMessage | 메시지 수신 (비스트리밍) | Server → Client |
| ReceiveMessageChunk | 청크 수신 (스트리밍) | Server → Client |
| StreamError | 스트리밍 에러 | Server → Client |

---

## 결론

본 자전거 쇼핑몰 챗봇 백엔드는 **Clean Architecture**를 완벽하게 적용하여 유지보수성과 확장성을 극대화했습니다.

### 핵심 구현 완료 사항

✅ **Ollama 로컬 AI 통합**: qwen2.5:14b 모델을 사용한 한국어 대화  
✅ **스트리밍 응답**: IAsyncEnumerable 기반 실시간 응답  
✅ **SignalR 실시간 통신**: WebSocket 기반 양방향 통신  
✅ **JWT 인증**: Access Token + Refresh Token 기반 인증  
✅ **Repository 패턴**: Generic + Specialized Repository 구현  
✅ **의도 감지**: PromptService 기반 자동 카테고리 분류  
✅ **벡터 RAG 구현**: PostgreSQL + pgvector 기반 의미적 검색  
✅ **임베딩 생성**: nomic-embed-text 모델 통합 (768차원)  
✅ **VectorProductRepository**: 코사인 유사도 기반 제품 검색  
✅ **HttpClient 최적화**: Connection Pooling + Keep-alive 설정  
✅ **데이터베이스 시딩**: 자동 샘플 데이터 생성  
✅ **에러 처리**: Structured Logging + Exception Handling  

### 아키텍처 하이라이트

**레이어 분리**:
- **Domain**: 비즈니스 엔티티 (User, ChatSession, Product 등)
- **Application**: 서비스 로직 (ChatService, OllamaService 등)
- **Infrastructure**: 데이터 액세스 (Repositories, DbContext)
- **API**: 웹 인터페이스 (Controllers, SignalR Hubs)

**데이터베이스**:
- PostgreSQL 16 + pgvector 확장
- EF Core 9.0 (Code-First 마이그레이션)
- 기존 product_embeddings 테이블 재사용

**AI 통합**:
- Ollama HTTP API (스트리밍/비스트리밍)
- 재시도 로직 (Exponential Backoff)
- 타임아웃 및 에러 핸들링

### 성능 최적화

- **Eager Loading**: Include()로 N+1 문제 방지
- **Vector Indexing**: IVFFlat 인덱스 (lists=100)
- **Connection Pooling**: 15분 Connection Lifetime
- **Keep-alive**: 60초 Ping 간격
- **Rate Limiting**: 임베딩 API 호출 간 100ms 딜레이

### 향후 개선 방향

🔜 **캐싱 시스템**: Redis 기반 응답 캐싱  
🔜 **Rate Limiting**: API 호출 제한 미들웨어  
🔜 **모니터링**: Prometheus + Grafana 통합  
🔜 **테스트**: 단위/통합 테스트 추가 (xUnit)  
🔜 **FAQ 벡터화**: FAQ도 벡터 검색 지원  
🔜 **하이브리드 검색**: 벡터 + 키워드 스코어 결합  
🔜 **사용자 피드백**: 응답 평가 시스템  

---

**문서 버전**: 2.0  
**최종 수정**: 2026-01-12  
**작성자**: GitHub Copilot (Claude Sonnet 4.5)  
**프로젝트**: 자전거 쇼핑몰 AI 챗봇  
