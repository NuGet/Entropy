using Microsoft.ApplicationInsights;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGetPackageAnalyzerFunction.Extensions;
using NuGetPackageAnalyzerFunction.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetPackageAnalyzerFunction
{
    public static class TimestampV1Analyzer
    {
        private static readonly string v3_flatContainer_nupkg_template = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg";
        private static readonly string P7SExtension = ".p7s";
        private static readonly HashSet<string> allExtensions = new HashSet<string>() {
            P7SExtension
        };

        public static async Task Process(CatalogLeafMessage message, TelemetryClient telemetry, ILogger log)
        {
            var url = GetFlatContainerNupkgUrl(message);
            // Create a temp directory for extracting pdb/PE files.
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            try
            {
                log.LogInformation($"Started processing {message.Id}/{message.Version} : {url.ToString()}");

                var analyzedObject = new AnalyzedObject(message.Id, message.Version, message.Created);

                // Download the nupkg file locally and process for the required data.
                using (var nupkgStream = await PackageDownloader.DownloadAsync(url, telemetry, log, new CancellationToken()))
                using (PackageArchiveReader packageReader = new PackageArchiveReader(nupkgStream))
                {
                    CancellationToken cancellationToken = CancellationToken.None;
                    var signature = await packageReader.GetPrimarySignatureAsync(cancellationToken);
                    analyzedObject.hasPrimarySignature = true;
                    analyzedObject.primarySignatureType = signature.Type.ToString();

                    var repositoryCountersignatureExists = SignatureUtility.HasRepositoryCountersignature(signature);
                    analyzedObject.hasCounterSignature = repositoryCountersignatureExists;

                    var signerInfo = signature.SignedCms.SignerInfos[0];

                    AnalyzeFiles(signerInfo, analyzedObject, isPrimary: true);

                    if (repositoryCountersignatureExists)
                    {
                        var countersignatureSignerInfo = signature.SignedCms.SignerInfos[0].CounterSignerInfos[0];

                        AnalyzeFiles(countersignatureSignerInfo, analyzedObject, isPrimary: false);
                    }
                    await SqlWriter.AddRecordAsync(analyzedObject, log);
                    telemetry.TrackMetric("PackageAnalyzed", 1, properties: new Dictionary<string, string> {
                        { "Id", message.Id },
                        { "Version", message.Version }
                    });
                }
                log.LogInformation($"Completed processing {message.Id}/{message.Version} : {url.ToString()}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing {message.Id} {message.Version}", ex);
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        private static void AnalyzeFiles(SignerInfo signerInfo, AnalyzedObject analyzedObject, bool isPrimary)
        {
            int countV1 = 0, countV2 = 0;

            CryptographicAttributeObjectCollection unsignedAttributes = signerInfo.UnsignedAttributes;

            foreach (CryptographicAttributeObject unsignedAttribute in unsignedAttributes)
            {
                if (string.Equals(unsignedAttribute.Oid.Value, Oids.SignatureTimeStampTokenAttribute, StringComparison.Ordinal))
                {
                    foreach (AsnEncodedData value in unsignedAttribute.Values)
                    {
                        var timestampCms = new SignedCms();

                        timestampCms.Decode(value.RawData);

                        if (isPrimary)
                        {
                            analyzedObject.primarySignatureTimestampCertSubject = timestampCms.Certificates[timestampCms.Certificates.Count - 1].Subject;
                        }
                        else
                        {
                            analyzedObject.counterSignatureTimestampCertSubject = timestampCms.Certificates[timestampCms.Certificates.Count - 1].Subject;
                        }

                        foreach (var attribute in timestampCms.SignerInfos[0].SignedAttributes)
                        {
                            switch (attribute.Oid.Value)
                            {
                                case Oids.SigningCertificate:
                                    countV1 += attribute.Values.Count;
                                    break;

                                case Oids.SigningCertificateV2:
                                    countV2 += attribute.Values.Count;
                                    break;
                            }
                        }
                    }
                }
            }

            if (isPrimary)
            {
                analyzedObject.primaryTimestampV1Count = countV1;
                analyzedObject.primaryTimestampV2Count = countV2;
            }
            else 
            {
                analyzedObject.counterSignatureTimestampV1Count = countV1;
                analyzedObject.counterSignatureTimestampV2Count = countV2;
            }
        }

        private static void Extract(ZipArchiveEntry entry, string targetDirectory)
        {
            string destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
            string destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            entry.ExtractToFile(destinationPath);
        }

        private static Uri GetFlatContainerNupkgUrl(CatalogLeafMessage message)
        {
            var url = string.Format(v3_flatContainer_nupkg_template,
                    message.Id,
                    message.Version);

            return new Uri(url);
        }
    }
}
