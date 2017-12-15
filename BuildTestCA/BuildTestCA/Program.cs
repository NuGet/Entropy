using System;
using System.IO;

namespace BuildTestCA
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var runner = new OpenSslRunner(
                Path.Combine(currentDirectory, "output"),
                Path.Combine(currentDirectory, "openssl.exe"),
                Path.Combine(currentDirectory, "openssl.conf"),
                "http://fake-example.blob.core.windows.net/testca/",
                42000);

            /*
            runner.StartOcspResponder(
                CertificateId.IntermediateOcspSigner,
                CertificateId.IntermediateOcsp);
            */

            GenerateCertificates(runner);
        }

        private static void GenerateCertificates(OpenSslRunner runner)
        {
            if (Directory.Exists(runner.OutputDirectoryPath))
            {
                Directory.Delete(runner.OutputDirectoryPath, recursive: true);
            }

            runner.IssueCertificate(
                CertificateId.Root,
                CertificateId.Root,
                DateTimeOffset.UtcNow.AddYears(-1),
                DateTimeOffset.UtcNow.AddYears(20),
                null,
                CertificateExtension.RootCaCertificate);
            
            GenerateIntermediateWithOcsp(runner);
            GenerateIntermediateWithBrokenCrl(runner);
            GenerateNormalIntermediate(runner);
            GenerateRevokedIntermediate(runner);

            runner.IssueCrl(CertificateId.Root);
        }

        private static void GenerateIntermediateWithOcsp(OpenSslRunner runner)
        {
            runner.IssueCertificate(
                CertificateId.IntermediateOcsp,
                CertificateId.Root,
                DateTimeOffset.UtcNow.AddMonths(-1),
                DateTimeOffset.UtcNow.AddYears(11),
                null,
                CertificateExtension.IntermediateCaCertificate);

            runner.IssueCertificate(
                CertificateId.IntermediateOcspSigner,
                CertificateId.IntermediateOcsp,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                null,
                CertificateExtension.IntermediateOcspCertificate);

            runner.IssueCertificate(
                CertificateId.LeafWithOcsp,
                CertificateId.IntermediateOcsp,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafUsingOcspCertificate);

            runner.IssueCertificate(
                CertificateId.LeafWithOcspRevokedUnspecified,
                CertificateId.IntermediateOcsp,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafUsingOcspCertificate);
            runner.RevokeCertificate(
                CertificateId.LeafWithOcspRevokedUnspecified,
                CertificateId.IntermediateOcsp,
                RevocationReason.Unspecified,
                null);

            runner.IssueCrl(CertificateId.IntermediateOcsp, copyOnline: false);
        }

        private static void GenerateIntermediateWithBrokenCrl(OpenSslRunner runner)
        {
            runner.IssueCertificate(
                CertificateId.Intermediate404Crl,
                CertificateId.Root,
                DateTimeOffset.UtcNow.AddMonths(-1),
                DateTimeOffset.UtcNow.AddYears(11),
                null,
                CertificateExtension.IntermediateCaCertificate);

            runner.IssueCertificate(
                CertificateId.Leaf404Crl,
                CertificateId.Intermediate404Crl,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafBrokenCrlCertificate);

            runner.IssueCrl(CertificateId.Intermediate404Crl);
        }

        private static void GenerateNormalIntermediate(OpenSslRunner runner)
        {
            runner.IssueCertificate(
                CertificateId.Intermediate,
                CertificateId.Root,
                DateTimeOffset.UtcNow.AddMonths(-1),
                DateTimeOffset.UtcNow.AddYears(11),
                null,
                CertificateExtension.IntermediateCaCertificate);

            runner.IssueCertificate(
                CertificateId.Leaf1,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.Leaf2,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafWithNoEku,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                null,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafExpired,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow.AddHours(-2),
                DateTimeOffset.UtcNow.AddHours(-1),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafNotYetValid,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow.AddYears(9),
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.Leaf1024BitKeyLength,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate,
                keyLengthInBits: 1024);

            runner.IssueCertificate(
                CertificateId.LeafSha1,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate,
                signatureAlgorithm: SignatureAlgorithm.Sha1);

            runner.IssueCertificate(
                CertificateId.LeafNotCodeSigning,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.NotCodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafNoCrlDistributionPoint,
                CertificateId.Intermediate,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddYears(10),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafNoCrlCertificate);

            var allRevoked = new[]
            {
                new { Id = CertificateId.LeafRevokedUnspecified, Reason = RevocationReason.Unspecified, CompromiseTime = (DateTimeOffset?)null },
                new { Id = CertificateId.LeafRevokedKeyCompromise, Reason = RevocationReason.KeyCompromise, CompromiseTime = (DateTimeOffset?)DateTimeOffset.UtcNow },
                new { Id = CertificateId.LeafRevokedCaCompromise, Reason = RevocationReason.CaCompromise, CompromiseTime = (DateTimeOffset?)DateTimeOffset.UtcNow },
                new { Id = CertificateId.LeafRevokedAffiliationChanged, Reason = RevocationReason.AffiliationChanged, CompromiseTime = (DateTimeOffset?)null },
                new { Id = CertificateId.LeafRevokedSuperseded, Reason = RevocationReason.Superseded, CompromiseTime = (DateTimeOffset?)null },
                new { Id = CertificateId.LeafRevokedCessationOfOperation, Reason = RevocationReason.CessationOfOperation, CompromiseTime = (DateTimeOffset?)null },
                new { Id = CertificateId.LeafRevokedCertificateHold, Reason = RevocationReason.CertificateHold, CompromiseTime = (DateTimeOffset?)null },
                new { Id = CertificateId.LeafRevokedRemoveFromCrl, Reason = RevocationReason.RemoveFromCrl, CompromiseTime = (DateTimeOffset?)null },
            };

            foreach (var revoked in allRevoked)
            {
                runner.IssueCertificate(
                    revoked.Id,
                    CertificateId.Intermediate,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddYears(10),
                    CertificateRequestExtension.CodeSigning,
                    CertificateExtension.LeafCertificate);
                runner.RevokeCertificate(
                    revoked.Id,
                    CertificateId.Intermediate,
                    revoked.Reason,
                    revoked.CompromiseTime);
            }

            runner.IssueCrl(CertificateId.Intermediate);
        }

        private static void GenerateRevokedIntermediate(OpenSslRunner runner)
        {
            runner.IssueCertificate(
                CertificateId.IntermediateRevokedCaCompromise,
                CertificateId.Root,
                DateTimeOffset.UtcNow.AddMinutes(-2),
                DateTimeOffset.UtcNow.AddYears(11),
                null,
                CertificateExtension.IntermediateCaCertificate);

            runner.IssueCertificate(
                CertificateId.LeafBeforeIntermediateRevoked,
                CertificateId.IntermediateRevokedCaCompromise,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddHours(1),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafDuringIntermediateRevoked,
                CertificateId.IntermediateRevokedCaCompromise,
                DateTimeOffset.UtcNow.AddHours(1),
                DateTimeOffset.UtcNow.AddHours(3),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.IssueCertificate(
                CertificateId.LeafAfterIntermediateRevoked,
                CertificateId.IntermediateRevokedCaCompromise,
                DateTimeOffset.UtcNow.AddHours(3),
                DateTimeOffset.UtcNow.AddHours(4),
                CertificateRequestExtension.CodeSigning,
                CertificateExtension.LeafCertificate);

            runner.RevokeCertificate(
                CertificateId.IntermediateRevokedCaCompromise,
                CertificateId.Root,
                RevocationReason.CaCompromise,
                DateTimeOffset.UtcNow.AddHours(2));

            runner.IssueCrl(CertificateId.IntermediateRevokedCaCompromise);
        }
    }
}
