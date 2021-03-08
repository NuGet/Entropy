// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace RemoveSignaturesFromNuGetPackage
{
    [Flags]
    internal enum Signatures
    {
        None = 0,
        Primary = 1 << 0,
        PrimaryTimestamp = 1 << 1,
        Countersignature = 1 << 2,
        CountersignatureTimestamp = 1 << 3
    }
}
