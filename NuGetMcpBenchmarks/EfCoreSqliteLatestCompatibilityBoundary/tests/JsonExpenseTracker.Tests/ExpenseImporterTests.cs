using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JsonExpenseTracker.Tests;

public sealed class ExpenseImporterTests
{
    [Fact]
    public async Task ImportsExpensesFromJson()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        var importer = new ExpenseImporter(database.Context);
        await using FileStream fixture = OpenFixture();

        int imported = await importer.ImportAsync(fixture);

        Expense[] expenses = await database.Context.Expenses
            .OrderBy(expense => expense.Description)
            .ToArrayAsync();
        Assert.Equal(2, imported);
        Assert.Collection(
            expenses,
            expense =>
            {
                Assert.Equal("Team lunch", expense.Description);
                Assert.Equal("Meals", expense.Category);
                Assert.Equal(68.25m, expense.Amount);
            },
            expense =>
            {
                Assert.Equal("Train ticket", expense.Description);
                Assert.Equal("Travel", expense.Category);
                Assert.Equal(42.50m, expense.Amount);
            });
    }

    [Fact]
    public async Task ReimportingFixtureDoesNotDuplicateExpenses()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        var importer = new ExpenseImporter(database.Context);
        await using (FileStream firstImport = OpenFixture())
        {
            Assert.Equal(2, await importer.ImportAsync(firstImport));
        }

        await using (FileStream secondImport = OpenFixture())
        {
            Assert.Equal(0, await importer.ImportAsync(secondImport));
        }

        Assert.Equal(2, await database.Context.Expenses.CountAsync());
    }

    private static FileStream OpenFixture()
        => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "fixtures", "expenses.json"));
}
