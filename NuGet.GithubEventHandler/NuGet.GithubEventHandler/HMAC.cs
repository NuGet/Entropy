using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NuGet.GithubEventHandler
{
    public static class HMAC
    {
        public static bool Validate(byte[] signature, Stream body, string secretName, IEnvironment env)
        {
            string? secretString = env.Get("WEBHOOK_SECRET_" + secretName);
            byte[] secret = string.IsNullOrEmpty(secretString) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(secretString);
            var result = Validate(signature, body, secret);
            return secret.Length == 0 ? false : result;
        }

        private static bool Validate(byte[] signature, Stream body, byte[] secret)
        {
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (body == null) { throw new ArgumentNullException(nameof(body)); }
            if (secret == null) { throw new ArgumentNullException(nameof(secret)); }

            if (signature.Length != 32) { throw new ArgumentException(paramName: nameof(signature), message: "Signature must be 32 bytes"); }

            byte[] computedHash;
            using (var hmac = new HMACSHA256(secret))
            {
                computedHash = hmac.ComputeHash(body);
            }

            Debug.Assert(signature.Length == computedHash.Length);

            bool result = ConstantTimeCompare(signature, computedHash);
            return result;
        }

        private static bool ConstantTimeCompare(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) { throw new ArgumentException(message: "Length of parameters do not match"); }

            // Do not use conditional, especially avoid early exit.
            int correctBytes = 0;
            for (int i = 0; i < a.Length; i++)
            {
                correctBytes += a[i] == b[i] ? 1 : 0;
            }

            return correctBytes == a.Length;
        }
    }
}
