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

## Configuration

When configurating locally with an Azure Storage emulator, or deploying to a new Azure resource, the following needs to be set up:

1. Environment variable `WEBHOOK_SECRET_{name}`

The webhook endpoint route is of the format `https://{host}/api/hooks/{name}`, to allow this app to allow different webhook registrations to use different secrets.
When setting up the webhook on GitHub, whatever you use for `{name}` is the suffix for the environment variable.
For example, if I use `https://{host}/api/hooks/nuget`, then my environment variable name must be `WEHBOOK_SECRET_nuget`.

It is ideal for the UTF8 byte encoding of the string to be 64 bytes (64 ASCII characters).

When deployed on Azure, [Key Vault can be used to secure secrets](https://docs.microsoft.com/azure/app-service/app-service-key-vault-references).

2. Blob container `webhooks`

The Webhoobs HTTP trigger will save blobs in this container, so the container must exist.
