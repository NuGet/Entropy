using System.Security.Cryptography;

namespace MakeTestCert
{
    internal static class Oids
    {
        internal static readonly Oid CodeSigningEku = new(DottedDecimalValues.CodeSigningEku);

        private static class DottedDecimalValues
        {
            internal const string CodeSigningEku = "1.3.6.1.5.5.7.3.3";
        }
    }
}