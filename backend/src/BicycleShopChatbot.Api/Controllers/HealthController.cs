using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BicycleShopChatbot.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext context,
        IOllamaService ollamaService,
        ILogger<HealthController> logger)
    {
        _context = context;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = await CheckDatabaseAsync(),
            ollama = await CheckOllamaAsync()
        };

        return Ok(health);
    }

    private async Task<object> CheckDatabaseAsync()
    {
        try
        {
            await _context.Database.CanConnectAsync();
            return new { status = "connected", provider = _context.Database.ProviderName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new { status = "error", message = ex.Message };
        }
    }

    private async Task<object> CheckOllamaAsync()
    {
        try
        {
            var isAvailable = await _ollamaService.IsModelAvailableAsync();
            return new { status = isAvailable ? "available" : "unavailable" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama health check failed");
            return new { status = "error", message = ex.Message };
        }
    }
}
