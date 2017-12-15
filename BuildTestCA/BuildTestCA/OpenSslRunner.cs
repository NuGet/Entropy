using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildTestCA
{
    public class OpenSslRunner : IDisposable
    {
        private readonly string _newCertsBaseDirectoryPath;
        private readonly string _executablePath;
        private readonly string _configFilePath;
        private readonly string _caBaseUrl;
        private readonly int _ocspPort;
        private Process _oscpResponderProcess;

        public OpenSslRunner(
            string outputDirectoryPath,
            string executablePath,
            string configFilePath,
            string caBaseUrl,
            int ocspPort)
        {
            OutputDirectoryPath = outputDirectoryPath;
            _newCertsBaseDirectoryPath = Path.Combine(OutputDirectoryPath, "certs");
            OnlineDirectoryPath = Path.Combine(OutputDirectoryPath, "online");
            _executablePath = executablePath;
            _configFilePath = configFilePath;
            _caBaseUrl = caBaseUrl;
            _ocspPort = ocspPort;
        }

        public string OutputDirectoryPath { get; }
        public string OnlineDirectoryPath { get; }

        public void StartOcspResponder(string signerId, string issuerId)
        {
            if (_oscpResponderProcess != null)
            {
                throw new InvalidOperationException("The OCSP responder is already runner.");
            }

            var logFilePath = Path.Combine(OutputDirectoryPath, "ocsp-responder.log");

            Console.WriteLine("Starting OCSP responder...");

            var context = new StartOcspResponderContext(signerId, issuerId, logFilePath, this);

            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            var arguments = new List<string>
            {
                "ocsp",
                "-index", context.IssuerDatabaseFilePath,
                "-port", context.OcspPort.ToString(),
                "-rsigner", context.CertificatePemFilePath,
                "-rkey", context.PrivateKeyFilePath,
                "-CA", context.IssuerCertificatePemFilePath,
                "-text",
                "-out", context.LogFilePath,
                "-ignore_err"
            };

            var result = Execute(context, arguments.ToArray(), waitForExit: false, shareProcessObject: true);
            _oscpResponderProcess = result.Process;
        }

        public void StopOcspResponder()
        {
            if (_oscpResponderProcess == null)
            {
                return;
            }

            _oscpResponderProcess.Kill();
        }

        public void IssueCertificate(
            string id,
            string issuerId,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            string requestExtensions,
            string extensions,
            int keyLengthInBits = 2048,
            string signatureAlgorithm = SignatureAlgorithm.Sha256)
        {
            Console.Write($"Issuing certificate {id}...");

            var context = new IssueCertificateContext(
                id,
                issuerId,
                requestExtensions,
                extensions,
                keyLengthInBits,
                startDate,
                endDate,
                signatureAlgorithm,
                this);

            Directory.CreateDirectory(OnlineDirectoryPath);
            Directory.CreateDirectory(context.NewCertsDirectoryPath);

            GeneratePrivateKey(context);
            CreateCertificateSigningRequest(context);
            FulfillCertificateSigningRequest(context);
            GeneratePfx(context);
            GenerateX509(context);

            Console.WriteLine(" done.");
        }

        public void RevokeCertificate(
            string id,
            string issuerId,
            string crlReason,
            DateTimeOffset? compromiseTime)
        {
            Console.Write($"Revoking certificate {id}...");

            var context = new RevokeCertificateContext(
                id,
                issuerId,
                crlReason,
                compromiseTime,
                this);

            RevokeCertificate(context);

            Console.WriteLine(" done.");
        }

        public void IssueCrl(string id, bool copyOnline = true)
        {
            Console.Write($"Issuing CRL for certificate {id}...");

            var context = new IssueCrlContext(id, this);

            GenerateCrl(context);
            ConvertCrl(context, copyOnline);

            Console.WriteLine(" done.");
        }

        private void GenerateCrl(IssueCrlContext context)
        {
            var arguments = new List<string>
            {
                "ca",
                "-gencrl",
                "-out", context.IssuerCrlPemFilePath,
                "-config", context.ConfigFilePath,
            };

            Execute(context, arguments.ToArray());
        }

        private void ConvertCrl(IssueCrlContext context, bool copyOnline)
        {
            var arguments = new List<string>
            {
                "crl",
                "-in", context.IssuerCrlPemFilePath,
                "-inform", "pem",
                "-out", context.IssuerCrlFilePath,
                "-outform", "der",
            };

            Execute(context, arguments.ToArray());

            if (copyOnline)
            {
                File.Copy(
                   context.IssuerCrlFilePath,
                   Path.Combine(OnlineDirectoryPath, Path.GetFileName(context.IssuerCrlFilePath)));
            }
        }

        private void RevokeCertificate(RevokeCertificateContext context)
        {
            if (context.CrlReason == RevocationReason.CaCompromise)
            {
                var arguments = new List<string>
                {
                    "ca",
                    "-revoke", context.CertificatePemFilePath,
                    "-crl_CA_compromise", FormatDate(context.CompromiseTime.Value),
                    "-config", context.ConfigFilePath,
                };

                Execute(context, arguments.ToArray());
            }
            else if (context.CrlReason == RevocationReason.KeyCompromise)
            {
                var arguments = new List<string>
                {
                    "ca",
                    "-revoke", context.CertificatePemFilePath,
                    "-crl_compromise", FormatDate(context.CompromiseTime.Value),
                    "-config", context.ConfigFilePath,
                };

                Execute(context, arguments.ToArray());
            }
            else
            {
                var arguments = new List<string>
                {
                    "ca",
                    "-revoke", context.CertificatePemFilePath,
                    "-crl_reason", context.CrlReason,
                    "-config", context.ConfigFilePath,
                };

                Execute(context, arguments.ToArray());
            }
        }

        private void GeneratePfx(IssueCertificateContext context)
        {
            var arguments = new List<string>
            {
                "pkcs12",
                "-export",
                "-in", context.CertificatePemFilePath,
                "-inkey", context.PrivateKeyFilePath,
                "-out", context.CertificatePfxFilePath,
                "-passout", "pass:",
            };

            Execute(context, arguments.ToArray());
        }

        private void GenerateX509(IssueCertificateContext context)
        {
            var arguments = new List<string>
            {
                "x509",
                "-in", context.CertificatePemFilePath,
                "-out", context.CertificateCrtFilePath,
                "-outform", "der",
            };

            Execute(context, arguments.ToArray());

            File.Copy(
                context.CertificateCrtFilePath,
                Path.Combine(OnlineDirectoryPath, Path.GetFileName(context.CertificateCrtFilePath)));
        }

        private void FulfillCertificateSigningRequest(IssueCertificateContext context)
        {
            var arguments = new List<string>
            {
                "ca",
                "-batch",
                "-in", context.CertificateRequestFilePath,
                "-out", context.CertificatePemFilePath,
                "-startdate", FormatDate(context.StartDate),
                "-enddate", FormatDate(context.EndDate),
                "-md", context.SignatureAlgorithm,
                "-extensions", context.Extensions,
                "-extfile", context.ConfigFilePath,
                "-config", context.ConfigFilePath,
                "-notext",
            };

            if (context.Id == context.IssuerId)
            {
                arguments.Add("-selfsign");
            }

            Execute(context, arguments.ToArray());
        }
        
        private void GeneratePrivateKey(IssueCertificateContext context)
        {
            File.Delete(context.PrivateKeyFilePath);

            var arguments = new List<string>
            {
                "genrsa",
                "-out", context.PrivateKeyFilePath,
                context.KeyLengthInBits.ToString(),
            };

            Execute(context, arguments.ToArray());
        }

        private void CreateCertificateSigningRequest(IssueCertificateContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.Extensions) && context.Extensions.EndsWith("ca_certificate"))
            {
                File.WriteAllText(Path.Combine(OutputDirectoryPath, $"{context.Id}.database"), string.Empty);
                File.WriteAllText(Path.Combine(OutputDirectoryPath, $"{context.Id}.database.attr"), string.Empty);
                File.WriteAllText(Path.Combine(OutputDirectoryPath, $"{context.Id}.crlnumber"), "1000");
                File.WriteAllText(Path.Combine(OutputDirectoryPath, $"{context.Id}.serialnumber"), "01");
            }

            var arguments = new List<string>
            {
                "req",
                "-new",
                "-key", context.PrivateKeyFilePath,
                "-out", context.CertificateRequestFilePath,
                "-config", context.ConfigFilePath,
            };

            if (!string.IsNullOrWhiteSpace(context.RequestExtensions))
            {
                arguments.Add("-reqexts");
                arguments.Add(context.RequestExtensions);
            }

            Execute(context, arguments.ToArray());
        }

        private string FormatDate(DateTimeOffset input)
        {
            return input.ToUniversalTime().ToString("yyyyMMddHHmmssZ");
        }

        private CommandRunnerResult Execute(
            OpenSslContext context,
            IReadOnlyList<string> arguments,
            bool waitForExit = true,
            bool shareProcessObject = false)
        {
            var encodedArguments = new StringBuilder();

            foreach (var argument in arguments)
            {
                encodedArguments.Append(EncodeParameterArgument(argument.ToString()));
                encodedArguments.Append(' ');
            }

            var result = CommandRunner.Run(
                _executablePath,
                OutputDirectoryPath,
                encodedArguments.ToString(),
                waitForExit: waitForExit,
                shareProcessObject: shareProcessObject,
                environmentVariables: context.GetEnvironmentVariables());

            return result;
        }

        /// <summary>
        /// Source: https://stackoverflow.com/a/12364234
        /// </summary>
        private static string EncodeParameterArgument(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return original;
            }
                
            var value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
            value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");

            return value;
        }

        private class StartOcspResponderContext : OpenSslContext
        {
            public StartOcspResponderContext(
                string signerId,
                string issuerId,
                string logFilePath,
                OpenSslRunner runner) : base(signerId, issuerId, runner)
            {
                LogFilePath = logFilePath;
            }

            public string LogFilePath { get; }
        }

        private class IssueCrlContext : OpenSslContext
        {
            public IssueCrlContext(string id, OpenSslRunner runner)
                : base(id, id, runner)
            {
            }
        }

        private class RevokeCertificateContext : OpenSslContext
        {
            public RevokeCertificateContext(
                string id,
                string issuerId,
                string crlReason,
                DateTimeOffset? compromiseTime,
                OpenSslRunner runner) : base(id, issuerId, runner)
            {
                CrlReason = crlReason;
                CompromiseTime = compromiseTime;
            }

            public string CrlReason { get; }
            public DateTimeOffset? CompromiseTime { get; }
        }

        private class IssueCertificateContext : OpenSslContext
        {
            public IssueCertificateContext(
                string id,
                string issuerId,
                string requestExtensions,
                string extensions,
                int keyLengthInBits,
                DateTimeOffset startDate,
                DateTimeOffset endDate,
                string signatureAlgorithm,
                OpenSslRunner runner) : base(id, issuerId, runner)
            {
                RequestExtensions = requestExtensions;
                Extensions = extensions;
                KeyLengthInBits = keyLengthInBits;
                StartDate = startDate;
                EndDate = endDate;
                SignatureAlgorithm = signatureAlgorithm;
            }

            public string RequestExtensions { get; }
            public string Extensions { get; }
            public int KeyLengthInBits { get; }
            public DateTimeOffset StartDate { get; }
            public DateTimeOffset EndDate { get; }
            public string SignatureAlgorithm { get; }
        }

        private class OpenSslContext
        {
            private readonly OpenSslRunner _runner;

            public OpenSslContext(
                string id,
                string issuerId,
                OpenSslRunner runner)
            {
                Id = id;
                IssuerId = issuerId;
                _runner = runner;
            }

            public string Id { get; }
            public string IssuerId { get; }

            public int OcspPort => _runner._ocspPort;
            public string CaBaseUrl => _runner._caBaseUrl;
            public string ConfigFilePath => _runner._configFilePath;
            public string OutputDirectoryPath => _runner.OutputDirectoryPath;

            public string CertificateCrtFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.crt");
            public string CertificatePemFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.pem");
            public string CertificatePfxFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.pfx");
            public string CertificateRequestFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.csr.pem");
            public string CommonName => $"NUGET_DO_NOT_TRUST.{Id}.test.test";
            public string IssuerCertificatePemFilePath => Path.Combine(OutputDirectoryPath, $"{IssuerId}.pem");
            public string IssuerCrlFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.crl");
            public string IssuerCrlNumberFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.crlnumber");
            public string IssuerCrlPemFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.crl.pem");
            public string IssuerDatabaseFilePath => Path.Combine(OutputDirectoryPath, $"{IssuerId}.database");
            public string IssuerPrivateKeyFilePath => Path.Combine(OutputDirectoryPath, $"{IssuerId}.key.pem");
            public string IssuerRandomSeedFilePath => Path.Combine(OutputDirectoryPath, $"${Id}.randomseed");
            public string IssuerSerialNumberFilePath => Path.Combine(OutputDirectoryPath, $"{IssuerId}.serialnumber");
            public string NewCertsDirectoryPath => Path.Combine(_runner._newCertsBaseDirectoryPath, IssuerId);
            public string PrivateKeyFilePath => Path.Combine(OutputDirectoryPath, $"{Id}.key.pem");
            public string RandFilePath => Path.Combine(OutputDirectoryPath, ".rnd");

            public Dictionary<string, string> GetEnvironmentVariables()
            {
                var output = new Dictionary<string, string>();

                output["CA_BASE_URL"] = CaBaseUrl;
                output["COMMON_NAME"] = CommonName;
                output["ID"] = Id;
                output["ISSUER_CERTIFICATE_PEM_FILE_PATH"] = IssuerCertificatePemFilePath;
                output["ISSUER_CRL_FILE_PATH"] = IssuerCrlFilePath;
                output["ISSUER_CRL_NUMBER_FILE_PATH"] = IssuerCrlNumberFilePath;
                output["ISSUER_DATABASE_FILE_PATH"] = IssuerDatabaseFilePath;
                output["ISSUER_ID"] = IssuerId;
                output["ISSUER_PRIVATE_KEY_FILE_PATH"] = IssuerPrivateKeyFilePath;
                output["ISSUER_RANDOM_SEED_FILE_PATH"] = IssuerRandomSeedFilePath;
                output["ISSUER_SERIAL_NUMBER_FILE_PATH"] = IssuerSerialNumberFilePath;
                output["NEW_CERTS_DIRECTORY_PATH"] = NewCertsDirectoryPath;
                output["OCSP_PORT"] = OcspPort.ToString();
                output["RANDFILE"] = RandFilePath;

                return output;
            }
        }
        
        public void Dispose()
        {
            StopOcspResponder();
        }
    }
}
