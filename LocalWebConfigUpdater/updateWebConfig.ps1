param (
    $ParamsFile,
    $WebConfig
)

$paramsJson = Get-Content $ParamsFile | ConvertFrom-Json
$webConfigXml = New-Object xml
$webConfigXml.Load($WebConfig)

$paramsSettings = $paramsJson.parameters.appSettings.value

$paramNames = $paramsSettings | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty name
$webConfigSettings = $webConfigXml.configuration.appSettings.add

foreach ($setting in $webConfigSettings) {
    if ($paramNames -contains $setting.key) {
        $k = $setting.key
        $oldValue = $setting.value
        $newValue = $paramsSettings.$k
        $setting.value = $newValue
        Write-Output "$k : $oldValue => $newValue"
    }
}

$webConfigConnectionStrings = $webConfigXml.configuration.connectionStrings.add
$paramsConnectionStrings = $paramsJson.parameters.connectionStrings.value
$paramsCsNames = $paramsConnectionStrings | Select-Object -ExpandProperty name

Write-Output "=============== connection strings";

foreach ($cs in $webConfigConnectionStrings) {
    if ($paramsCsNames -contains $cs.name) {
        $n = $cs.name
        $oldValue = $cs.connectionString
        $newValue = ($paramsConnectionStrings | Where-Object { $_.name -eq $n }).connectionString
        $cs.connectionString = $newValue
        Write-Output "$n : $oldValue => $newValue";
    }
}

$webConfigXml.Save($WebConfig)

# foreach ($paramName in $paramNames){
#     $paramValue = $paramsSettings.$paramName;
#     # Write-Output "$paramName => $paramValue";
# }