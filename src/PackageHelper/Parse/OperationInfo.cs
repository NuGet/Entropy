using System;
using System.Collections.Generic;
using System.Diagnostics;
using PackageHelper.Replay.Requests;

namespace PackageHelper.Parse
{
    [DebuggerDisplay("{Request,nq} -> {Operation,nq}")]
    public class OperationInfo
    {
        public OperationInfo(
            Operation operation,
            StartRequest request,
            IReadOnlyList<KeyValuePair<string, Uri>> sourceResourceUris)
        {
            Operation = operation;
            Request = request;
            SourceResourceUris = sourceResourceUris;
        }

        public Operation Operation { get; }
        public StartRequest Request { get; }
        public IReadOnlyList<KeyValuePair<string, Uri>> SourceResourceUris { get; }
    }
}
