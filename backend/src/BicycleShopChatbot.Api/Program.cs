using System.Text;
using BicycleShopChatbot.Api.Hubs;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Services;
using BicycleShopChatbot.Application.Configuration;
using BicycleShopChatbot.Application.Plugins;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Auth;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Infrastructure.Repositories.Implementation;
using BicycleShopChatbot.Infrastructure.AI.SemanticKernel;
using BicycleShopChatbot.Infrastructure.AI.Reranking;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using BicycleShopChatbot.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.UseVector();
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ??
                new[] { "http://localhost:4200" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT 설정
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
var jwtSettings = new BicycleShopChatbot.Application.DTOs.JwtSettings();
jwtSettingsSection.Bind(jwtSettings);

Console.WriteLine($"🔑 JWT Secret loaded: {(!string.IsNullOrEmpty(jwtSettings.Secret) ? "Yes" : "No")} (Length: {jwtSettings.Secret?.Length ?? 0})");
Console.WriteLine($"🔑 JWT Issuer: {jwtSettings.Issuer}");
Console.WriteLine($"🔑 JWT Audience: {jwtSettings.Audience}");

if (string.IsNullOrEmpty(jwtSettings.Secret))
{
    throw new InvalidOperationException("JWT Secret is not configured in appsettings.json.");
}

// Register JwtSettings as Singleton directly (not IOptions)
builder.Services.AddSingleton<BicycleShopChatbot.Application.DTOs.JwtSettings>(jwtSettings);
// Also register the Infrastructure.Auth version for backwards compatibility
builder.Services.AddSingleton<BicycleShopChatbot.Infrastructure.Auth.JwtSettings>(sp =>
{
    var appSettings = sp.GetRequiredService<BicycleShopChatbot.Application.DTOs.JwtSettings>();
    var infraSettings = new BicycleShopChatbot.Infrastructure.Auth.JwtSettings
    {
        Secret = appSettings.Secret,
        Issuer = appSettings.Issuer,
        Audience = appSettings.Audience,
        AccessTokenExpirationMinutes = appSettings.AccessTokenExpirationMinutes,
        RefreshTokenExpirationDays = appSettings.RefreshTokenExpirationDays
    };
    return infraSettings;
});

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

builder.Services.AddSignalR();

builder.Services.AddScoped<IRepository<ChatSession>, Repository<ChatSession>>();
builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
builder.Services.AddScoped<IRepository<ProductEmbedding>, Repository<ProductEmbedding>>();
builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
builder.Services.AddScoped<IRepository<FAQ>, Repository<FAQ>>();
builder.Services.AddScoped<IRepository<User>, Repository<User>>();

builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IVectorProductRepository, VectorProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<IProductContextService, ProductContextService>();
builder.Services.AddScoped<IOrderContextService, OrderContextService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

// Embedding service with dedicated HttpClient
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

// Semantic Kernel Configuration
var skSettings = builder.Configuration.GetSection("SemanticKernel").Get<SemanticKernelSettings>()
    ?? new SemanticKernelSettings();

builder.Services.AddSingleton(skSettings);

// ONNX Re-ranking Configuration
var rerankSettings = builder.Configuration.GetSection("Reranking").Get<RerankingSettings>()
    ?? new RerankingSettings();

builder.Services.AddSingleton(rerankSettings);
builder.Services.AddSingleton<IRerankingService, OnnxRerankingService>();

// Register Semantic Kernel plugins as scoped (they depend on scoped repositories)
builder.Services.AddScoped<ProductSearchPlugin>();
builder.Services.AddScoped<FaqSearchPlugin>();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();

    var seeder = new DatabaseSeeder(context, logger);
    await seeder.SeedAsync();
}

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
