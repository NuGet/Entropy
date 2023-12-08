// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MakeTestCert
{
    internal static class Program
    {
        private const int Success = 0;
        private const int Error = 1;

        private static readonly ECCurve DefaultECCurve = ECCurve.NamedCurves.nistP256;
        private static readonly HashAlgorithmName DefaultHashAlgorithmName = HashAlgorithmName.SHA384;
        private static readonly string DefaultKeyAlgorithmName = "RSA";
        private static readonly ushort DefaultKeySizeInBits = 3072;
        private static readonly RSASignaturePadding DefaultRsaSignaturePadding = RSASignaturePadding.Pkcs1;
        private static readonly X500DistinguishedName DefaultSubject = new("CN=NuGet testing");
        private static readonly ushort DefaultValidityPeriodInHours = 2;

        private static int Main(string[] args)
        {
            try
            {
                return MainCore(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return Error;
            }
        }

        private static int MainCore(string[] args)
        {
            if (args.Any(arg =>
                string.Equals(arg, "-?", StringComparison.Ordinal)
                    || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp();

                return Success;
            }

            HashSet<string> ekuSet = new(StringComparer.Ordinal);
            HashAlgorithmName hashAlgorithmName = DefaultHashAlgorithmName;
            string keyAlgorithmName = DefaultKeyAlgorithmName;
            ushort keySizeInBits = DefaultKeySizeInBits;
            ECCurve namedCurve = DefaultECCurve;
            DateTimeOffset? notAfter = null;
            DateTimeOffset notBefore = DateTimeOffset.Now;
            DirectoryInfo outputDirectory = new(".");
            string? password = null;
            RSASignaturePadding rsaSignaturePadding = DefaultRsaSignaturePadding;
            X500DistinguishedName subject = DefaultSubject;
            ushort validityPeriodInHours = DefaultValidityPeriodInHours;

            int argsCount = args.Length;

            for (var i = 0; i < argsCount - 1; i += 2)
            {
                string arg = args[i].ToLowerInvariant();
                string nextArg = args[i + 1];

                switch (arg)
                {
                    case "-eku":
                    case "--extended-key-usage":
                        ekuSet.Add(nextArg);
                        break;

                    case "-ha":
                    case "--hash-algorithm":
                        if (!TryGetHashAlgorithmName(nextArg, out hashAlgorithmName))
                        {
                            Console.Error.WriteLine($"Hash algorithm '{nextArg}' is unsupported.");
                            Console.WriteLine();

                            PrintHelp();

                            return Error;
                        }
                        break;

                    case "-ka":
                    case "--key-algorithm":
                        keyAlgorithmName = nextArg;
                        break;

                    case "-ks":
                    case "--key-size":
                        keySizeInBits = ushort.Parse(nextArg);
                        break;

                    case "-na":
                    case "--not-after":
                        notAfter = DateTimeOffset.Parse(nextArg);
                        break;

                    case "-nb":
                    case "--not-before":
                        notBefore = DateTimeOffset.Parse(nextArg);
                        break;

                    case "-nc":
                    case "--named-curve":
                        if (string.Equals(nextArg, "nistP256", StringComparison.OrdinalIgnoreCase))
                        {
                            namedCurve = ECCurve.NamedCurves.nistP256;
                        }
                        else if (string.Equals(nextArg, "nistP384", StringComparison.OrdinalIgnoreCase))
                        {
                            namedCurve = ECCurve.NamedCurves.nistP384;
                        }
                        else if (string.Equals(nextArg, "nistP521", StringComparison.OrdinalIgnoreCase))
                        {
                            namedCurve = ECCurve.NamedCurves.nistP521;
                        }
                        else
                        {
                            Console.Error.WriteLine($"Named curve '{nextArg}' is unsupported.");
                            Console.WriteLine();

                            PrintHelp();

                            return Error;
                        }
                        break;

                    case "-od":
                    case "--output-directory":
                        outputDirectory = new DirectoryInfo(nextArg);
                        break;

                    case "-p":
                    case "--password":
                        password = nextArg;
                        break;

                    case "-rsp":
                    case "--rsa-signature-padding":
                        if (!Enum.TryParse(nextArg, ignoreCase: true, out RSASignaturePaddingMode mode)
                            || (mode != RSASignaturePaddingMode.Pkcs1 && mode != RSASignaturePaddingMode.Pss))
                        {
                            Console.Error.WriteLine($"RSA signature padding '{nextArg}' is unsupported.");
                            Console.WriteLine();

                            PrintHelp();

                            return Error;
                        }

                        if (mode == RSASignaturePaddingMode.Pkcs1)
                        {
                            rsaSignaturePadding = RSASignaturePadding.Pkcs1;
                        }
                        else if (mode == RSASignaturePaddingMode.Pss)
                        {
                            rsaSignaturePadding = RSASignaturePadding.Pss;
                        }
                        break;

                    case "-s":
                    case "--subject":
                        subject = new X500DistinguishedName(nextArg);
                        break;

                    case "-vp":
                    case "--validity-period":
                        validityPeriodInHours = ushort.Parse(nextArg);
                        break;

                    default:
                        PrintHelp();

                        return Error;
                }
            }

            OidCollection ekus = new();

            foreach (string eku in ekuSet)
            {
                ekus.Add(new Oid(eku));
            }

            if (ekus.Count == 0)
            {
                ekus.Add(Oids.CodeSigningEku);
            }

            notAfter ??= notBefore.AddHours(validityPeriodInHours);

            // Ensure the directory exists.
            outputDirectory.Create();

            if (string.Equals(keyAlgorithmName, "RSA", StringComparison.OrdinalIgnoreCase))
            {
                using (RSA keyPair = RSA.Create(keySizeInBits))
                {
                    CertificateRequest certificateRequest = new(
                        subject,
                        keyPair,
                        hashAlgorithmName,
                        rsaSignaturePadding);

                    Console.WriteLine($"Signature algorithm:    RSA {hashAlgorithmName}");
                    Console.WriteLine($"Key size (bits):        {keySizeInBits}");
                    Console.WriteLine($"RSA signature padding:  {rsaSignaturePadding.Mode}");

                    CreateCertificate(
                        certificateRequest,
                        ekus,
                        notBefore,
                        notAfter.Value,
                        password,
                        outputDirectory);
                }
            }
            else if (string.Equals(keyAlgorithmName, "ECDSA", StringComparison.OrdinalIgnoreCase))
            {
                using (ECDsa keyPair = ECDsa.Create(namedCurve))
                {
                    CertificateRequest certificateRequest = new(
                        subject,
                        keyPair,
                        hashAlgorithmName);

                    Console.WriteLine($"Signature algorithm:    ECDSA {hashAlgorithmName}");
                    Console.WriteLine($"Named curve:            {namedCurve.Oid.FriendlyName}");

                    CreateCertificate(
                        certificateRequest,
                        ekus,
                        notBefore,
                        notAfter.Value,
                        password,
                        outputDirectory);
                }
            }
            else
            {
                Console.Error.WriteLine($"Key algorithm '{keyAlgorithmName}' is unsupported.");
                Console.WriteLine();

                PrintHelp();

                return Error;
            }

            return Success;
        }

        private static void PrintHelp()
        {
            FileInfo file = new(Environment.ProcessPath!);

            Console.WriteLine($"Usage:  {file.Name} [option(s)]");
            Console.WriteLine();
            Console.WriteLine($"  Option                        Description                    Default");
            Console.WriteLine($"  ----------------------------- ------------------------------ -----------------");
            Console.WriteLine($"  --extended-key-usage, -eku    extended key usage (EKU)       {Oids.CodeSigningEku.Value}");
            Console.WriteLine($"  --hash-algorithm, -ha         signature hash algorithm       {DefaultHashAlgorithmName.Name!.ToLowerInvariant()}");
            Console.WriteLine($"                                (sha256, sha384, or sha512)");
            Console.WriteLine($"  --key-algorithm, -ka          RSA or ECDSA                   {DefaultKeyAlgorithmName}");
            Console.WriteLine($"  --key-size, -ks               RSA key size in bits           {DefaultKeySizeInBits}");
            Console.WriteLine($"  --named-curve, -nc            ECDSA named curve              {DefaultECCurve.Oid.FriendlyName}");
            Console.WriteLine($"  --not-after, -na              validity period end datetime   (now)");
            Console.WriteLine($"  --not-before, -nb             validity period start datetime (now + {DefaultValidityPeriodInHours} hours)");
            Console.WriteLine($"  --output-directory, -od       output directory path          .{Path.DirectorySeparatorChar}");
            Console.WriteLine($"  --password, -p                PFX file password              (none)");
            Console.WriteLine($"  --rsa-signature-padding, -rsp RSA signature padding          {DefaultRsaSignaturePadding.Mode.ToString().ToLowerInvariant()}");
            Console.WriteLine($"                                (pkcs1 or pss)");
            Console.WriteLine($"  --subject, -s                 certificate subject            {DefaultSubject.Name}");
            Console.WriteLine($"  --validity-period, -vp        validity period (in hours)     {DefaultValidityPeriodInHours}");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine();
            Console.WriteLine("  Common values for --extended-key-usage / -eku are defined in RFC 5280, section 4.2.1.12 and include:");
            Console.WriteLine($"    {Oids.ServerAuthenticationEku.Value}: {Oids.ServerAuthenticationEku.FriendlyName}");
            Console.WriteLine($"    {Oids.ClientAuthenticationEku.Value}: {Oids.ClientAuthenticationEku.FriendlyName}");
            Console.WriteLine($"    {Oids.CodeSigningEku.Value}: {Oids.CodeSigningEku.FriendlyName}");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name}");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} {DefaultKeySizeInBits}-bit code signing certificate valid for {DefaultValidityPeriodInHours} hours from creation time.");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name} -vp 8");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} {DefaultKeySizeInBits}-bit code signing certificate valid for 8 hours from creation time.");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name} -nb \"2022-08-01 08:00\" -na \"2022-08-01 16:00\"");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} {DefaultKeySizeInBits}-bit code signing certificate valid for the specified local time ");
            Console.WriteLine("    period.");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name} -od .{Path.DirectorySeparatorChar}certs");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} {DefaultKeySizeInBits}-bit code signing certificate valid for {DefaultValidityPeriodInHours} hours in the 'certs' ");
            Console.WriteLine("    subdirectory.");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name} -ks 4096 -s CN=untrusted");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} 4096-bit code signing certificate valid for {DefaultValidityPeriodInHours} hours with the subject ");
            Console.WriteLine("    'CN=untrusted'.");
            Console.WriteLine();
            Console.WriteLine($"  {file.Name} -eku 1.3.6.1.5.5.7.3.1 -eku 1.3.6.1.5.5.7.3.2");
            Console.WriteLine($"    Creates an {DefaultKeyAlgorithmName} {DefaultKeySizeInBits}-bit TLS certificate valid for {DefaultValidityPeriodInHours} hours from creation time.");
        }

        private static void CreateCertificate(
            CertificateRequest certificateRequest,
            OidCollection ekus,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string? password,
            DirectoryInfo outputDirectory)
        {
            X509SubjectKeyIdentifierExtension skiExtension = new(certificateRequest.PublicKey, critical: false);

            certificateRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true));
            certificateRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(ekus, critical: true));
            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyCertSign,
                    critical: true));
            certificateRequest.CertificateExtensions.Add(skiExtension);
            certificateRequest.CertificateExtensions.Add(
                X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(skiExtension));

            using (X509Certificate2 certificate = certificateRequest.CreateSelfSigned(notBefore, notAfter))
            {
                string fingerprint = certificate.GetCertHashString().ToLowerInvariant();

                Console.Write("EKU's:                  ");

                for (var i = 0; i < ekus.Count; ++i)
                {
                    Oid eku = ekus[i];

                    if (i > 0)
                    {
                        Console.Write("                        ");
                    }

                    Console.Write(eku.Value);

                    if (!string.IsNullOrWhiteSpace(eku.FriendlyName))
                    {
                        Console.Write($" ({eku.FriendlyName})");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine($"NotBefore:              {certificate.NotBefore:yyyy-MM-dd HH:mm:ssK}");
                Console.WriteLine($"NotAfter:               {certificate.NotAfter:yyyy-MM-dd HH:mm:ssK}");
                Console.WriteLine($"Fingerprint (SHA-1):    {fingerprint}");
                Console.WriteLine($"Fingerprint (SHA-256):  {certificate.GetCertHashString(HashAlgorithmName.SHA256).ToLowerInvariant()}");
                Console.WriteLine($"PFX password:           {(string.IsNullOrEmpty(password) ? string.Empty : password)}");
                Console.WriteLine($"Output directory:       {outputDirectory.FullName}");
                Console.WriteLine("Certificate files:");

                WritePfxFile(certificate, outputDirectory, fingerprint, password);
                WriteDerFile(certificate, outputDirectory, fingerprint);
                WritePemFile(certificate, outputDirectory, fingerprint);
                WriteP7bFile(certificate, outputDirectory, fingerprint);
            }
        }

        private static bool TryGetHashAlgorithmName(string value, out HashAlgorithmName hashAlgorithmName)
        {
            switch (value.ToLowerInvariant())
            {
                case "sha256":
                    hashAlgorithmName = HashAlgorithmName.SHA256;
                    break;

                case "sha384":
                    hashAlgorithmName = HashAlgorithmName.SHA384;
                    break;

                case "sha512":
                    hashAlgorithmName = HashAlgorithmName.SHA512;
                    break;

                default:
                    hashAlgorithmName = DefaultHashAlgorithmName;
                    return false;
            }

            return true;
        }

        private static void WriteDerFile(X509Certificate2 certificate, DirectoryInfo directory, string fileName)
        {
            FileInfo file = new(Path.Combine(directory.FullName, $"{fileName}.cer"));

            File.WriteAllBytes(file.FullName, certificate.RawData);

            Console.WriteLine($"    {file.Name}");
        }

        private static void WritePemFile(X509Certificate2 certificate, DirectoryInfo directory, string fileName)
        {
            FileInfo file = new(Path.Combine(directory.FullName, $"{fileName}.pem"));

            using (StreamWriter writer = new(file.FullName))
            {
                string pem = certificate.ExportCertificatePem();

                writer.WriteLine(pem);
            }

            Console.WriteLine($"    {file.Name}");
        }

        private static void WritePfxFile(
            X509Certificate2 certificate,
            DirectoryInfo directory,
            string fileName,
            string? password)
        {
            FileInfo file = new(Path.Combine(directory.FullName, $"{fileName}.pfx"));
            byte[] bytes;

            if (string.IsNullOrEmpty(password))
            {
                bytes = certificate.Export(X509ContentType.Pfx);
            }
            else
            {
                bytes = certificate.Export(X509ContentType.Pfx, password);
            }

            File.WriteAllBytes(file.FullName, bytes);

            Console.WriteLine($"    {file.Name}");
        }

        private static void WriteP7bFile(X509Certificate2 certificate, DirectoryInfo directory, string fileName)
        {
            FileInfo file = new(Path.Combine(directory.FullName, $"{fileName}.p7b"));
            X509Certificate2Collection certificates = new(certificate);
            byte[]? bytes = certificates.Export(X509ContentType.Pkcs7);

            File.WriteAllBytes(file.FullName, bytes!);

            Console.WriteLine($"    {file.Name}");
        }
    }
}