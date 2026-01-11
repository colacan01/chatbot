# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AI chatbot system for a bicycle online shop using Ollama LLM (qwen2.5/exaone models), built with ASP.NET Core 9.0 backend and Angular 21 frontend. The system provides real-time chat using SignalR with Korean language support.

## Development Commands

### Backend (.NET)

```bash
# Build the solution
cd backend
dotnet restore
dotnet build

# Run the API server
cd src/BicycleShopChatbot.Api
dotnet run
# Server runs at http://localhost:5069

# Run specific project
dotnet run --project src/BicycleShopChatbot.Api/BicycleShopChatbot.Api.csproj

# Clean and rebuild
dotnet clean
dotnet build --no-incremental

# Add migration (EF Core)
dotnet ef migrations add MigrationName --project src/BicycleShopChatbot.Infrastructure --startup-project src/BicycleShopChatbot.Api

# Update database
dotnet ef database update --project src/BicycleShopChatbot.Infrastructure --startup-project src/BicycleShopChatbot.Api
```

### Frontend (Angular)

```bash
cd frontend

# Install dependencies
npm install

# Development server
npm start
# Runs at http://localhost:4200

# Build
npm run build

# Watch mode
npm run watch

# Test
npm test
```

### Ollama Setup

```bash
# Install Ollama (if not installed)
curl -fsSL https://ollama.com/install.sh | sh

# Pull the model (check appsettings.json for current model)
ollama pull qwen2.5:14b
# or
ollama pull qwen2.5:7b
# or
ollama pull exaone3.5:7.8b

# Start Ollama server (usually runs automatically)
ollama serve
# Default: http://localhost:11434
```

## Architecture

### Clean Architecture Layers

The backend follows Clean Architecture with strict dependency rules:

```
API (Controllers, Hubs)
  ↓
Infrastructure (DbContext, Repositories, External Services)
  ↓
Application (Business Logic, Services, DTOs)
  ↓
Domain (Entities, Enums)
```

**Dependency Rule**: Outer layers depend on inner layers, never the reverse. Domain has no dependencies.

### Core Projects

- `BicycleShopChatbot.Domain` - Core entities (ChatSession, ChatMessage, Product, Order, FAQ, User)
- `BicycleShopChatbot.Application` - Business logic services and interfaces
- `BicycleShopChatbot.Infrastructure` - EF Core, repositories, external integrations
- `BicycleShopChatbot.Api` - ASP.NET Core API with SignalR hubs

### Key Services (Application Layer)

- **ChatService** (`ChatService.cs:14-300+`) - Main orchestrator for chat logic
  - Coordinates between Ollama, prompt engineering, and context services
  - Manages conversation history and intent detection
  - Provides both streaming (`ProcessUserMessageStreamAsync`) and non-streaming responses

- **OllamaService** (`OllamaService.cs`) - Ollama API integration
  - Handles HTTP communication with Ollama
  - Supports streaming and non-streaming responses
  - Implements retry logic with exponential backoff
  - Configurable timeout (default 300s in appsettings.json)

- **PromptService** (`PromptService.cs`) - Prompt engineering
  - Intent detection (ProductSearch, OrderStatus, FAQ, General)
  - System prompt generation with product/order context
  - Korean language optimization

- **ProductContextService** - Retrieves relevant products for context
- **OrderContextService** - Fetches order information for queries
- **AuthService** - JWT-based authentication with refresh tokens

### SignalR Hub

**ChatHub** (`backend/src/BicycleShopChatbot.Api/Hubs/ChatHub.cs`) - Real-time communication
- Requires JWT authentication (`[Authorize]` attribute)
- Hub methods:
  - `SendMessage(SendMessageRequest)` - Non-streaming chat
  - `SendMessageStream(SendMessageRequest)` - Streaming chat
  - `JoinSession(sessionId)` - Join chat session
  - `LeaveSession(sessionId)` - Leave session
  - `LoadSessionHistory(sessionId)` - Load past messages
- Client events:
  - `ReceiveMessage` - Full message response
  - `ReceiveMessageChunk` - Streaming chunk
  - `Error` - Error notification
  - `SessionHistoryLoaded` - History loaded

### Frontend Architecture

Angular standalone components with RxJS state management:

```
src/app/
├── core/
│   ├── services/
│   │   ├── signalr.service.ts - SignalR connection management
│   │   ├── chat.service.ts - Chat state management
│   │   ├── auth.service.ts - Authentication
│   │   └── token.service.ts - JWT token handling
│   └── models/ - TypeScript interfaces
├── features/
│   ├── chat/ - Chat components
│   └── auth/ - Login/register components
└── shared/ - Reusable components
```

**SignalR Integration**: Frontend connects to `/hubs/chat` with JWT token via query string (`?access_token=...`)

## Database Schema

SQLite for development (switch to PostgreSQL for production).

### Key Entities

**ChatSession** - Conversation sessions
- `SessionId` (string, unique identifier)
- `UserId` (int, FK to User)
- `IsActive`, `CreatedAt`, `LastActivityAt`
- Navigation: `Messages` (1:N)

**ChatMessage** - Individual messages
- `Role` (User, Assistant, System)
- `Content`, `Category`, `IntentDetected`
- `ProcessingTimeMs`, `TokensUsed` - Performance metrics
- Foreign keys: `ChatSessionId`, `ProductId`, `OrderId`

**Product** - Bicycle catalog
- `ProductCode`, `Name`, `NameKorean`
- `Category` (Road, Mountain, Hybrid, Electric)
- `Price`, `StockQuantity`, `IsAvailable`

**User** - User accounts (JWT authentication)
- `Email`, `PasswordHash`
- `DisplayName`, `CreatedAt`

**Order** - Customer orders
- `OrderNumber`, `Status`, `TrackingNumber`
- `CustomerEmail`, `TotalAmount`

**FAQ** - Frequently asked questions
- `QuestionKorean`, `AnswerKorean`
- `Category`, `Keywords`

## Configuration

### appsettings.json Structure

Located at `backend/src/BicycleShopChatbot.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bicycleshop.db"
  },
  "JwtSettings": {
    "Secret": "YourSecretKey...",
    "Issuer": "BicycleShopChatbot",
    "Audience": "BicycleShopChatbotUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "qwen2.5:14b",
    "TimeoutSeconds": "300",
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

**IMPORTANT**: Update `Ollama.BaseUrl` if running Ollama on a different machine (e.g., WSL, Docker).

## Dependency Injection

All service registrations are in `Program.cs:16-154`:

- **DbContext**: Scoped (`AddDbContext`)
- **Repositories**: Scoped (generic `IRepository<T>` and specialized interfaces)
- **Services**: Scoped (IChatService, IAuthService, IPromptService, etc.)
- **OllamaService**: HttpClient factory with custom `SocketsHttpHandler`
- **JwtSettings**: Singleton (registered twice for compatibility)

## Intent Detection

The system categorizes user messages into:

- `ProductSearch` - Keywords: "추천", "찾아", "자전거"
- `OrderStatus` - Keywords: "주문", "배송", "송장"
- `FAQ` - Keywords: "환불", "교환", "반품"
- `General` - Fallback category

Detected in `PromptService.DetectIntent()` method.

## Streaming vs Non-Streaming

**Non-streaming** (`SendMessage`):
- Single complete response after AI generation
- Simpler client handling
- Higher perceived latency

**Streaming** (`SendMessageStream`):
- Token-by-token response via `ReceiveMessageChunk` events
- Better UX for long responses
- Requires client-side chunk assembly

Set `stream: true/false` in `OllamaService` API calls.

## Authentication Flow

1. User registers/logs in via `AuthService.RegisterAsync()` or `LoginAsync()`
2. Server returns JWT access token (15 min) + refresh token (7 days)
3. Frontend stores tokens via `TokenService`
4. SignalR connection includes token: `?access_token={jwt}`
5. Hub extracts user ID from `ClaimTypes.NameIdentifier`
6. Refresh token before expiration using `AuthService.RefreshTokenAsync()`

## Database Seeding

On startup, `DatabaseSeeder.SeedAsync()` runs:
- 10 products (road bikes, mountain bikes, electric, etc.)
- 20 FAQs (returns, shipping, assembly, warranty)
- 3 sample orders (shipped, processing, delivered)

Data is only seeded if tables are empty.

## Common Patterns

### Adding a New Entity

1. Create entity in `Domain/Entities/`
2. Add interface in `Application/Interfaces/Repositories/`
3. Implement repository in `Infrastructure/Repositories/Implementation/`
4. Add DbSet to `ApplicationDbContext`
5. Register in `Program.cs` DI container
6. Create migration: `dotnet ef migrations add AddNewEntity`

### Adding a New Service

1. Define interface in `Application/Interfaces/`
2. Implement in `Application/Services/`
3. Register in `Program.cs`: `builder.Services.AddScoped<IMyService, MyService>()`

### Adding a SignalR Method

1. Add method to `ChatHub.cs`
2. Extract authenticated user from `Context.User.FindFirst(ClaimTypes.NameIdentifier)`
3. Send response via `Clients.Caller.SendAsync("EventName", data)`
4. Update frontend `SignalrService` to handle event

## Performance Considerations

**Ollama Response Times**:
- qwen2.5:14b (9GB): 1.5-2 min per response
- qwen2.5:7b (4GB): 20-40 sec per response
- exaone3.5:7.8b: Similar to 7b models

**Optimization Strategies**:
- Use smaller models (7b instead of 14b)
- Enable streaming for better perceived performance
- Limit conversation history (MaxConversationHistory: 20)
- Reduce product context (top 3-5 most relevant)
- Increase timeout if needed (TimeoutSeconds in appsettings.json)

**HttpClient Configuration** (`Program.cs:132-153`):
- Connection pooling with 15-min lifetime
- Keep-alive pings every 60 sec
- Max 10 connections per server
- 30 sec connect timeout, separate from request timeout

## Known Issues

1. **Long first request**: Ollama loads model into memory on first call (30s-3min depending on model size)
2. **SignalR timeout**: For very long responses without streaming, connection may timeout
3. **CORS**: Must configure `AllowedOrigins` in appsettings.json for frontend domain
4. **JWT secret**: Default secret in appsettings.json is for development only - change for production

## File Locations

- Solution file: `backend/BicycleShopChatbot.sln`
- Main database: `backend/src/BicycleShopChatbot.Api/bicycleshop.db`
- Environment config: `frontend/src/environments/environment.ts`
- Startup entry: `backend/src/BicycleShopChatbot.Api/Program.cs`
- Chat orchestrator: `backend/src/BicycleShopChatbot.Application/Services/ChatService.cs`
- SignalR hub: `backend/src/BicycleShopChatbot.Api/Hubs/ChatHub.cs`
- Frontend chat service: `frontend/src/app/core/services/chat.service.ts`
- Frontend SignalR service: `frontend/src/app/core/services/signalr.service.ts`
