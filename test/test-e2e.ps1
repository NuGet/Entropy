. (Join-Path $PSScriptRoot "..\scripts\perftests\PerformanceTestUtilities.ps1")

$testDir = Join-Path $PSScriptRoot "testCases"
$testCases = Get-ChildItem (Join-Path $testDir "Test-*.ps1")
$testVariantName = "teste2e"
$nugetOrgVariantName = "nugetorg"
$solutionName = "ExampleProj"
$requestGraphPath = Join-Path $PSScriptRoot "..\out\graphs\requestGraph-$testVariantName-$solutionName.json.gz"
$operationGraphPath = Join-Path $PSScriptRoot "..\out\graphs\operationGraph-$solutionName.json.gz"
$nugetOrgRequestGraphPath = Join-Path $PSScriptRoot "..\out\graphs\requestGraph-$nugetOrgVariantName-$solutionName.json.gz"
$packageHelper = Join-Path $PSScriptRoot "..\src\PackageHelper\PackageHelper.csproj"
$dockerName = "nuget-server"
$dockerDataDir = Join-Path $PSScriptRoot "..\out\baget-data"

$imageName = "loicsharma/baget:26b871f70f849457c4de4032ddeabb06c09dad81"
$apiKey = [Guid]::NewGuid().ToString()

Log "Starting test package source" "Magenta"
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

Log "Discovering packages" "Magenta"
& (Join-Path $PSScriptRoot "..\discover-packages.ps1") `
    -variantName $testVariantName `
    -testCases $testCases `
    -maxDownloadsPerId 2

Log "Pushing packages" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    push $source `
    --api-key $apiKey

Log "Running test restores with test package source" "Magenta"
& (Join-Path $PSScriptRoot "..\run-tests.ps1") `
    -variantName $testVariantName `
    -iterations 2 `
    -sources @($source) `
    -testCases $testCases

Log "Running test restores with default sources" "Magenta"
& (Join-Path $PSScriptRoot "..\run-tests.ps1") `
    -variantName "default" `
    -fast `
    -testCases $testCases

Log "Parsing restore logs" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    parse-restore-logs `
    --write-graphviz

Log "Replaying request graph" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    replay-request-graph $requestGraphPath `
    --iterations 2

Log "Converting request graph to operation graph" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    convert-graph $requestGraphPath `
    --write-graphviz `
    --no-variant-name

Log "Converting operation graph to request graph" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    convert-graph $operationGraphPath `
    --sources "https://api.nuget.org/v3/index.json" `
    --variant-name $nugetOrgVariantName `
    --write-graphviz

Log "Replaying generating request graph" "Magenta"
dotnet run `
    --framework netcoreapp3.1 `
    --project $packageHelper `
    -- `
    replay-request-graph $nugetOrgRequestGraphPath `
    --iterations 2

Log "Testing max concurrency" "Magenta"
& (Join-Path $PSScriptRoot "test-max-concurrency.ps1") `
    -variantNames $testVariantName `
    -iterations 2 `
    -initialMaxConcurrency 4

Log "Testing log merge asymptote" "Magenta"
Move-Item (Join-Path $PSScriptRoot "..\out\logs") (Join-Path $PSScriptRoot "..\out\all-logs")
Move-Item (Join-Path $PSScriptRoot "..\out\graphs") (Join-Path $PSScriptRoot "..\out\all-graphs")

& (Join-Path $PSScriptRoot "test-log-merge-asymptote.ps1") `
    -variantName $testVariantName `
    -solutionName $solutionName `
    -iterations 2 `
    -noConfirm

Log "Stopping test package source" "Magenta"
Log "Stopping docker container..."
docker stop $dockerName

Log "Removing docker container..."
docker rm $dockerName --force
