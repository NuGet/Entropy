// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;

namespace TrustTestCert
{
    internal static class Program
    {
        private const int Success = 0;
        private const int Error = 1;

        private static StoreLocation _certificateStoreLocation = StoreLocation.CurrentUser;

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
                    || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase))
                || args.Length < 3)
            {
                PrintHelp();

                return Success;
            }

            string action = args[0].ToLowerInvariant();

            switch (action)
            {
                case "add":
                case "remove":
                    break;

                default:
                    PrintHelp();

                    return Error;
            }

            X509Certificate2? certificate = null;
            DirectoryInfo? versionedSdkDirectory = null;

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            int argsCount = args.Length;
            bool hasError = false;

            for (var i = 1; !hasError && i < argsCount - 1; i += 2)
            {
                string arg = args[i].ToLowerInvariant();
                string nextArg = args[i + 1];

                switch (arg)
                {
                    case "-c":
                    case "--certificate":
                        certificate = new X509Certificate2(nextArg);
                        break;

                    case "-csl":
                    case "--certificate-store-location":
                        if (!isWindows)
                        {
                            hasError = true;
                            break;
                        }

                        _certificateStoreLocation = Enum.Parse<StoreLocation>(nextArg, ignoreCase: true);
                        break;

                    case "-vsd":
                    case "--versioned-sdk-directory":
                        if (isWindows)
                        {
                            hasError = true;
                            break;
                        }

                        versionedSdkDirectory = new DirectoryInfo(nextArg);
                        break;

                    default:
                        PrintHelp();

                        return Error;
                }
            }

            if (hasError || certificate is null)
            {
                PrintHelp();

                return Error;
            }

            FileInfo? certificateBundle = null;

            if (versionedSdkDirectory is not null)
            {
                certificateBundle = new(Path.Combine(versionedSdkDirectory.FullName, "trustedroots", "codesignctl.pem"));

                if (!certificateBundle.Exists)
                {
                    Console.Error.WriteLine($"The .NET SDK at {versionedSdkDirectory.FullName} does not have a fallback certificate bundle.");
                    Console.Error.WriteLine($"Only .NET 6.0.400 SDK and later is supported.");

                    return Error;
                }
            }

            switch (action)
            {
                case "add":
                    {
                        if (isWindows)
                        {
                            return AddTrust(certificate, _certificateStoreLocation);
                        }
                        else
                        {
                            return AddTrust(certificate, certificateBundle!);
                        }
                    }

                case "remove":
                    {
                        if (isWindows)
                        {
                            return RemoveTrust(certificate, _certificateStoreLocation);
                        }
                        else
                        {
                            return RemoveTrust(certificate, certificateBundle!);
                        }
                    }
            }

            return Error;
        }

        private static int AddTrust(X509Certificate2 certificate, StoreLocation storeLocation)
        {
            bool alreadyExists;

            using (X509Store store = new(StoreName.Root, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                alreadyExists = store.Certificates.Contains(certificate);

                if (!alreadyExists)
                {
                    store.Add(certificate);
                }
            }

            string storeLocationFriendlyName = GetFriendlyName(storeLocation);

            if (alreadyExists)
            {
                Console.WriteLine($"The certificate already exists in the {storeLocationFriendlyName}'s root store.");
            }
            else
            {
                using (X509Store store = new(StoreName.Root, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);

                    if (store.Certificates.Contains(certificate))
                    {
                        Console.WriteLine($"The certificate was added to the {storeLocationFriendlyName}'s root store.");
                    }
                    else
                    {
                        Console.Error.WriteLine($"The certificate was not added to the {storeLocationFriendlyName}'s root store.");

                        return Error;
                    }
                }
            }

            return Success;
        }

        private static int AddTrust(X509Certificate2 certificate, FileInfo certificateBundle)
        {
            bool alreadyExists = CertificateBundleUtilities.CertificateExistsInBundle(certificate, certificateBundle);

            if (alreadyExists)
            {
                Console.WriteLine($"The certificate already exists in {certificateBundle.FullName}.");
            }
            else
            {
                CertificateBundleUtilities.AddCertificateToBundle(certificate, certificateBundle);

                if (CertificateBundleUtilities.CertificateExistsInBundle(certificate, certificateBundle))
                {
                    Console.WriteLine($"The certificate was added to {certificateBundle.FullName}.");
                }
                else
                {
                    Console.Error.WriteLine($"The certificate was not added to {certificateBundle.FullName}.");

                    return Error;
                }
            }

            return Success;
        }

        private static int RemoveTrust(X509Certificate2 certificate, StoreLocation storeLocation)
        {
            bool alreadyExists;

            using (X509Store store = new(StoreName.Root, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                alreadyExists = store.Certificates.Contains(certificate);

                if (alreadyExists)
                {
                    store.Remove(certificate);
                }
            }

            string storeLocationFriendlyName = GetFriendlyName(storeLocation);

            if (alreadyExists)
            {
                using (X509Store store = new(StoreName.Root, storeLocation))
                {
                    store.Open(OpenFlags.ReadOnly);

                    if (store.Certificates.Contains(certificate))
                    {
                        Console.Error.WriteLine($"The certificate was not removed from the {storeLocationFriendlyName}'s root store.");

                        return Error;
                    }

                    Console.WriteLine($"The certificate was removed from the {storeLocationFriendlyName}'s root store.");
                }
            }
            else
            {
                Console.WriteLine($"The certificate was not present in the {storeLocationFriendlyName}'s root store.");
            }

            return Success;
        }

        private static int RemoveTrust(X509Certificate2 certificate, FileInfo certificateBundle)
        {
            bool alreadyExists = CertificateBundleUtilities.CertificateExistsInBundle(certificate, certificateBundle);

            if (alreadyExists)
            {
                CertificateBundleUtilities.RemoveCertificateFromBundle(certificate, certificateBundle);

                if (CertificateBundleUtilities.CertificateExistsInBundle(certificate, certificateBundle))
                {
                    Console.Error.WriteLine($"The certificate was not removed from {certificateBundle.FullName}.");

                    return Error;
                }

                Console.WriteLine($"The certificate was removed from {certificateBundle.FullName}.");
            }
            else
            {
                Console.WriteLine($"The certificate was not present in {certificateBundle.FullName}.");
            }

            return Success;
        }

        private static string GetFriendlyName(StoreLocation storeLocation)
        {
            return storeLocation switch
            {
                StoreLocation.CurrentUser => "current user",
                StoreLocation.LocalMachine => "local machine",
                _ => throw new ArgumentException(message: null, nameof(storeLocation))
            };
        }

        private static void PrintHelp()
        {
            FileInfo file = new(Environment.ProcessPath!);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
            string storeLocationFriendlyName = GetFriendlyName(_certificateStoreLocation);

            Console.WriteLine($"Usage:  {file.Name} <add|remove> --certificate <CertificateFilePath> [option]");
            Console.WriteLine();
            Console.WriteLine("  Option                              Description");
            Console.WriteLine("  ----------------------------------- -------------------------------------------");
            Console.WriteLine("  --certificate-store-location, -csl  The certificate store location, either");
            Console.WriteLine("                                      CurrentUser or LocalMachine.");
            Console.WriteLine("                                      On Windows, the default is CurrentUser,");
            Console.WriteLine("                                      and LocalMachine requires elevation.");
            Console.WriteLine("                                      On Linux/macOS, this option is never used.");
            Console.WriteLine("  --versioned-sdk-directory, -vsd     The versioned .NET SDK root directory that ");
            Console.WriteLine($"                                      contains trustedroots{Path.DirectorySeparatorChar}codesignctl.pem.");
            Console.WriteLine("                                      On Windows, this option is never used.");
            Console.WriteLine("                                      On Linux/macOS, this option is required.");
            Console.WriteLine("Examples:");
            Console.WriteLine();
            Console.WriteLine("  Windows:");
            Console.WriteLine($"    {fileNameWithoutExtension}.exe add -c .\\test.cer");
            Console.WriteLine($"      Adds the certificate to the {storeLocationFriendlyName}'s root store.");
            Console.WriteLine();
            Console.WriteLine($"    {fileNameWithoutExtension}.exe add -c .\\test.pfx");
            Console.WriteLine($"      Adds the certificate and its private key to the {storeLocationFriendlyName}'s root store.");
            Console.WriteLine();
            Console.WriteLine($"    {fileNameWithoutExtension}.exe add -c .\\test.cer -csl LocalMachine");
            Console.WriteLine($"      Adds the certificate to the {GetFriendlyName(StoreLocation.LocalMachine)}'s root store.");
            Console.WriteLine();
            Console.WriteLine($"    {fileNameWithoutExtension}.exe remove -c .\\test.cer");
            Console.WriteLine($"      Removes the certificate from the {storeLocationFriendlyName}'s root store.");
            Console.WriteLine();
            Console.WriteLine("  Linux/macOS:");
            Console.WriteLine($"    {fileNameWithoutExtension} add -c ./test.pem -vsd ~/dotnet/sdk/7.0.100");
            Console.WriteLine("      Adds the certificate to the specified .NET SDK's fallback certificate ");
            Console.WriteLine("      bundle.");
            Console.WriteLine();
            Console.WriteLine($"    {fileNameWithoutExtension} remove -c ./test.pem -vsd ~/dotnet/sdk/7.0.100");
            Console.WriteLine("      Removes the certificate from the specified .NET SDK's fallback ");
            Console.WriteLine("      certificate bundle.");
        }
    }
}