namespace BuildTestCA
{
    public static class RevocationReason
    {
        public const string Unspecified = "unspecified";
        public const string CaCompromise = "caCompromise";
        public const string KeyCompromise = "keyCompromise";
        public const string AffiliationChanged = "affiliationChanged";
        public const string Superseded = "superseded";
        public const string CessationOfOperation = "cessationOfOperation";
        public const string RemoveFromCrl = "removeFromCRL";
        public const string CertificateHold = "certificateHold";
    }
}
