Function Get-NupkgFiles(
    [Parameter(Mandatory = $True)][string]$NupkgUrl) {
    $zip = Get-ZipArchiveObject -Url $NupkgUrl
    $zip.Entries
}

Function Get-NupkgFileContent(
    [Parameter(Mandatory = $True)][string]$NupkgUrl,
    [Parameter(Mandatory = $True)][string]$Filename) {
    $zip = Get-ZipArchiveObject -Url $NupkgUrl
    $entry = $zip.Entries | Where-Object { $_.Name -like "*$Filename" } | Select-Object -First 1
    if ($entry) {
        $stream = $entry.Open()
        $reader = New-Object System.IO.StreamReader($stream)
        $reader.ReadToEnd()
    }
}

Function Get-ZipArchiveObject($Url) {
    $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing
    $content = $response.Content
    $stream = New-Object System.IO.MemoryStream (,$content)
    New-Object System.IO.Compression.ZipArchive $stream
}

Export-ModuleMember -Function Get-NupkgFiles
Export-ModuleMember -Function Get-NupkgFileContent