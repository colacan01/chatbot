using BicycleShopChatbot.Application.DTOs;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IResponseValidationService
{
    Task<ResponseValidationResult> ValidateAndCleanResponseAsync(
        string response,
        CancellationToken cancellationToken = default);

    IEnumerable<string> ExtractProductCodes(string response);

    Task<Dictionary<string, bool>> ValidateProductCodesAsync(
        IEnumerable<string> productCodes,
        CancellationToken cancellationToken = default);
}
