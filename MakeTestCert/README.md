# Description

MakeTestCert is a simple, cross-platform CLI for creating test code signing certificates.

`dotnet run --framework net7.0 -- -?` output:

```text
Usage:  MakeTestCert.exe [option(s)]

  Option                   Description                     Default
  ------------------------ ------------------------------- -----------------
  --key-algorithm, -ka     RSA or ECDSA                    RSA
  --key-size, -ks          RSA key size in bits            3072
  --named-curve, -nc       ECDSA named curve               nistP256
  --not-after, -na         validity period end datetime    (now)
  --not-before, -nb        validity period start datetime  (now + 2 hours)
  --password, -p           PFX file password               (none)
  --output-directory, -od  output directory path           .\
  --subject, -s            certificate subject             CN=NuGet testing
  --validity-period, -vp   validity period (in hours)      2

Examples:

  MakeTestCert.exe
    Creates an RSA 3072-bit certificate valid for 2 hours from creation time.

  MakeTestCert.exe -vp 8
    Creates an RSA 3072-bit certificate valid for 8 hours from creation time.

  MakeTestCert.exe -nb "2022-08-01 08:00" -na "2022-08-01 16:00"
    Creates an RSA 3072-bit certificate valid for the specified local time
    period.

  MakeTestCert.exe -od .\certs
    Creates an RSA 3072-bit certificate valid for 2 hours in the 'certs'
    subdirectory.

  MakeTestCert.exe -ks 4096 -s CN=untrusted
    Creates an RSA 4096-bit certificate valid for 2 hours with the subject
    'CN=untrusted'.
```
