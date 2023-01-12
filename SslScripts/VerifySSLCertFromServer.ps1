<#
    .SYNOPSIS
    This script is used to verify the SSL certificate from the server
#>

$TestMaxRounds = 1000
$SleepDurationInSeconds = 6

$resultPath = ".\result.csv"

# Place a list of URLs to test under the same domain
$URLs = @("https://api.nuget.org/v3-registration5-gz-semver2/newtonsoft.json/index.json",
          "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json",
          "https://api.nuget.org/v3/index.json")
$Domain = "api.nuget.org"
$CertSubjectName = "CN=*.nuget.org"

$TestRound = 1
$SuccessedTimes = 0
$FailedTimes = 0
while ($TestRound -le $TestMaxRounds)
{
    Write-Host "Round: ", $TestRound

    $URL = $URLs | Get-Random
    $request = [Net.WebRequest]::Create($URL)

    $servicePoint = $request.ServicePoint
    # Set "MaxIdleTime" as 0 to ensure that the certificate is refreshed from the server again each round
    $servicePoint.MaxIdleTime = 0
    Write-Host "ServicePointHash: ", $servicePoint.GetHashCode()

    try {
        $request.GetResponse().Dispose()
    } catch
    {

    }

    $certificate = $request.ServicePoint.Certificate
    if ($null -ne $certificate)
    {
        $subjectName = $certificate.Subject.Split(",")[0]

        if ($subjectName -eq $CertSubjectName)
        {
            $SuccessedTimes = $SuccessedTimes + 1
        }
        else
        {
            $FailedTimes = $FailedTimes + 1

            $dnsRecord = (Resolve-DnsName $Domain | where-Object { $_.QueryType -eq "A" })[0]

            $date = (Get-Date).ToUniversalTime()
            Write-Host $TestRound, $date, $subjectName, $dnsRecord.Name, $dnsRecord.IP4Address, $URL -ForegroundColor red

            $log = @(
                [pscustomobject]@{
                    TestRound = $TestRound
                    Date_UTC = $date
                    ReturnedCertSubjectName = $subjectName
                    DNSRecord = $dnsRecord.Name
                    IP4Address = $dnsRecord.IP4Address
                    TestURL = $URL
                }
            )

            $log | Export-Csv -Path $resultPath -Append -NoTypeInformation
        }
    }

    $TestRound = $TestRound + 1
    Start-Sleep -Seconds $SleepDurationInSeconds
}

Write-Host "Succeeded: ", $SuccessedTimes
Write-Host "Failed: ", $FailedTimes