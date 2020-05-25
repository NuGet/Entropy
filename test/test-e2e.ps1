. (Join-Path $PSScriptRoot "..\scripts\perftests\PerformanceTestUtilities.ps1")

$testDir = Join-Path $PSScriptRoot "testCases"
$testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")
$variantName = "teste2e"
$solutionName = "ExampleProj"
$packageHelper = Join-Path $PSScriptRoot "..\src\PackageHelper\PackageHelper.csproj"
$dockerName = "nuget-server"
$dockerDataDir = Join-Path $PSScriptRoot "..\out\baget-data"

$apiKey = [Guid]::NewGuid().ToString()

# 0. Start up the test package source
$ps = @(docker ps --filter "name=$dockerName")
if ($ps.Length -gt 1) {
    Log "Stopping docker container..."
    docker stop $dockerName
    
    Log "Removing docker container..."
    docker rm $dockerName --force
}

Log "Starting docker container..."
docker run `
    --name $dockerName `
    --rm -d -P `
    --env "ApiKey=$apiKey" `
    --env "Storage__Type=FileSystem" `
    --env "Storage__Path=/var/baget/packages" `
    --env "Database__Type=Sqlite" `
    --env "Database__ConnectionString=Data Source=/var/baget/baget.db" `
    --env "Search__Type=Database" `
    --volume "$($dockerDataDir):/var/baget" `
    loicsharma/baget:5cd32c168b23a29ee0e6a16d69eeddbbab932808

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
    -testCases $testCases

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
    -testCases $testCase

# Run the tests with the default sources
& (Join-Path $PSScriptRoot "..\run-tests.ps1") `
    -variantName "default" `
    -fast `
    -testCases $testCase

# Parse the logs
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    parse-restore-logs

# Replay request graph
dotnet run `
    replay-request-graph (Join-Path $PSScriptRoot "..\out\request-graphs\requestGraph-$variantName-$solutionName.json.gz") `
    --iterations 2 `
    --framework netcoreapp3.1 `
    --project .\src\PackageHelper\PackageHelper.csproj

# Test max concurrency
& (Join-Path $PSScriptRoot "test-max-concurrency.ps1") `
    -variantNames $variantName `
    -iterations 2

# Test log merge asymptote
Move-Item (Join-Path $PSScriptRoot "..\out\logs") (Join-Path $PSScriptRoot "..\out\all-logs")
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
