using Microsoft.EntityFrameworkCore;

namespace InventoryApp;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options)
{
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
}
