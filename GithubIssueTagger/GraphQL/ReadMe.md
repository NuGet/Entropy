# Working with GitHub GraphQL

[GitHub has documentation on GraphQL](https://docs.github.com/en/graphql).
I found the page titled [Form calls with GraphQL](https://docs.github.com/en/graphql/guides/forming-calls-with-graphql) the most helpful with getting started, more so than the [Introduction to GraphQL](https://docs.github.com/en/graphql/guides/introduction-to-graphql) page.

I'm probably being overly cautious, but [all GitHub APIs have resource limits](https://docs.github.com/en/rest/rate-limit).
The [GraphQL resource limit](https://docs.github.com/en/graphql/overview/resource-limitations) is based on the maximum number of nodes a query might return.
Individual queries have a maximum number of nodes they can query, but even within the limit, the cost (a "point" score) of a query depends on the maximum nodes the query can return.
Therefore, we need to trade off between a query requesting the maximum it can, but being high cost, versus a query that returns all information for most, but not all, items, and then having a second query which only gets the additional information for the selected node. For example, a query that lists 100 issues, and selects up to 100 labels vs selecting only 5 labels per issue (cost of 1) and the few issues that have more than 5 labels, running a separate query (cost of 1 per issue).

Queries for a single node can be achieved by returning the `id` for each node in the first query, and then using the `node` query with relevant "typecasting" syntax:

```graphql
query($issue: ID!) {
  node(id: $issue) {
    ... on Issue {
      number,
      title,
      labels(first: 100) {
        nodes {
          name
        }
      }
    }
  }
}
```

## Test with GitHub GraphQL Explorer

One option to test queries is using [GitHub's GraphQL Explorer](https://docs.github.com/en/graphql/overview/explorer).
First, you need to log in, and there is a button on the right, just above the query editor and results view.
Once logged in, you can copy-paste a query into the query editor, and add variables below.

For example, you can try this query:

```graphql
query($owner: String!, $repo: String!) {
    repository(owner: $owner, name: $repo) {
        issues(last: 2) {
            nodes {
                number,
                title,
                id
            }
        }
    }
}
```

In the variables editor, you need to define values for `owner` and `repo`. For example:

```json
{
    "owner": "nuget",
    "repo": "home"
}
```

## Test with GitHub CLI

[GitHub has a CLI tool](https://cli.github.com/) that can do a lot, including querying the REST and GraphQL APIs.
The [docs for the `api` command](https://cli.github.com/manual/gh_api) have examples for GraphQL, but be aware that the syntax for multi-line commands might be different for different shells (bash vs powershell for example).
Instead, I recommend saving the query in a file and then using the `--input <file>` syntax.
This makes it much, much easier to edit.
Also, when you work with multiple queries, it's much easier to manage.
Variables are passed with the `-F key=value` argument.
For example, `gh api graphql --input GetIssues.graphql -F owner=nuget -F repo=home`.