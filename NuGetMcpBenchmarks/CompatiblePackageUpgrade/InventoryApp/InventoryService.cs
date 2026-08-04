using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryApp;

public sealed class InventoryService(
    InventoryDbContext context,
    ILogger<InventoryService> logger)
{
    public async Task AddAsync(InventoryItem item)
    {
        context.Inventory.Add(item);
        await context.SaveChangesAsync();
        logger.LogInformation("Stored inventory item {Sku}", item.Sku);
    }

    public Task<List<InventoryItem>> GetLowStockAsync(int maximumQuantity)
    {
        return context.Inventory
            .Where(item => item.Quantity <= maximumQuantity)
            .OrderBy(item => item.Quantity)
            .ThenBy(item => item.Sku)
            .ToListAsync();
    }
}
