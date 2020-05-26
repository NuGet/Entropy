. (Join-Path $PSScriptRoot "..\scripts\perftests\PerformanceTestUtilities.ps1")

$testDir = Join-Path $PSScriptRoot "testCases"
$testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")
$variantName = "teste2e"
$solutionName = "ExampleProj"
$requestGraphPath = Join-Path $PSScriptRoot "..\out\graphs\requestGraph-$variantName-$solutionName.json.gz"
$packageHelper = Join-Path $PSScriptRoot "..\src\PackageHelper\PackageHelper.csproj"
$dockerName = "nuget-server"
$dockerDataDir = Join-Path $PSScriptRoot "..\out\baget-data"

$imageName = "loicsharma/baget:26b871f70f849457c4de4032ddeabb06c09dad81"
$apiKey = [Guid]::NewGuid().ToString()

# 0. Start up the test package source
$ps = @(docker ps --filter "name=$dockerName")
if ($ps.Length -gt 1) {
    Log "Stopping docker container..."
    docker stop $dockerName
    
    Log "Removing docker container..."
    docker rm $dockerName --force
}

Log "Fetching docker image..."
docker pull $imageName

Log "Starting docker container..."
docker run `
    --name $dockerName `
    -d -P `
    --env "ApiKey=$apiKey" `
    --env "Storage__Type=FileSystem" `
    --env "Storage__Path=/var/baget/packages" `
    --env "Database__Type=Sqlite" `
    --env "Database__ConnectionString=Data Source=/var/baget/baget.db" `
    --env "Search__Type=Database" `
    --volume "$($dockerDataDir):/var/baget" `
    $imageName

Log "Determining port..."
$port = docker port $dockerName `
    | Where-Object { $_.StartsWith("80/tcp") } `
    | ForEach-Object { $_.Split(" -> ")[-1].Split(":")[-1] }
if (!$port) {
    throw "Could not determine the port."
}
$source = "http://localhost:$port/v3/index.json"
Log "The package source is $source"

# Discover packages
& (Join-Path $PSScriptRoot "..\discover-packages.ps1") `
    -variantName $variantName `
    -testCases $testCases `
    -maxDownloadsPerId 2

# Push all packages
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    push $source `
    --api-key $apiKey

# Run the tests with the custom source
& (Join-Path $PSScriptRoot "..\run-tests.ps1") `
    -variantName $variantName `
    -iterations 2 `
    -sources @($source) `
    -testCases $testCases

# Run the tests with the default sources
& (Join-Path $PSScriptRoot "..\run-tests.ps1") `
    -variantName "default" `
    -fast `
    -testCases $testCases

# Parse the logs
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    parse-restore-logs `
    --write-graphviz

# Replay request graph
dotnet run `
    replay-request-graph $requestGraphPath `
    --framework netcoreapp3.1 `
    --project .\src\PackageHelper\PackageHelper.csproj `
    -- `
    --iterations 2

# Convert request graph to operation graph
dotnet run `
    convert-graph $requestGraphPath `
    --framework netcoreapp3.1 `
    --project .\src\PackageHelper\PackageHelper.csproj `
    -- `
    --write-graphviz

# Test max concurrency
& (Join-Path $PSScriptRoot "test-max-concurrency.ps1") `
    -variantNames $variantName `
    -iterations 2 `
    -initialMaxConcurrency 4

# Move test data in preparation for the next script
Move-Item (Join-Path $PSScriptRoot "..\out\logs") (Join-Path $PSScriptRoot "..\out\all-logs")
Move-Item (Join-Path $PSScriptRoot "..\out\graphs") (Join-Path $PSScriptRoot "..\out\all-graphs")

# Test log merge asymptote
& (Join-Path $PSScriptRoot "test-log-merge-asymptote.ps1") `
    -variantName $variantName `
    -solutionName $solutionName `
    -iterations 2 `
    -noConfirm

# Stop the docker container
Log "Stopping docker container..."
docker stop $dockerName

# Removing the docker container
Log "Removing docker container..."
docker rm $dockerName --force
