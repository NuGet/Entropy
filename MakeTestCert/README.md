# Description

MakeTestCert is a simple, cross-platform CLI for creating test code signing certificates.

`dotnet run --framework net7.0 -- -?` output:

```text
Usage:  MakeTestCert.exe [option(s)]

  Option                        Description                    Default
  ----------------------------- ------------------------------ -----------------
  --extended-key-usage, -eku    extended key usage (EKU)       1.3.6.1.5.5.7.3.3
  --hash-algorithm, -ha         signature hash algorithm       sha384
                                (sha256, sha384, or sha512)
  --key-algorithm, -ka          RSA or ECDSA                   RSA
  --key-size, -ks               RSA key size in bits           3072
  --named-curve, -nc            ECDSA named curve              nistP256
  --not-after, -na              validity period end datetime   (now)
  --not-before, -nb             validity period start datetime (now + 2 hours)
  --output-directory, -od       output directory path          .\
  --password, -p                PFX file password              (none)
  --rsa-signature-padding, -rsp RSA signature padding          pkcs1
                                (pkcs1 or pss)
  --subject, -s                 certificate subject            CN=NuGet testing
  --validity-period, -vp        validity period (in hours)     2

Notes:

  Common values for --extended-key-usage / -eku are defined in RFC 5280, section 4.2.1.12 and include:
    1.3.6.1.5.5.7.3.1: Server Authentication
    1.3.6.1.5.5.7.3.2: Client Authentication
    1.3.6.1.5.5.7.3.3: Code Signing

Examples:

  MakeTestCert.exe
    Creates an RSA 3072-bit code signing certificate valid for 2 hours from creation time.

  MakeTestCert.exe -vp 8
    Creates an RSA 3072-bit code signing certificate valid for 8 hours from creation time.

  MakeTestCert.exe -nb "2022-08-01 08:00" -na "2022-08-01 16:00"
    Creates an RSA 3072-bit code signing certificate valid for the specified local time
    period.

  MakeTestCert.exe -od .\certs
    Creates an RSA 3072-bit code signing certificate valid for 2 hours in the 'certs'
    subdirectory.

  MakeTestCert.exe -ks 4096 -s CN=untrusted
    Creates an RSA 4096-bit code signing certificate valid for 2 hours with the subject
    'CN=untrusted'.

  MakeTestCert.exe -eku 1.3.6.1.5.5.7.3.1 -eku 1.3.6.1.5.5.7.3.2
    Creates an RSA 3072-bit TLS certificate valid for 2 hours from creation time.
```
