using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InventoryApp.Tests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task StoresAndRetrievesInventoryItem()
    {
        await using var fixture = await InventoryFixture.CreateAsync();

        await fixture.Service.AddAsync(new InventoryItem
        {
            Sku = "WIDGET-1",
            Name = "Widget",
            Quantity = 12,
        });

        var stored = await fixture.Context.Inventory.SingleAsync();

        Assert.Equal("WIDGET-1", stored.Sku);
        Assert.Equal("Widget", stored.Name);
        Assert.Equal(12, stored.Quantity);
    }

    [Fact]
    public async Task QueriesLowStockItemsInDeterministicOrder()
    {
        await using var fixture = await InventoryFixture.CreateAsync();
        fixture.Context.Inventory.AddRange(
            new InventoryItem { Sku = "B", Name = "Bolts", Quantity = 3 },
            new InventoryItem { Sku = "A", Name = "Adapters", Quantity = 3 },
            new InventoryItem { Sku = "C", Name = "Cables", Quantity = 15 });
        await fixture.Context.SaveChangesAsync();

        var lowStock = await fixture.Service.GetLowStockAsync(5);

        Assert.Equal(["A", "B"], lowStock.Select(item => item.Sku));
    }

    private sealed class InventoryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ILoggerFactory _loggerFactory;

        private InventoryFixture(
            SqliteConnection connection,
            InventoryDbContext context,
            ILoggerFactory loggerFactory)
        {
            _connection = connection;
            Context = context;
            _loggerFactory = loggerFactory;
            Service = new InventoryService(
                context,
                loggerFactory.CreateLogger<InventoryService>());
        }

        public InventoryDbContext Context { get; }

        public InventoryService Service { get; }

        public static async Task<InventoryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new InventoryDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return new InventoryFixture(connection, context, loggerFactory);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            _loggerFactory.Dispose();
            await _connection.DisposeAsync();
        }
    }
}
