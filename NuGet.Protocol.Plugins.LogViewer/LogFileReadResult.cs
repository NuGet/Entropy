// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal sealed class LogFileReadResult
    {
        internal IReadOnlyList<JObject> JObjects { get; }
        internal string Messages { get; }

        internal LogFileReadResult(IReadOnlyList<JObject> jObjects, string messages)
        {
            JObjects = jObjects;
            Messages = messages;
        }
    }
}