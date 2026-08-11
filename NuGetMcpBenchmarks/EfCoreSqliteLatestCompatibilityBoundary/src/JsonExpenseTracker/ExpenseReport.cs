using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace JsonExpenseTracker;

public sealed class ExpenseReport(ExpenseDbContext dbContext)
{
    public async Task<string> BuildAsync(CancellationToken cancellationToken = default)
    {
        var expenses = await dbContext.Expenses
            .AsNoTracking()
            .Select(expense => new { expense.Category, expense.Amount })
            .ToListAsync(cancellationToken);

        var categoryTotals = expenses
            .GroupBy(expense => expense.Category, StringComparer.Ordinal)
            .Select(group => new
            {
                Category = group.Key,
                Total = group.Sum(expense => expense.Amount),
            })
            .OrderBy(item => item.Category, StringComparer.Ordinal);

        var report = new StringBuilder();
        foreach (var category in categoryTotals)
        {
            report.Append(category.Category)
                .Append(": ")
                .AppendLine(category.Total.ToString("0.00", CultureInfo.InvariantCulture));
        }

        report.Append("Total: ")
            .Append(expenses.Sum(expense => expense.Amount)
                .ToString("0.00", CultureInfo.InvariantCulture));
        return report.ToString();
    }
}
