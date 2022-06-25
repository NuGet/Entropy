# ReservedNamespaceAnalyzer

This little tool takes data fetched from the NuGet.org gallery (as CSV) and correlates it in memory.

Right now, this tool just produces a `prefix-matches.csv` which contains all reserved namespaces which are prefixes (or exact matches, as specified) of any other package ID.

This data can be loaded into Kusto for exploration.

This tool requires .NET 6.

## Instructions

1. Run the SQL queries below to generate input CSV files  (see [SQL queries](#sql-queries)).
2. Put those CSV files in a single directory.
3. Run this program with a single command line argument which is the path to that directory.
4. Create the Kusto tables to hold the data (see [Kusto queries](#kusto-queries))
4. Import the produced CSVs (2 from SQL, 1 from this tool) into Azure Data Explorer (Kusto)
5. Play with the data.
6. Clean up after yourself by deleting the Kusto tables.

There's some sample queries at the bottom.

## SQL queries

### Reserved namespaces info (save as `reserved-namespaces.csv`)

```sql
SELECT
    ISNULL(u.Username, '') AS OwnerUsername,
    rn.Value,
    rn.IsSharedNamespace,
    rn.IsPrefix
FROM ReservedNamespaces rn
LEFT OUTER JOIN ReservedNamespaceOwners rno ON rn.[Key] = rno.ReservedNamespaceKey
LEFT OUTER JOIN Users u ON rno.UserKey = u.[Key]
ORDER BY rn.Value, u.Username, rn.IsSharedNamespace, rn.IsPrefix
```

### Package registrations info (save as `package-registrations.csv`)

```sql
SELECT
    ISNULL(u.Username, '') AS OwnerUsername,
    pr.Id,
    pr.IsLocked,
    pr.IsVerified,
    pr.DownloadCount AS TotalDownloadCount,
    ISNULL(a.VersionCount, 0) AS PackageCount,
    ISNULL(a.ListedCount, 0) AS ListedCount,
    ISNULL(a.LastUpdated, '') AS LastUpdated
FROM PackageRegistrations pr
LEFT OUTER JOIN PackageRegistrationOwners pro ON pro.PackageRegistrationKey = pr.[Key]
LEFT OUTER JOIN Users u ON pro.UserKey = u.[Key]
LEFT OUTER JOIN (
    SELECT p.PackageRegistrationKey, COUNT(*) AS VersionCount, SUM(IIF(p.PackageStatusKey = 1 AND p.Listed = 1, 1, 0)) AS ListedCount, MAX(p.Created) LastUpdated
    FROM Packages p
    INNER JOIN PackageRegistrations pr ON p.PackageRegistrationKey = pr.[Key]
    GROUP BY p.PackageRegistrationKey
) a ON pr.[Key] = a.PackageRegistrationKey
ORDER BY pr.Id, u.Username
```

## Kusto queries

Run these queries so the tables are created.

```kql
// import reserved-namespaces.csv into here
.create table JverReservedNamespaces (
    OwnerUsername : string,
    Value : string,
    IsSharedPrefix : bool,
    IsPrefix : bool
);

// import package-registrations.csv into here
.create table JverPackageRegistrations (
    OwnerUsername : string,
    Id : string,
    IsLocked : bool,
    IsVerified : bool,
    TotalDownloadCount : long,
    PackageCount : int,
    ListedCount : int,
    LastUpdated : datetime
);


// import prefix-matches.csv into here
.create table JverReservedNamespaceMatches (
    Value : string,
    IsPrefix : bool,
    Id : string
);
```

## Sample queries

### Find top unverified packages in top namespaces

```kql
let JverReservedNamespaceLongestMatches = JverReservedNamespaceMatches
| summarize (Length, Value) = arg_max(strlen(Value), Value), IsPrefix = take_any(IsPrefix) by Id
| project Value, IsPrefix, Id;
JverReservedNamespaces
| summarize ReservedNamespaceOwners = array_sort_asc(make_set_if(OwnerUsername, isnotempty(OwnerUsername))) by Value, IsSharedPrefix, IsPrefix
| join kind=inner JverReservedNamespaceLongestMatches on Value
| project-away Value1, IsPrefix1
| join kind=inner (
    JverPackageRegistrations
    | summarize PackageRegistrationOwners = array_sort_asc(make_set_if(OwnerUsername, isnotempty(OwnerUsername))) by Id, IsVerified, TotalDownloadCount
) on Id
| summarize
    VerifiedTotalDownloadCount = sumif(TotalDownloadCount, IsVerified),
    UnverifiedTotalDownloadCount = sumif(TotalDownloadCount, IsVerified == false),
    TopUnverifiedIds = array_slice(array_sort_desc(
        make_list_if(TotalDownloadCount, IsVerified == false),
        make_list_if(pack("Id", Id, "TotalDownloadCount", TotalDownloadCount, "PackageRegistrationOwners", PackageRegistrationOwners), IsVerified == false)
    )[1], 0, 4)
    by Value, IsSharedPrefix, tostring(ReservedNamespaceOwners)
| where array_length(TopUnverifiedIds) > 0
| where VerifiedTotalDownloadCount > 0
| order by UnverifiedTotalDownloadCount desc
| take 5
| mv-expand UnverifiedId = TopUnverifiedIds
| extend UnverifiedIdTotalDownloadCount = tolong(UnverifiedId.TotalDownloadCount)
| extend UnverifiedIdOwners = UnverifiedId.PackageRegistrationOwners
| extend UnverifiedId = tostring(UnverifiedId.Id)
| project-away TopUnverifiedIds, VerifiedTotalDownloadCount, UnverifiedTotalDownloadCount
```