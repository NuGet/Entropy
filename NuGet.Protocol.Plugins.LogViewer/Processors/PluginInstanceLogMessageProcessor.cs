﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Protocol.Plugins.LogViewer
{
    internal sealed class PluginInstanceLogMessageProcessor : LogMessageProcessor
    {
        internal PluginInstanceLogMessageProcessor()
            : base("plugin instance")
        {
        }
    }
}