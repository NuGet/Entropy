param (
    [Parameter(Mandatory=$true)]$WebConfig,
    [Parameter(Mandatory=$true)]$ParamsFile
)

if (-not (Test-Path $WebConfig)) {
    throw "$WebConfig file does not exist";
}

if (-not (Test-Path $ParamsFile)) {
    throw "$ParamsFile file does not exist";
}

# Loading and saving Web.Config with Powershell's XML processing messes up the file, so we'll do good old search and replace

$wc = Get-Content -Raw $WebConfig;
$pf = Get-Content -Raw $ParamsFile | ConvertFrom-Json;

$appSettings = $pf.parameters.appSettings.value

foreach ($name in ($appSettings | Get-Member -Type NoteProperty | Select-Object -ExpandProperty Name))
{
    $value = $appSettings.$name;
    $value = $value -replace "\$\$",'$$$$$$$$';
    $value = $value -replace "<","&lt;";
    $value = $value -replace ">","&gt;";
    $search = "<add(?:\s+)key=`"$name`"(?:\s+)value=`"[^`"]*`"(?:\s*)/>";
    $replace = "<add key=`"$name`" value=`"$value`"/>";
    # Write-Host "Searching for '$search', replacing with '$replace'"
    $wc = $wc -replace $search,$replace
}

$connectionStrings = $pf.parameters.connectionStrings.value;

foreach ($cs in $connectionStrings) {
    $name = $cs.name;
    $value = $cs.connectionString;
    $value = $value -replace "\$\$",'$$$$$$$$';
    # Write-Host "connection string: $name => $value";
    $search = "<add(?:\s+)name=`"$name`"(?:\s+)connectionString=`"[^`"]*`"";
    $replace = "<add name=`"$name`" connectionString=`"$value`"";
    $wc = $wc -replace $search,$replace
}

$wc | Out-File -Encoding utf8 $WebConfig