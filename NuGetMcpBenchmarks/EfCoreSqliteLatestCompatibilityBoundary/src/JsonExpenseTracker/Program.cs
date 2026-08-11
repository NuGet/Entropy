using JsonExpenseTracker;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: JsonExpenseTracker <expenses.json> [expenses.db]");
    return 1;
}

string fixturePath = Path.GetFullPath(args[0]);
string databasePath = Path.GetFullPath(
    args.Length == 2 ? args[1] : "expenses.db");
var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
}.ToString();
var options = new DbContextOptionsBuilder<ExpenseDbContext>()
    .UseSqlite(connectionString)
    .Options;

await using var dbContext = new ExpenseDbContext(options);
await dbContext.Database.EnsureCreatedAsync();
await using (FileStream fixture = File.OpenRead(fixturePath))
{
    var importer = new ExpenseImporter(dbContext);
    await importer.ImportAsync(fixture);
}

var report = new ExpenseReport(dbContext);
Console.WriteLine(await report.BuildAsync());
return 0;
