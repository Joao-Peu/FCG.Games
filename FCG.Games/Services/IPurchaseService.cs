using FCG.Games.Models;

namespace FCG.Games.Services;

public interface IPurchaseService
{
    Task<PurchaseOrder> CreatePurchaseIntentAsync(Guid userId, Guid gameId, string? correlationId);
}
