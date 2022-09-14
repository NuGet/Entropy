# NuGet.GitHubEventHandler

This is a GitHub webhook handler used by the NuGet team.

## Architecture

It's best practise for webhook recepients to return a response to the caller as quickly as possible, so that the caller (GitHub) doesn't have to maintain resources (HTTP connection) for the duration of processing the hook.
Additionally, when there are errors/exceptions unrelated to the validation of the webhook, GitHub shouldn't receive an error and therefore be required to resend the contents.
Errors that need retrying should be handled by requeing the contents saved in the app's own storage.
Finally, we want this app to be configurable, so different repos can use it with just a configuration change.
Therefore, the design of this app is to use the following triggers/data flow:

1. HTTP trigger (`hooks/{name}`)
   * Validate input (HMAC), if valid then save body to storage.
2. Blob trigger
   * Parse the webhook contents, check which queues are "subscribed" from configuration (table storage), and queue all the relevant messages.
3. Queue trigger
   * This is where the real "business logic" is. Each feature will have a separate queue, and when a message is received, the webhook body is read (again), and processed.
4. Timer trigger
   * Delete blobs after a few days, so storage doesn't grow forever.

## Configuration

When configuring locally with an Azure Storage emulator, or deploying to a new Azure resource, the following needs to be set up.
On the [Azure Portal](https://portal.azure.com/), you can use "Storage Browser" when viewing your Azure Functions' Storage account.
[Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/) can be used with Azure Storage emulators and also Azure Storage accounts.

### 1. Environment variables

When deployed on Azure, [Key Vault can be used to secure secrets](https://docs.microsoft.com/azure/app-service/app-service-key-vault-references).
When running locally, environment variables can be set in `launchSettings.json` (gitignored), or your machine settings.

#### `WEBHOOK_SECRET_{name}`

When setting up the webhook on github, the endpoint to use is of the format `https://{host}/api/hooks/{name}`, where name can be any value you wish.
And example is to use the GitHub repo name for repo webhooks, or org name for org webhooks.
The name used in the URL must match the environment variable suffix in `WEBHOOK_SECRET_{name}`.
For example, if I use `https://{host}/api/hooks/nuget`, then my environment variable name must be `WEHBOOK_SECRET_nuget`.

It is ideal for the UTF8 byte encoding of the string to be 64 bytes (64 ASCII characters).

#### `AZDO_TOKEN_{org}`

Azure DevOps organizations use the URL `https://dev.azure.com/{org}`.
Create a personal access token at `https://dev.azure.com/{org}/_usersSettings/tokens`, and save this in the variable `AZDO_TOKEN_{org}`.

The Personal Access Token needs the following scopes:
*  Build (Read & Execute)

### 2. Blob container `webhooks`

The webhooks HTTP trigger will save blobs in this container, so the container must exist.

### 3. Table `BuildPullRequestOnAzDO`

You can either create this table manually, or you can have GitHub send a PullRequest notification of action 'labeled', and the Azure Functions will create the table for you.

Once the table is created, you will need to insert a row manually.
The first row will not have the function's expected columns pre-created.
Look at `BuildPullRequestOnAzDO.cs`'s `SubscriptionTableEntry` to see the expected columns.
The column names and XML doc are intended to be sufficient to understand what value to set.

### 4. Queues

It should not be necessary to create the queues, and Azure Functions will create them on first use.

## Debugging

Client app developers may not be familiar with web application or Azure Functions debugging.
The following are non-exhaustive tips to get started.

For debugging, you should always use your own GitHub repo.
GitHub private repos have been free for many years, including private repos if you wish to test in secret.
Similarly, Azure DevOps has been free for organizations with a small number of contributors, so you can [create your own Azure DevOps organization](https://docs.microsoft.com/azure/devops/organizations/accounts/create-organization?view=azure-devops) to test with. You will need to create a build in Azure DevOps to test the `BuildPullRequestOnAzDO` function.

Note that you can not test the functions independently, unless you comment out the `FunctionName` attributes. For example, testing the HTTP trigger will test the full end-to-end workflow, HTTP trigger, blob trigger, then any queues that match.

### Debugging the HTTP trigger

Use an [HTTP tunneling service](https://github.com/anderspitman/awesome-tunneling), then add a webhook on your GitHub repo that points to the tunnel's public endpoint.

### Blob trigger

Use Azure Storage Explorer to upload a file to the `webhooks` container, in the `incoming/` virtual directory.
It does not need to match the `webhooks/incoming/yyyy-MM-dd/*.json` format.
In fact, to avoid the `DeleteOldBlobs` trigger deleting the files, you may wish to use a different path like `webhooks/test/*.json`.

###  Queues

Use Azure Storage Explorer to add a message to the queue, where the message contents is the URL of the file in blob storage, starting from the container name (so, without the URL schema or hostname, or the storage emulator's account prefix).
For example, if a file's full URL is `http://127.0.0.1:10000/devstoreaccount1/webhooks/test/pr1-approved.json`, then the message contents must be `webhooks/test/pr1-approved.json`.

### Timer trigger

Timer trigger have a `RunOnStartup` attribute, which must not be set in the Release build, so is probably set inside an `#if DEBUG` block.
Therefore, the trigger likely always starts every time the debug build is started.
