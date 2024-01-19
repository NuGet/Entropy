# Description

TrustTestCert is a simple, cross-platform CLI for adding and removing trust for test code signing certificates.

`dotnet run --framework net7.0 -- -?` output:

```text
Usage:  TrustTestCert.exe <add|remove> --certificate <CertificateFilePath> [option]

  Option                           Description
  -------------------------------- -------------------------------------------
  --versioned-sdk-directory, -vsd  The versioned .NET SDK root directory that
                                   contains trustedroots\codesignctl.pem.
                                   On Windows, this option is never used.
                                   On Linux/macOS, this option is required.
Examples:

  Windows:
    TrustTestCert.exe add -c .\test.cer
      Adds the certificate to the current user's root store.
  
    TrustTestCert.exe add -c .\test.pfx
      Adds the certificate and its private key to the current user's root store.

    TrustTestCert.exe remove -c .\test.cer
      Removes the certificate from the current user's root store.

  Linux/macOS:
    TrustTestCert add -c ./test.pem -vsd ~/dotnet/sdk/7.0.100
      Adds the certificate to the specified .NET SDK's fallback certificate
      bundle.

    TrustTestCert remove -c ./test.pem -vsd ~/dotnet/sdk/7.0.100
      Removes the certificate from the specified .NET SDK's fallback
      certificate bundle.
```

## Windows

If you're testing signing with a trusted test certificate in the Windows certificate store (i.e.:  with the `CertificateStoreName` option), you should pass the path to the .pfx file.  If you're testing signing with a trusted test certificate in a PFX file (i.e.:  with the `CertificatePath` option), you can pass the path to either the .pfx file or the .cer file.