using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JsonExpenseTracker.Tests;

public sealed class ExpensePersistenceTests
{
    [Fact]
    public async Task ImportedExpensesPersistAcrossContexts()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"expenses-{Guid.NewGuid():N}.db");

        try
        {
            DbContextOptions<ExpenseDbContext> options = CreateOptions(databasePath);
            await using (var importContext = new ExpenseDbContext(options))
            {
                await importContext.Database.EnsureCreatedAsync();
                var importer = new ExpenseImporter(importContext);
                await using FileStream fixture = File.OpenRead(
                    Path.Combine(AppContext.BaseDirectory, "fixtures", "expenses.json"));
                await importer.ImportAsync(fixture);
            }

            await using var queryContext = new ExpenseDbContext(options);
            Expense[] stored = await queryContext.Expenses
                .OrderBy(expense => expense.Category)
                .ToArrayAsync();

            Assert.Equal(2, stored.Length);
            Assert.Equal(["Meals", "Travel"], stored.Select(expense => expense.Category));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static DbContextOptions<ExpenseDbContext> CreateOptions(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();
        return new DbContextOptionsBuilder<ExpenseDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }
}
