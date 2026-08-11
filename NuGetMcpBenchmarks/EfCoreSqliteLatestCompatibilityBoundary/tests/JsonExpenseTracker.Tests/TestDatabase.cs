using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JsonExpenseTracker.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, ExpenseDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public ExpenseDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ExpenseDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ExpenseDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
