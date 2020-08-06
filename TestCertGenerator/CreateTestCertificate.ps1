Param(
    [string] $HashAlgorithm = 'SHA256',
    [int] $KeyLength = 2048,
    [DateTime] $NotBefore = [System.DateTime]::Now,
    [DateTime] $NotAfter = [System.DateTime]::Now.AddDays(1),
    [switch] $UseLocalMachineStore,
    [string] $PfxFilePath,
    [string] $Password,
    [switch] $AddAsTrustedRootAuthority,
    [switch] $AlternateSignatureAlgorithm,
    [string] $Type = "CodeSigningCert",
    [switch] $GenerateCerFile

)

$friendlyName = "5914c830-f5c7-4dae-aeed-86566bbf213a"

If ($UseLocalMachineStore) {
    $certificateStoreLocation = "LocalMachine"
} Else {
    $certificateStoreLocation = "CurrentUser"
}

Write-Host Creating a new test certificate for testing NuGet package signing.

If ($AlternateSignatureAlgorithm) {
    $certificate = New-SelfSignedCertificate `
        -Type $Type `
        -CertStoreLocation "Cert:\$certificateStoreLocation\My" `
        -FriendlyName $friendlyName `
        -Subject "CN=Test certificate for testing NuGet package signing" `
        -KeyAlgorithm RSA `
        -HashAlgorithm $HashAlgorithm `
        -KeyLength $KeyLength `
        -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
        -KeySpec Signature `
        -NotBefore $NotBefore `
        -NotAfter $NotAfter `
        -AlternateSignatureAlgorithm `
        -KeyUsage CertSign, CRLSign
} Else {
    $certificate = New-SelfSignedCertificate `
        -Type $Type `
        -CertStoreLocation "Cert:\$certificateStoreLocation\My" `
        -FriendlyName $friendlyName `
        -Subject "CN=Test certificate for testing NuGet package signing" `
        -KeyAlgorithm RSA `
        -HashAlgorithm $HashAlgorithm `
        -KeyLength $KeyLength `
        -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
        -KeySpec Signature `
        -NotBefore $NotBefore `
        -NotAfter $NotAfter `
        -KeyUsage CertSign, CRLSign
}

$thumbprint = $certificate.Thumbprint
Write-Host "Certificate created with fingerprint:  ${thumbprint}"

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

If ([string]::IsNullOrEmpty($PfxFilePath)) {
    $PfxFilePath=".\$thumbprint.pfx"
}

If ([string]::IsNullOrEmpty($Password)) {
    $output = Export-PfxCertificate -Cert "Cert:\$certificateStoreLocation\My\$thumbprint" -ProtectTo $currentUser -FilePath $PfxFilePath
} Else {
    $PasswordSecureString = ConvertTo-SecureString $Password -AsPlainText -Force
    $output = Export-PfxCertificate -Cert "Cert:\$certificateStoreLocation\My\$thumbprint" -Password $PasswordSecureString -FilePath $PfxFilePath
}

if ($GenerateCerFile) {
    $CertFilePath=".\$thumbprint.cer"
    $outputCert = Export-Certificate -Cert "Cert:\$certificateStoreLocation\My\$thumbprint" -Type CERT -FilePath $CertFilePath
}

If ($AddAsTrustedRootAuthority) {
    Get-ChildItem "Cert:\$certificateStoreLocation\My\$thumbprint" | Remove-Item

    If ($PasswordSecureString) {
        $output = Import-PfxCertificate -FilePath $PfxFilePath -CertStoreLocation Cert:\$certificateStoreLocation\Root -Password $PasswordSecureString
    } Else {
        $output = Import-PfxCertificate -FilePath $PfxFilePath -CertStoreLocation Cert:\$certificateStoreLocation\Root
    }

    $certificateStoreName = "Root"
} else {
    $certificateStoreName = "My"
}

Write-Host "Certificate added to certificate store:  ${certificateStoreLocation}\${certificateStoreName}"
Write-Host "Certificate exported to:  ${PfxFilePath}"

if ($GenerateCerFile) {
    "Certificate exported to:  ${CertFilePath}"
}