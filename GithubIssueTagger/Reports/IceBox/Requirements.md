# IceBox Report — Requirements

## Purpose

The IceBox report identifies GitHub issues that have been placed in an "ice box" (deprioritized) but have since received significant community interest in the form of positive reactions (upvotes).

## Inputs

The report accepts the following command line arguments:
- The path to a JSON configuration file (required).
- `--pat` (optional): a GitHub personal access token for authentication (see section on Authentication).
- `--apply` (optional): when specified, the report modifies issues that meet the upvote threshold (see section 5).
  Without it, the report runs in dry-run mode — it outputs which issues qualify but does not modify them.
- `--verbose` (optional): when specified, the report additionally outputs the date the `searchLabel` was applied and the number of upvotes the issue has received since that date.

### Configuration File Schema

The JSON file contains the following properties:

| Property | Description |
|----------|-------------|
| `owner` | GitHub owner (org or user) of the repository. |
| `repo` | Repository to search for issues in. |
| `searchLabel` | The label used to filter issues (i.e., the ice box label). |
| `triage` | Object containing triage settings. |
| `triage.upvotes` | Number of unique positive reactions required to meet the threshold. |
| `triage.label` | Label to add to issues that meet the upvote threshold. |

All properties are required.
If any property is missing or undefined, the report must print an error message identifying the missing property and exit without processing any issues.

### Example Configuration File

```json
{
  "owner": "NuGet",
  "repo": "Home",
  "searchLabel": "Priority:3",
  "triage": {
    "upvotes": 5,
    "label": "Triage:NeedsTriageDiscussion"
  }
}
```

## Authentication

The report requires a GitHub access token to authenticate with the GraphQL API.
The token is resolved using the following strategy, in order:

1. If `--pat` is provided on the command line, that value is used.
2. Otherwise, the report attempts to obtain a token from the **GitHub CLI** (`gh auth token`).
3. If the GitHub CLI is not available or does not return a token, the report attempts to obtain a token from **Git Credential Manager**.
4. If no token can be obtained, the report prints an error message and exits without processing any issues.

## Behaviour

### 1. Issue Retrieval and Data Fetching

The report queries GitHub's GraphQL API to retrieve all **open issues** in the specified repository that have the `searchLabel`.
Issues are fetched in pages of 100 using cursor-based pagination.

GitHub's GraphQL API enforces a data size limit per query, so the amount of nested data fetched per issue must be kept small.
For each issue, the initial query fetches only:
- The **last 5 timeline events** of type `LabeledEvent` and `UnlabeledEvent` (used to determine the cutoff date — see section 3).
- The most recent **reactions** (fetched at a count of `triage.upvotes × 2`).
- The first **100 labels** on the issue (used to check whether the `triage.label` has already been applied).

If the pre-fetched timeline events are insufficient to determine the cutoff date, a separate GraphQL query retrieves the **last 100 labeled/unlabeled event entries** for that specific issue.
If the required events still cannot be found, a **GitHub Actions warning** is emitted and the issue is skipped.

GitHub's GraphQL API also enforces rate limits.
When a response includes a `Retry-After` header, the report must wait for the specified duration before making the next request.

### 2. Skip If Triage Label Already Applied

Each issue is first checked to see whether it already has the `triage.label`.
If it does, the issue is **skipped** — no further processing or output is produced for it.

### 3. Determine the Cutoff Date

The report determines a **cutoff date** for each issue.
Only reactions after this date are counted toward the upvote threshold.
The cutoff date is the **most recent** of the following two events:

- The most recent date the `searchLabel` was **added** to the issue.
- The most recent date the `triage.label` was **removed** from the issue (if it was ever removed).

If the `triage.label` was never removed, the cutoff date is simply when the `searchLabel` was last added.

### 4. Count Positive Reactions Since the Cutoff Date

Only reactions that occurred **after** the cutoff date are considered.
A reaction is considered "positive" if its content type is one of:

- `THUMBS_UP` (👍)
- `HEART` (❤️)
- `ROCKET` (🚀)

Reactions are **deduplicated by user** — multiple positive reactions from the same user count as a single upvote.
The total number of distinct users who added positive reactions after the cutoff date is compared against the configured upvote threshold.

If the issue has more reactions than were fetched (pagination applies), the report attempts a best-effort determination:
- If the already-fetched reactions exceed the threshold, the issue qualifies.
- If the oldest fetched reaction is older than the cutoff date, the issue does not qualify (fetching more would not change the result).
- Otherwise, a GitHub Actions warning is emitted indicating more reactions need to be fetched, and the issue is treated as **not qualifying**.

### 5. Output and Label Application

For each issue that meets the upvote threshold, a message is printed: `Issue {url} has enough upvotes`.
If `--verbose` is specified, a message is printed for **every** issue, including the cutoff date, the reason for the cutoff (whether the search label was added or the triage label was removed), and the number of upvotes the issue received since that date.
If `--apply` is specified, the `triage.label` is **added to the issue** via a GraphQL mutation.
The label's node ID is resolved once (by name) and cached for subsequent issues.

### 6. Error and Warning Reporting

The report uses GitHub Actions annotation syntax for warnings and errors:
- `::warning ::` for non-fatal conditions (e.g., unsupported pagination scenarios).
- `::error ::` for GraphQL response errors.

This makes the report suitable for running as a **GitHub Actions workflow step**, where these annotations surface in the Actions UI.

## Known Limitations

The following scenarios are not fully handled and produce warnings:

1. **Issue has more than 100 labels** — The report cannot confirm whether the action label is already applied.
2. **Ice box label not found in the last 100 timeline events** — The report cannot determine when the label was applied and skips the issue.
3. **Reactions require additional pagination** — When the fetched reactions are insufficient to determine the threshold and a definitive answer cannot be derived from the available data, the issue is treated as not qualifying.

## Interactive Mode

The IceBox report is **not available in interactive mode**.
Running it without CLI arguments prints an informational message and exits.
