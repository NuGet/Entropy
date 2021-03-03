// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Signing;

namespace ExtractCertificatesFromNuGetPackage
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine();

                if (args.Length != 1)
                {
                    PrintHelp();

                    return;
                }

                var file = new FileInfo(args[0]);

                await ExtractCertificatesAsync(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task ExtractCertificatesAsync(FileInfo file)
        {
            using (FileStream stream = file.OpenRead())
            using (var archive = new SignedPackageArchive(stream, Stream.Null))
            {
                PrimarySignature primarySignature = await archive.GetPrimarySignatureAsync(CancellationToken.None);

                if (primarySignature is null)
                {
                    Console.WriteLine("The package is not signed.");

                    return;
                }

                var certificatesDirectory = new DirectoryInfo($"{file.FullName}.certificates");

                certificatesDirectory.Create();

                Console.WriteLine($"Certificates extracted to {certificatesDirectory.FullName}");
                Console.WriteLine();

                ExtractCertificates(primarySignature, certificatesDirectory);

                if (primarySignature.Type == SignatureType.Repository)
                {
                    return;
                }

                RepositoryCountersignature repositoryCountersignature = RepositoryCountersignature.GetRepositoryCountersignature(primarySignature);

                if (repositoryCountersignature is null)
                {
                    return;
                }

                ExtractCertificates(primarySignature, repositoryCountersignature, certificatesDirectory);
            }
        }

        private static void ExtractCertificates(PrimarySignature primarySignature, DirectoryInfo certificatesDirectory)
        {
            using (IX509CertificateChain primaryCertificates = SignatureUtility.GetCertificateChain(primarySignature))
            {
                WriteCertificates(
                    "primary signature's",
                    certificatesDirectory,
                    certificatesDirectory,
                    primaryCertificates);

                SignedCms signedCms = primarySignature.SignedCms;
                SignerInfo primarySignerInfo = signedCms.SignerInfos[0];

                CheckSignatureValidity(primarySignerInfo, "primary");
                ReportCertificatesNotInSignedCms(primaryCertificates, signedCms);

                Timestamp timestamp = primarySignature.Timestamps.FirstOrDefault();

                if (timestamp is object)
                {
                    using (IX509CertificateChain timestampCertificates = SignatureUtility.GetTimestampCertificateChain(primarySignature))
                    {
                        DirectoryInfo timestampDirectory = certificatesDirectory.CreateSubdirectory("timestamp");

                        WriteCertificates(
                            "primary timestamp signature's",
                            certificatesDirectory,
                            timestampDirectory,
                            timestampCertificates);

                        SignerInfo timestampSignerInfo = timestamp.SignedCms.SignerInfos[0];

                        CheckSignatureValidity(timestampSignerInfo, "primary timestamp");
                        ReportCertificatesNotInSignedCms(timestampCertificates, timestamp.SignedCms);
                    }
                }
            }
        }

        private static void ExtractCertificates(
            PrimarySignature primarySignature,
            RepositoryCountersignature repositoryCountersignature,
            DirectoryInfo certificatesDirectory)
        {
            using (IX509CertificateChain countersignatureCertificates = SignatureUtility.GetCertificateChain(primarySignature, repositoryCountersignature))
            {
                DirectoryInfo countersignatureDirectory = certificatesDirectory.CreateSubdirectory("countersignature");

                WriteCertificates(
                    "repository countersignature's",
                    certificatesDirectory,
                    countersignatureDirectory,
                    countersignatureCertificates);

                CheckSignatureValidity(repositoryCountersignature.SignerInfo, "repository countersignature");
                ReportCertificatesNotInSignedCms(countersignatureCertificates, primarySignature.SignedCms);

                Timestamp timestamp = repositoryCountersignature.Timestamps.FirstOrDefault();

                if (timestamp is object)
                {
                    using (IX509CertificateChain timestampCertificates = SignatureUtility.GetTimestampCertificateChain(
                        primarySignature,
                        repositoryCountersignature))
                    {
                        DirectoryInfo timestampDirectory = countersignatureDirectory.CreateSubdirectory("timestamp");

                        WriteCertificates(
                            "repository countersignature timestamp signature's",
                            certificatesDirectory,
                            timestampDirectory,
                            timestampCertificates);

                        CheckSignatureValidity(timestamp.SignerInfo, "repository countersignature timestamp");
                        ReportCertificatesNotInSignedCms(timestampCertificates, timestamp.SignedCms);
                    }
                }
            }
        }

        private static void CheckSignatureValidity(SignerInfo signerInfo, string name)
        {
            try
            {
                signerInfo.CheckSignature(verifySignatureOnly: true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"The {name} signature is invalid.");
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static void ReportCertificatesNotInSignedCms(
            IX509CertificateChain certificateChain,
            SignedCms signedCms)
        {
            foreach (X509Certificate2 certificate in certificateChain)
            {
                if (!signedCms.Certificates.Contains(certificate))
                {
                    Console.Error.WriteLine($"The CMS certificates collection does not contain the following certificate:");
                    Console.Error.WriteLine($" Subject:              {certificate.Subject}");
                    Console.Error.WriteLine($" Issuer:               {certificate.Issuer}");
                    Console.Error.WriteLine($" Fingerprint (SHA-1):  {certificate.Thumbprint}");
                }
            }
        }

        private static void WriteCertificates(
            string name,
            DirectoryInfo rootDirectory,
            DirectoryInfo certificatesDirectory,
            IReadOnlyList<X509Certificate2> chain)
        {
            Console.WriteLine($"The {name} certificate chain:");
            Console.WriteLine();
            Console.WriteLine("  Fingerprint (SHA-1)                       Level  Relative File Path");
            Console.WriteLine("  ----------------------------------------- ------ -----------------------------------");

            for (int i = 0, iend = chain.Count - 1; i <= iend; ++i)
            {
                X509Certificate2 certificate = chain[i];
                string level;

                if (i == 0)
                {
                    level = "leaf";
                }
                else if (i == iend)
                {
                    level = "root";
                }
                else
                {
                    level = "int";
                }

                var fileName = $"{i}";
                FileInfo derFile = WriteDerEncodedCertificateFile(certificate, certificatesDirectory, fileName);
                FileInfo pemFile = WritePemEncodedCertificateFile(certificate, certificatesDirectory, fileName);
                string filePath;
                
                if (OperatingSystem.IsWindows())
                {
                    filePath = derFile.FullName.Substring(rootDirectory.FullName.Length);
                }
                else
                {
                    filePath = pemFile.FullName.Substring(rootDirectory.FullName.Length);
                }

                Console.WriteLine($"  {certificate.Thumbprint.ToLowerInvariant()}  {level,-4}   .{filePath}");
            }

            Console.WriteLine();
        }

        private static FileInfo WriteDerEncodedCertificateFile(X509Certificate2 certificate, DirectoryInfo directory, string fileName)
        {
            var file = new FileInfo(Path.Combine(directory.FullName, $"{fileName}.cer"));

            File.WriteAllBytes(file.FullName, certificate.RawData);

            return file;
        }

        private static FileInfo WritePemEncodedCertificateFile(X509Certificate2 certificate, DirectoryInfo directory, string fileName)
        {
            var file = new FileInfo(Path.Combine(directory.FullName, $"{fileName}.pem"));

            string base64 = Convert.ToBase64String(certificate.RawData);

            using (var stream = new StreamWriter(file.FullName))
            {
                stream.WriteLine("-----BEGIN CERTIFICATE-----");

                for (var i = 0; i < base64.Length; i += 64)
                {
                    int length = Math.Min(64, base64.Length - i);

                    stream.WriteLine(base64.Substring(i, length));
                }

                stream.Write("-----END CERTIFICATE-----");
            }

            return file;
        }

        private static void PrintHelp()
        {
            var file = new FileInfo(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine($"Syntax:  {file.Name} <package file path>");
        }
    }
}