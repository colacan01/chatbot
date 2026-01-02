using BicycleShopChatbot.Api.Hubs;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Services;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Infrastructure.Data;
using BicycleShopChatbot.Infrastructure.Repositories.Implementation;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using BicycleShopChatbot.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString ?? "Data Source=bicycleshop.db");
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

builder.Services.AddSignalR();

builder.Services.AddScoped<IRepository<ChatSession>, Repository<ChatSession>>();
builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
builder.Services.AddScoped<IRepository<Product>, Repository<Product>>();
builder.Services.AddScoped<IRepository<Order>, Repository<Order>>();
builder.Services.AddScoped<IRepository<FAQ>, Repository<FAQ>>();

builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddScoped<IProductContextService, ProductContextService>();
builder.Services.AddScoped<IOrderContextService, OrderContextService>();

builder.Services.AddHttpClient<IOllamaService, OllamaService>();

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

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
