$friendlyName = "5914c830-f5c7-4dae-aeed-86566bbf213a"

Function Remove-TestCertificates {
    Param(
        [String] $CertificateStorePath
    )

    Get-ChildItem $CertificateStorePath |
        Where-Object { $_.FriendlyName -match $friendlyName } |
        Remove-Item
}

Remove-TestCertificates -CertificateStorePath "Cert:\CurrentUser\My"
Remove-TestCertificates -CertificateStorePath "Cert:\CurrentUser\Root"
Remove-TestCertificates -CertificateStorePath "Cert:\LocalMachine\My"
Remove-TestCertificates -CertificateStorePath "Cert:\LocalMachine\My"

$FilesToDelete = [System.IO.Path]::Combine($PSScriptRoot, "*.pfx")

Remove-Item $FilesToDelete