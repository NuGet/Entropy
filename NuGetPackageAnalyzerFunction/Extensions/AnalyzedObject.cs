using System;

namespace NuGetPackageAnalyzerFunction.Extensions
{
    public class AnalyzedObject
    {
        public string Id;
        public string Version;
        public DateTime? Created;
        public bool hasPrimarySignature;
        //Author , Repository
        public string primarySignatureType;
        public string primarySignatureTimestampCertSubject;
        //newly added
        public int primaryTimestampV1Count;
        public int primaryTimestampV2Count;

        public bool hasCounterSignature;
        public string counterSignatureTimestampCertSubject;
        public int counterSignatureTimestampV1Count;
        public int counterSignatureTimestampV2Count;

        public AnalyzedObject(string id, string version) : this(id, version, null)
        { }

        public AnalyzedObject(string id, string version, DateTime? created)
        {
            Id = id;
            Version = version;
            Created = created;
        }
    }
}
