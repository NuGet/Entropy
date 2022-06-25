
using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using VDS.Common.Tries;

var dataRoot = args.ElementAtOrDefault(0) ?? @"C:\Users\jver\Desktop\package-registrations";
var fastRun = false;

var users = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

Console.WriteLine("Loading reserved namespaces...");
var reservedNamespacesPath = Path.Combine(dataRoot, "reserved-namespaces.csv");
var reservedNamespaces = new Dictionary<string, ReservedNamespace>(StringComparer.OrdinalIgnoreCase);
using (var textReader = File.OpenText(reservedNamespacesPath))
using (var csvReader = new CsvReader(textReader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
{
    foreach (var record in csvReader.GetRecords<ReservedNamespaceRecord>())
    {
        if (fastRun && !record.Value.StartsWith("micro", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!reservedNamespaces.TryGetValue(record.Value, out var existing))
        {
            existing = new ReservedNamespace(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                record.Value,
                record.IsSharedNamespace,
                record.IsPrefix,
                new List<PackageRegistration>());
            reservedNamespaces.Add(record.Value, existing);
        }

        if (record.IsSharedNamespace != existing.IsSharedNamespace)
        {
            throw new InvalidDataException($"The '{record.Value}' reserved namespace has multiple values for {nameof(record.IsSharedNamespace)}.");
        }

        if (record.IsPrefix != existing.IsPrefix)
        {
            throw new InvalidDataException($"The '{record.Value}' reserved namespace has multiple values for {nameof(record.IsPrefix)}.");
        }

        if (!string.IsNullOrEmpty(record.OwnerUsername))
        {
            existing.OwnerUsernames.Add(users.GetOrAdd(record.OwnerUsername, record.OwnerUsername));
        }
    }
}

Console.WriteLine("Loading package registrations...");
var packageRegistrationsPath = Path.Combine(dataRoot, "package-registrations.csv");
var packageRegistrations = new Dictionary<string, PackageRegistration>(StringComparer.OrdinalIgnoreCase);
using (var textReader = File.OpenText(packageRegistrationsPath))
using (var csvReader = new CsvReader(textReader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
{
    foreach (var record in csvReader.GetRecords<PackageRegistrationRecord>())
    {
        if (fastRun && !record.Id.StartsWith("micro", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!packageRegistrations.TryGetValue(record.Id, out var existing))
        {
            existing = new PackageRegistration(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                record.Id,
                record.IsLocked,
                record.IsVerified,
                record.TotalDownloadCount,
                record.PackageCount,
                record.ListedCount,
                record.LastUpdated);
            packageRegistrations.Add(record.Id, existing);
        }

        if (!string.IsNullOrEmpty(record.OwnerUsername))
        {
            existing.OwnerUsernames.Add(users.GetOrAdd(record.OwnerUsername, record.OwnerUsername));
        }
    }
}

Console.WriteLine("Building package ID trie...");
var packageRegistrationTrie = new Trie<string, char, PackageRegistration>(s => s.ToLowerInvariant().AsEnumerable());
var firstLetter = string.Empty;
foreach (var pr in packageRegistrations.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
{
    if (firstLetter != pr.Id[0].ToString())
    {
        firstLetter = pr.Id[0].ToString();
        Console.Write(firstLetter + " ");
    }

    packageRegistrationTrie.Add(pr.Id, pr);
}
Console.WriteLine();

Console.WriteLine("Matching reserved namespace to packages...");
var length = int.MaxValue;
// Process the reserved namespaces from longest to shortest, so packages match the longest prefix first
foreach (var rn in reservedNamespaces.Values.OrderByDescending(x => x.Value.Length).ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase))
{
    if (length != rn.Value.Length)
    {
        length = rn.Value.Length;
        Console.Write(length + " ");
    }

    var node = packageRegistrationTrie.Find(rn.Value);
    if (node != null)
    {
        foreach (var pr in node.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList())
        {
            if (rn.IsPrefix || rn.Value.Equals(pr.Id, StringComparison.OrdinalIgnoreCase))
            {
                rn.PackageRegistrations.Add(pr);
            }
        }
    }
}
Console.WriteLine();

Console.WriteLine("Writing reserved namespace matches...");
var matchesPath = Path.Combine(dataRoot, "prefix-matches.csv");
using (var stream = new FileStream(matchesPath, FileMode.Create))
using (var writer = new StreamWriter(stream))
using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false }))
{
    foreach (var rn in reservedNamespaces.Values.OrderBy(x => x.Value, StringComparer.OrdinalIgnoreCase))
    {
        foreach (var pr in rn.PackageRegistrations.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            csvWriter.WriteRecord(new ReservedNamespaceMatch(rn.Value, rn.IsPrefix, pr.Id));
            csvWriter.NextRecord();
        }
    }
}

Console.WriteLine("Done.");

record ReservedNamespaceRecord(
    string OwnerUsername,
    string Value,
    bool IsSharedNamespace,
    bool IsPrefix);

record ReservedNamespace(
    HashSet<string> OwnerUsernames,
    string Value,
    bool IsSharedNamespace,
    bool IsPrefix,
    List<PackageRegistration> PackageRegistrations);

record PackageRegistrationRecord(
    string OwnerUsername,
    string Id,
    bool IsLocked,
    bool IsVerified,
    long TotalDownloadCount,
    int PackageCount,
    int ListedCount,
    DateTimeOffset? LastUpdated);

record PackageRegistration(
    HashSet<string> OwnerUsernames,
    string Id,
    bool IsLocked,
    bool IsVerified,
    long TotalDownloadCount,
    int PackageCount,
    int ListedCount,
    DateTimeOffset? LastUpdated);

record ReservedNamespaceMatch(string Value, bool IsPrefix, string Id);
