using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace JsonExpenseTracker;

public sealed class ExpenseImporter(ExpenseDbContext dbContext)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<int> ImportAsync(
        Stream json,
        CancellationToken cancellationToken = default)
    {
        List<ExpenseInput>? inputs = await JsonSerializer.DeserializeAsync<List<ExpenseInput>>(
            json,
            SerializerOptions,
            cancellationToken);

        if (inputs is null)
        {
            throw new InvalidDataException("The expense file must contain a JSON array.");
        }

        List<Expense> candidates = inputs.Select(CreateExpense).ToList();
        string[] keys = candidates.Select(expense => expense.ImportKey).ToArray();
        List<string> storedKeys = await dbContext.Expenses
            .Where(expense => keys.Contains(expense.ImportKey))
            .Select(expense => expense.ImportKey)
            .ToListAsync(cancellationToken);
        HashSet<string> existingKeys = storedKeys.ToHashSet(StringComparer.Ordinal);

        List<Expense> additions = candidates
            .Where(expense => existingKeys.Add(expense.ImportKey))
            .ToList();

        dbContext.Expenses.AddRange(additions);
        await dbContext.SaveChangesAsync(cancellationToken);
        return additions.Count;
    }

    private static Expense CreateExpense(ExpenseInput input)
    {
        string description = RequireValue(input.Description, "description");
        string category = RequireValue(input.Category, "category");

        if (input.Amount < 0)
        {
            throw new InvalidDataException("Expense amounts cannot be negative.");
        }

        string identity = string.Join(
            '\u001f',
            description.ToUpperInvariant(),
            category.ToUpperInvariant(),
            input.Amount.ToString("G29", CultureInfo.InvariantCulture));
        string importKey = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

        return new Expense
        {
            Description = description,
            Category = category,
            Amount = input.Amount,
            ImportKey = importKey,
        };
    }

    private static string RequireValue(string? value, string propertyName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new InvalidDataException(
                $"Expense property '{propertyName}' cannot be empty.");
        }

        return normalized;
    }

    private sealed record ExpenseInput(string? Description, string? Category, decimal Amount);
}
