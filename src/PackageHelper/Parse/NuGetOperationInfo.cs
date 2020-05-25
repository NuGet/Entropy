using System;
using System.Collections.Generic;
using System.Diagnostics;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Request,nq} -> {Operation,nq}")]
    public class NuGetOperationInfo
    {
        public NuGetOperationInfo(
            NuGetOperation operation,
            StartRequest request,
            IReadOnlyList<KeyValuePair<string, Uri>> sourceResourceUris)
        {
            Operation = operation;
            Request = request;
            SourceResourceUris = sourceResourceUris;
        }

        public NuGetOperation Operation { get; }
        public StartRequest Request { get; }
        public IReadOnlyList<KeyValuePair<string, Uri>> SourceResourceUris { get; }
    }
}
