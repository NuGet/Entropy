namespace BuildTestCA
{
    public static class CertificateId
    {
        public const string Root = "root";

        public const string Intermediate = "intermediate";
        public const string IntermediateOcsp = "intermediate-ocsp";

        public const string IntermediateOcspSigner = "intermediate-ocsp-signer";

        public const string Leaf1 = "leaf-1";
        public const string Leaf2 = "leaf-2";
        public const string LeafWithOcsp = "leaf-with-ocsp";
        public const string LeafWithNoEku = "leaf-with-no-eku";
        public const string LeafWithOcspRevokedUnspecified = "leaf-with-ocsp-revoked-" + RevocationReason.Unspecified;
        public const string LeafExpired = "leaf-expired";
        public const string LeafNotYetValid = "leaf-not-yet-valid";
        public const string Leaf1024BitKeyLength = "leaf-1024-bit-key-length";
        public const string LeafSha1 = "leaf-sha-1";
        public const string LeafNoCrlDistributionPoint = "leaf-no-crl-distribution-point";
        public const string Intermediate404Crl = "intermediate-404-crl";
        public const string Leaf404Crl = "leaf-404-crl";
        public const string LeafRevokedUnspecified = "leaf-revoked-" + RevocationReason.Unspecified;
        public const string LeafRevokedAffiliationChanged = "leaf-revoked-" + RevocationReason.AffiliationChanged;
        public const string LeafRevokedCaCompromise = "leaf-revoked-" + RevocationReason.CaCompromise;
        public const string LeafRevokedKeyCompromise = "leaf-revoked-" + RevocationReason.KeyCompromise;
        public const string LeafRevokedSuperseded = "leaf-revoked-" + RevocationReason.Superseded;
        public const string LeafRevokedCessationOfOperation = "leaf-revoked-" + RevocationReason.CessationOfOperation;
        public const string LeafRevokedCertificateHold = "leaf-revoked-" +  RevocationReason.CertificateHold;
        public const string LeafRevokedRemoveFromCrl = "leaf-revoked-" + RevocationReason.RemoveFromCrl;
        public const string IntermediateRevokedCaCompromise = "intermediate-revoked-" + RevocationReason.CaCompromise;
        public const string LeafBeforeIntermediateRevoked = "leaf-before-intermediate-revoked";
        public const string LeafDuringIntermediateRevoked = "leaf-during-intermediate-revoked";
        public const string LeafAfterIntermediateRevoked = "leaf-after-intermediate-revoked";
        public const string LeafNotCodeSigning = "leaf-not-code-signing";
    }
}
