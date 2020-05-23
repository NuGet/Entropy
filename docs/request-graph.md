# Request graph

This project utilizes a potentially crazy idea of rebuilding a dependency graph of HTTP requests from an application's
output log. In particular, it parses the log produced by a full NuGet restore operation a generates a graph describing
which HTTP requests depend on each other based on the order of the "start" and "end" logs. I was inspired by some
reading about [process mining](https://en.wikipedia.org/wiki/Process_mining) but my approach is much simpler than the
novel techniques applied in that field. Also, I couldn't figure out how to discretize events like an HTTP request
into a useful form for process mining. 

The purpose of this madness is to test the performance of a NuGet package source. In order to eliminate variables
in NuGet restore performance that are unrelated to server-side performance (such as client CPU, disk, memory),
the HTTP requests executed by restore are replayed in the same order that they occurred in restore, but without the
gaps and pauses between said HTTP requests caused by the computational or IO effort required for a NuGet restore.

This theoretically allows for more reproducable performance measurements for a NuGet package source. Also, test
run time should be shorter since we are performing less work in the "test loop".

## Example input

Consider a very simple C# project with two direct package dependencies:

```
ExampleProj.csproj
├─ NuGet.Configuration
│  └─ NuGet.Common 
│     └─ NuGet.Frameworks
└── NuGet.Packaging
    ├─ Newtonsoft.Json
    ├─ NuGet.Configuration
    │  └─ NuGet.Common 
    │     └─ NuGet.Frameworks
    └─ NuGet.Versioning
```

When this project is restored, a series of HTTP requests are rattled off, enumerating available package versions,
discovering dependencies, and downloading package content.

The request log contains something like this:

```
  GET https://api.nuget.org/v3-flatcontainer/nuget.configuration/index.json
  GET https://api.nuget.org/v3-flatcontainer/nuget.packaging/index.json
  OK https://api.nuget.org/v3-flatcontainer/nuget.packaging/index.json 206ms
  OK https://api.nuget.org/v3-flatcontainer/nuget.configuration/index.json 236ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.packaging/5.6.0/nuget.packaging.5.6.0.nupkg
  GET https://api.nuget.org/v3-flatcontainer/nuget.configuration/5.6.0/nuget.configuration.5.6.0.nupkg
  OK https://api.nuget.org/v3-flatcontainer/nuget.packaging/5.6.0/nuget.packaging.5.6.0.nupkg 36ms
  OK https://api.nuget.org/v3-flatcontainer/nuget.configuration/5.6.0/nuget.configuration.5.6.0.nupkg 41ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.common/index.json
  GET https://api.nuget.org/v3-flatcontainer/nuget.versioning/index.json
  OK https://api.nuget.org/v3-flatcontainer/nuget.common/index.json 224ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.common/5.6.0/nuget.common.5.6.0.nupkg
  OK https://api.nuget.org/v3-flatcontainer/nuget.versioning/index.json 216ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.versioning/5.6.0/nuget.versioning.5.6.0.nupkg
  OK https://api.nuget.org/v3-flatcontainer/nuget.common/5.6.0/nuget.common.5.6.0.nupkg 35ms
  OK https://api.nuget.org/v3-flatcontainer/nuget.versioning/5.6.0/nuget.versioning.5.6.0.nupkg 36ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.frameworks/index.json
  OK https://api.nuget.org/v3-flatcontainer/nuget.frameworks/index.json 103ms
  GET https://api.nuget.org/v3-flatcontainer/nuget.frameworks/5.6.0/nuget.frameworks.5.6.0.nupkg
  OK https://api.nuget.org/v3-flatcontainer/nuget.frameworks/5.6.0/nuget.frameworks.5.6.0.nupkg 32ms
```

*(note that Newtonsoft.Json is not downloaded because it is already available on my machine in a fallback folder... I
think)*

From this request log, we can generate a unweighted directed acyclic graph (DAG) representing all HTTP requests and
their relationship with each other. 

- The **node** is the request URL
- The **outgoing edges** for each node are to the requests higher up in the log that have completed
- Each **outgoing edge** can be considered a request dependency

This generates a very dense graph. As the log gets longer, the list of completed requests grows so
each new started request will depend on an ever grown lists of requests.

For example, this is the graph from that request log above:

![Example request log dependency graph](img/2020-05-22-request-graph-unpruned-ex.png)

## How many request logs should I parse?

Well, from my experimentation, 10 logs looks like enough and 20 is more than sufficient. The following script
incrementally tests merging more and more request logs and then tests the time it takes to replay the request graph.

```powershell
.\test-log-merge-asymptote.ps1 `
    -iterationCount 20 `
    -variantName "mysource" `
    -solutionName "OrchardCore"
```

Unsurprisingly, the average time to replay the request graph is asymtotal with respect to the number of logs merged.
This picture below went up to 70 request logs merged. Very quickly, the total request duration approached just over
6 seconds.

![Asymptotal simulated restore duration](img/2020-05-22-logs-per-graph.png)

