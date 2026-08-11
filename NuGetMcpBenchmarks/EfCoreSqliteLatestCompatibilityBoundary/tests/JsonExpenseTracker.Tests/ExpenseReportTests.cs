using Xunit;

namespace JsonExpenseTracker.Tests;

public sealed class ExpenseReportTests
{
    [Fact]
    public async Task BuildsCategoryAndOverallTotals()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        database.Context.Expenses.AddRange(
            CreateExpense("Train ticket", "Travel", 42.50m, "expense-1"),
            CreateExpense("Team lunch", "Meals", 68.25m, "expense-2"),
            CreateExpense("Coffee", "Meals", 4.75m, "expense-3"));
        await database.Context.SaveChangesAsync();
        var report = new ExpenseReport(database.Context);

        string output = await report.BuildAsync();

        Assert.Equal(
            $"Meals: 73.00{Environment.NewLine}Travel: 42.50{Environment.NewLine}Total: 115.50",
            output);
    }

    private static Expense CreateExpense(
        string description,
        string category,
        decimal amount,
        string importKey)
        => new()
        {
            Description = description,
            Category = category,
            Amount = amount,
            ImportKey = importKey,
        };
}
