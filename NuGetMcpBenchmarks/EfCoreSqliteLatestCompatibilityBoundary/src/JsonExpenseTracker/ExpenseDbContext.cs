using Microsoft.EntityFrameworkCore;

namespace JsonExpenseTracker;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options)
    : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expense>()
            .HasIndex(expense => expense.ImportKey)
            .IsUnique();
    }
}
