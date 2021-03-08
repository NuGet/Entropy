// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Signing;

namespace RemoveSignaturesFromNuGetPackage
{
    internal static class Program
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

                var inputFile = new FileInfo(args[0]);

                await RemoveSignaturesAsync(inputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static DirectoryInfo CreateSubdirectory(DirectoryInfo packagesDirectory, Signatures signatures)
        {
            string subdirectoryName = GetConfiguration(signatures);

            return packagesDirectory.CreateSubdirectory(subdirectoryName);
        }

        private static async Task<Signatures> FindAllSignaturesAsync(FileInfo file)
        {
            PrimarySignature primarySignature;

            using (FileStream stream = file.OpenRead())
            using (var archive = new SignedPackageArchive(stream, Stream.Null))
            {
                primarySignature = await archive.GetPrimarySignatureAsync(CancellationToken.None);
            }

            if (primarySignature is null)
            {
                return Signatures.None;
            }

            Signatures signatures = Signatures.Primary;

            if (primarySignature.Timestamps.Count > 0)
            {
                signatures |= Signatures.PrimaryTimestamp;
            }

            RepositoryCountersignature repositoryCountersignature = RepositoryCountersignature.GetRepositoryCountersignature(primarySignature);

            if (repositoryCountersignature is object)
            {
                signatures |= Signatures.Countersignature;

                if (repositoryCountersignature.Timestamps.Count > 0)
                {
                    signatures |= Signatures.CountersignatureTimestamp;
                }
            }

            return signatures;
        }

        private static string GetConfiguration(Signatures signatures)
        {
            if (signatures == Signatures.None)
            {
                return "none";
            }

            var builder = new StringBuilder();

            if (signatures.HasFlag(Signatures.Primary))
            {
                builder.Append("primary");
            }

            if (signatures.HasFlag(Signatures.PrimaryTimestamp))
            {
                builder.Append("+timestamp");
            }

            if (signatures.HasFlag(Signatures.Countersignature))
            {
                builder.Append("+countersignature");
            }

            if (signatures.HasFlag(Signatures.CountersignatureTimestamp))
            {
                builder.Append("+timestamp");
            }

            return builder.ToString();
        }

        private static void PrintHelp()
        {
            var file = new FileInfo(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine($"Syntax:  {file.Name} <package file path>");
        }

        private static void RemoveCountersignature(SignedCms signedCms)
        {
            SignerInfo primarySigner = signedCms.SignerInfos[0];

            for (var i = 0; i < primarySigner.CounterSignerInfos.Count; ++i)
            {
                SignerInfo counterSignerInfo = primarySigner.CounterSignerInfos[i];

                SignatureType countersignatureType = AttributeUtility.GetSignatureType(counterSignerInfo.SignedAttributes);

                if (countersignatureType == SignatureType.Repository)
                {
                    primarySigner.RemoveCounterSignature(i);
                    break;
                }
            }
        }

        private static void RemoveCountersignatureTimestamp(SignedCms signedCms)
        {
            SignerInfo primarySigner = signedCms.SignerInfos[0];

            for (var i = 0; i < primarySigner.CounterSignerInfos.Count; ++i)
            {
                SignerInfo counterSignerInfo = primarySigner.CounterSignerInfos[i];

                SignatureType countersignatureType = AttributeUtility.GetSignatureType(counterSignerInfo.SignedAttributes);

                if (countersignatureType == SignatureType.Repository)
                {
                    RemoveTimestamp(counterSignerInfo);
                    break;
                }
            }
        }

        private static void RemovePrimaryTimestamp(SignedCms signedCms)
        {
            SignerInfo primarySigner = signedCms.SignerInfos[0];

            RemoveTimestamp(primarySigner);
        }

        private static void RemovePrimaryTimestampAndCountersignatureTimestamp(SignedCms signedCms)
        {
            RemovePrimaryTimestamp(signedCms);
            RemoveCountersignatureTimestamp(signedCms);
        }

        private static async Task<FileInfo> RemoveSignatureAsync(
            FileInfo inputFile,
            DirectoryInfo packageDirectory,
            Action<SignedCms> remove,
            bool isPrimary)
        {
            var outputFile = new FileInfo(Path.Combine(packageDirectory.FullName, inputFile.Name));
            var tempPackageFile = new FileInfo(Path.GetTempFileName());

            try
            {
                using (FileStream unsignedPackageStream = tempPackageFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    PrimarySignature primarySignature;

                    using (FileStream inputStream = inputFile.OpenRead())
                    using (var package = new SignedPackageArchive(inputStream, unsignedPackageStream))
                    {
                        primarySignature = await package.GetPrimarySignatureAsync(CancellationToken.None);

                        await package.RemoveSignatureAsync(CancellationToken.None);
                    }

                    SignedCms signedCms = primarySignature.SignedCms;

                    remove(signedCms);

                    if (isPrimary)
                    {
                        unsignedPackageStream.Flush();
                        unsignedPackageStream.Close();

                        tempPackageFile.CopyTo(outputFile.FullName, overwrite: true);
                    }
                    else
                    {
                        using (FileStream outputStream = outputFile.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        using (var package = new SignedPackageArchive(unsignedPackageStream, outputStream))
                        using (var signatureStream = new MemoryStream(signedCms.Encode()))
                        {
                            await package.AddSignatureAsync(signatureStream, CancellationToken.None);
                        }
                    }
                }
            }
            finally
            {
                tempPackageFile.Refresh();

                if (tempPackageFile.Exists)
                {
                    tempPackageFile.Delete();
                }
            }

            return outputFile;
        }

        private static async Task RemoveSignaturesAsync(FileInfo inputFile)
        {
            Signatures signatures = await FindAllSignaturesAsync(inputFile);

            if (signatures == Signatures.None)
            {
                Console.WriteLine("The package is not signed.");

                return;
            }

            string packagesDirectoryPath = Path.Combine(
                inputFile.Directory.FullName,
                Path.GetFileNameWithoutExtension(inputFile.Name));
            var packagesDirectory = new DirectoryInfo(packagesDirectoryPath);

            packagesDirectory.Create();

            Console.WriteLine($"Packages written to {packagesDirectory.FullName}");
            Console.WriteLine();

            DirectoryInfo packageDirectory = packagesDirectory.CreateSubdirectory(GetConfiguration(signatures));
            FileInfo file = inputFile;

            file.CopyTo(Path.Combine(packageDirectory.FullName, file.Name), overwrite: true);

            if (signatures.HasFlag(Signatures.CountersignatureTimestamp))
            {
                if (signatures.HasFlag(Signatures.PrimaryTimestamp))
                {
                    packageDirectory = CreateSubdirectory(packagesDirectory, signatures & ~Signatures.PrimaryTimestamp);

                    _ = await RemoveSignatureAsync(inputFile, packageDirectory, RemovePrimaryTimestamp, isPrimary: false);
                }

                signatures &= ~Signatures.CountersignatureTimestamp;

                packageDirectory = CreateSubdirectory(packagesDirectory, signatures);

                file = await RemoveSignatureAsync(file, packageDirectory, RemoveCountersignatureTimestamp, isPrimary: false);
            }

            if (signatures.HasFlag(Signatures.Countersignature))
            {
                if (signatures.HasFlag(Signatures.PrimaryTimestamp))
                {
                    packageDirectory = CreateSubdirectory(packagesDirectory, signatures & ~Signatures.PrimaryTimestamp);

                    _ = await RemoveSignatureAsync(inputFile, packageDirectory, RemovePrimaryTimestampAndCountersignatureTimestamp, isPrimary: false);
                }

                signatures &= ~Signatures.Countersignature;

                packageDirectory = CreateSubdirectory(packagesDirectory, signatures);

                file = await RemoveSignatureAsync(file, packageDirectory, RemoveCountersignature, isPrimary: false);
            }

            if (signatures.HasFlag(Signatures.PrimaryTimestamp))
            {
                signatures &= ~Signatures.PrimaryTimestamp;

                packageDirectory = CreateSubdirectory(packagesDirectory, signatures);

                file = await RemoveSignatureAsync(file, packageDirectory, RemovePrimaryTimestamp, isPrimary: false);
            }

            signatures &= ~Signatures.Primary;

            packageDirectory = CreateSubdirectory(packagesDirectory, signatures);

            _ = await RemoveSignatureAsync(inputFile, packageDirectory, _ => { }, isPrimary: true);

            Console.WriteLine("  Relative File Path");
            Console.WriteLine("  ------------------------------------------------------------------------------------------");

            FileInfo[] packageFiles = packagesDirectory.GetFiles("*.nupkg", SearchOption.AllDirectories);

            foreach (FileInfo packageFile in packageFiles)
            {
                string filePath = packageFile.FullName.Substring(packagesDirectory.FullName.Length);

                Console.WriteLine($"  .{filePath}");
            }
        }

        private static void RemoveTimestamp(SignerInfo signerInfo)
        {
            var attributesToRemove = new List<AsnEncodedData>();

            foreach (CryptographicAttributeObject attribute in signerInfo.UnsignedAttributes)
            {
                if (string.Equals(attribute.Oid.Value, Oids.SignatureTimeStampTokenAttribute, StringComparison.Ordinal))
                {
                    foreach (AsnEncodedData value in attribute.Values)
                    {
                        attributesToRemove.Add(value);
                    }
                }
            }

            foreach (AsnEncodedData attributeToRemove in attributesToRemove)
            {
                signerInfo.RemoveUnsignedAttribute(attributeToRemove);
            }
        }
    }
}
