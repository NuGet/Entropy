namespace BuildTestCA
{
    public static class CertificateExtension
    {
        public const string RootCaCertificate = "root_ca_certificate";
        public const string IntermediateCaCertificate = "intermediate_ca_certificate";
        public const string IntermediateOcspCertificate = "intermediate_ocsp_certificate";
        public const string LeafCertificate = "leaf_certificate";
        public const string LeafUsingOcspCertificate = "leaf_using_ocsp_certificate";
        public const string LeafNoCrlCertificate = "leaf_no_crl_certificate";
        public const string LeafBrokenCrlCertificate = "leaf_broken_crl_certificate";
    }
}
