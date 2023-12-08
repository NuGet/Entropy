// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;

namespace MakeTestCert
{
    internal static class Oids
    {
        internal static readonly Oid ClientAuthenticationEku = new(DottedDecimalValues.ClientAuthenticationEku);
        internal static readonly Oid CodeSigningEku = new(DottedDecimalValues.CodeSigningEku);
        internal static readonly Oid ServerAuthenticationEku = new(DottedDecimalValues.ServerAuthenticationEku);

        private static class DottedDecimalValues
        {
            internal const string ServerAuthenticationEku = "1.3.6.1.5.5.7.3.1";
            internal const string ClientAuthenticationEku = "1.3.6.1.5.5.7.3.2";
            internal const string CodeSigningEku = "1.3.6.1.5.5.7.3.3";
        }
    }
}