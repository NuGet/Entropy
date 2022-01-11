// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
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

                var context = new Context();

                ExtractCertificates(primarySignature, certificatesDirectory, context);

                if (primarySignature.Type == SignatureType.Repository)
                {
                    return;
                }

                RepositoryCountersignature repositoryCountersignature = RepositoryCountersignature.GetRepositoryCountersignature(primarySignature);

                if (repositoryCountersignature is null)
                {
                    return;
                }

                ExtractCertificates(primarySignature, repositoryCountersignature, certificatesDirectory, context);

                WriteCertificates(
                    "primary signature's",
                    context.PrimarySignedCms,
                    certificatesDirectory,
                    certificatesDirectory,
                    context.UsedCertificateHashes);

                if (context.PrimaryTimestampSignedCms is object)
                {
                    WriteCertificates(
                        "primary timestamp signature's",
                        context.PrimaryTimestampSignedCms,
                        certificatesDirectory,
                        context.PrimaryTimestampDirectory,
                        context.UsedCertificateHashes);
                }

                if (context.CountersignatureTimestampSignedCms is object)
                {
                    WriteCertificates(
                        "repository countersignature timestamp signature's",
                        context.CountersignatureTimestampSignedCms,
                        certificatesDirectory,
                        context.CountersignatureTimestampDirectory,
                        context.UsedCertificateHashes);
                }
            }
        }

        private static void ExtractCertificates(
            PrimarySignature primarySignature,
            DirectoryInfo certificatesDirectory,
            Context context)
        {
            using (IX509CertificateChain primaryCertificates = SignatureUtility.GetCertificateChain(primarySignature))
            {
                AddCertificates(context, primaryCertificates);

                WriteCertificates(
                    "primary signature's",
                    certificatesDirectory,
                    certificatesDirectory,
                    primaryCertificates);

                SignedCms signedCms = primarySignature.SignedCms;
                SignerInfo primarySignerInfo = signedCms.SignerInfos[0];

                context.PrimarySignedCms = signedCms;

                CheckSignatureValidity(primarySignerInfo, "primary");
                ReportCertificatesNotInSignedCms(primaryCertificates, signedCms);

                Timestamp timestamp = primarySignature.Timestamps.FirstOrDefault();

                if (timestamp is object)
                {
                    using (IX509CertificateChain timestampCertificates = SignatureUtility.GetTimestampCertificateChain(primarySignature))
                    {
                        AddCertificates(context, timestampCertificates);

                        DirectoryInfo timestampDirectory = certificatesDirectory.CreateSubdirectory("timestamp");

                        context.PrimaryTimestampDirectory = timestampDirectory;

                        WriteCertificates(
                            "primary timestamp signature's",
                            certificatesDirectory,
                            timestampDirectory,
                            timestampCertificates);

                        SignedCms timestampSignedCms = timestamp.SignedCms;
                        SignerInfo timestampSignerInfo = timestampSignedCms.SignerInfos[0];

                        context.PrimaryTimestampSignedCms = timestampSignedCms;

                        CheckSignatureValidity(timestampSignerInfo, "primary timestamp");
                        ReportCertificatesNotInSignedCms(timestampCertificates, timestampSignedCms);
                    }
                }
            }
        }

        private static void ExtractCertificates(
            PrimarySignature primarySignature,
            RepositoryCountersignature repositoryCountersignature,
            DirectoryInfo certificatesDirectory,
            Context context)
        {
            using (IX509CertificateChain countersignatureCertificates = SignatureUtility.GetCertificateChain(primarySignature, repositoryCountersignature))
            {
                AddCertificates(context, countersignatureCertificates);

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
                        AddCertificates(context, timestampCertificates);

                        DirectoryInfo timestampDirectory = countersignatureDirectory.CreateSubdirectory("timestamp");

                        context.CountersignatureTimestampDirectory = timestampDirectory;

                        WriteCertificates(
                            "repository countersignature timestamp signature's",
                            certificatesDirectory,
                            timestampDirectory,
                            timestampCertificates);

                        SignedCms timestampSignedCms = timestamp.SignedCms;

                        context.CountersignatureTimestampSignedCms = timestampSignedCms;

                        CheckSignatureValidity(timestamp.SignerInfo, "repository countersignature timestamp");
                        ReportCertificatesNotInSignedCms(timestampCertificates, timestampSignedCms);
                    }
                }
            }
        }

        private static void AddCertificates(Context context, IX509CertificateChain certificates)
        {
            foreach (X509Certificate2 certificate in certificates)
            {
                string hash = GetCertificateHashString(certificate);

                context.UsedCertificateHashes.Add(hash);
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

        private static string GetCertificateHashString(X509Certificate2 certificate)
        {
            return certificate.GetCertHashString(HashAlgorithmName.SHA256);
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
                    Console.Error.WriteLine($"  Subject:              {certificate.Subject}");
                    Console.Error.WriteLine($"  Issuer:               {certificate.Issuer}");
                    Console.Error.WriteLine($"  Fingerprint (SHA-1):  {certificate.Thumbprint.ToLowerInvariant()}");
                    Console.Error.WriteLine();
                }
            }
        }

        private static List<X509Certificate2> CreateSortedList(X509Certificate2Collection certificates)
        {
            var unsortedCertificates = new List<X509Certificate2>(capacity: certificates.Count);

            foreach (X509Certificate2 certificate in certificates)
            {
                unsortedCertificates.Add(certificate);
            }

            return unsortedCertificates.OrderBy(c => c.Thumbprint).ToList();
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
            Console.WriteLine("  ----------------------------------------- ------ ---------------------------------------");

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

        private static void WriteCertificates(
            string name,
            SignedCms signedCms,
            DirectoryInfo rootDirectory,
            DirectoryInfo parentDirectory,
            HashSet<string> usedCertificateHashes)
        {
            Console.WriteLine($"The {name} CMS certificates collection:");
            Console.WriteLine();
            Console.WriteLine("  Fingerprint (SHA-1)                       Used   Relative File Path");
            Console.WriteLine("  ----------------------------------------- ------ ---------------------------------------");

            DirectoryInfo certificatesDirectory = parentDirectory.CreateSubdirectory("cms");

            List<X509Certificate2> certificates = CreateSortedList(signedCms.Certificates);

            for (int i = 0, iend = certificates.Count - 1; i <= iend; ++i)
            {
                X509Certificate2 certificate = certificates[i];
                string fileName = $"{i}";
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

                string hash = GetCertificateHashString(certificate);
                string status = usedCertificateHashes.Contains(hash) ? "yes" : "no";

                Console.WriteLine($"  {certificate.Thumbprint.ToLowerInvariant()}  {status,-5}  .{filePath}");
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

        private sealed class Context
        {
            internal HashSet<string> UsedCertificateHashes { get; } = new HashSet<string>();
            internal SignedCms PrimarySignedCms { get; set; }
            internal SignedCms PrimaryTimestampSignedCms { get; set; }
            internal SignedCms CountersignatureTimestampSignedCms { get; set; }
            internal DirectoryInfo PrimaryTimestampDirectory { get; set; }
            internal DirectoryInfo CountersignatureTimestampDirectory { get; set; }
        }
    }
}