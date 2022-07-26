// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TrustTestCert
{
    internal static class CertificateBundleUtilities
    {
        internal static void AddCertificateToBundle(X509Certificate2 certificate, FileInfo certificateBundle)
        {
            ArgumentNullException.ThrowIfNull(certificate, nameof(certificate));
            ArgumentNullException.ThrowIfNull(certificateBundle, nameof(certificateBundle));

            X509Certificate2Collection certificates = new();

            certificates.ImportFromPemFile(certificateBundle.FullName);

            if (!certificates.Contains(certificate))
            {
                certificates.Add(certificate);

                WriteCertificateBundle(certificates, certificateBundle);
            }
        }

        internal static bool CertificateExistsInBundle(X509Certificate2 certificate, FileInfo certificateBundle)
        {
            ArgumentNullException.ThrowIfNull(certificate, nameof(certificate));
            ArgumentNullException.ThrowIfNull(certificateBundle, nameof(certificateBundle));

            X509Certificate2Collection certificates = new();

            certificates.ImportFromPemFile(certificateBundle.FullName);

            return certificates.Contains(certificate);
        }

        internal static void RemoveCertificateFromBundle(X509Certificate2 certificate, FileInfo certificateBundle)
        {
            ArgumentNullException.ThrowIfNull(certificate, nameof(certificate));
            ArgumentNullException.ThrowIfNull(certificateBundle, nameof(certificateBundle));

            X509Certificate2Collection certificates = new();

            certificates.ImportFromPemFile(certificateBundle.FullName);

            if (certificates.Contains(certificate))
            {
                certificates.Remove(certificate);

                WriteCertificateBundle(certificates, certificateBundle);
            }
        }

        private static void WriteCertificateBundle(X509Certificate2Collection certificates, FileInfo certificateBundle)
        {
            FileInfo file = new(Path.GetTempFileName());

            try
            {
                using (StreamWriter writer = new(file.FullName))
                {
                    foreach (X509Certificate2 certificate in certificates)
                    {
                        char[] pem = PemEncoding.Write("CERTIFICATE", certificate.RawData);

                        writer.WriteLine(pem);
                        writer.WriteLine();
                    }
                }

                BackupCertificateBundleIfNecessary(certificateBundle);

                File.Copy(file.FullName, certificateBundle.FullName, overwrite: true);
            }
            finally
            {
                file.Delete();
            }
        }

        private static void BackupCertificateBundleIfNecessary(FileInfo certificateBundle)
        {
            FileInfo backupCertificateBundle = new(Path.Combine(certificateBundle.DirectoryName!, $"{certificateBundle.Name}.backup"));

            if (!backupCertificateBundle.Exists)
            {
                File.Copy(certificateBundle.FullName, backupCertificateBundle.FullName);
            }
        }
    }
}