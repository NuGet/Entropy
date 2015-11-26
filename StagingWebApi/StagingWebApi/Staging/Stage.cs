<<<<<<< HEAD
﻿using Newtonsoft.Json;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
using System;
using System.Collections.Generic;

namespace StagingWebApi
{
    public class Stage : LinkedDataDocument
    {
        IDictionary<string, Package> _packages;

<<<<<<< HEAD
        public Stage(string address, string name) : base(address, "Stage")
        {
            Name = name;
=======
        public Stage(string address, string name, string v3SourceBaseAddress) : base(address, "Stage")
        {
            Name = name;
            V3SourceBaseAddress = v3SourceBaseAddress;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
            _packages = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; set; }

<<<<<<< HEAD
=======
        public string V3SourceBaseAddress { get; set; }

>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
        public override void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteResource(jsonWriter);
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(Name);
<<<<<<< HEAD
=======
            jsonWriter.WritePropertyName("sources");
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("v2");
            jsonWriter.WriteValue("unavailable");
            jsonWriter.WritePropertyName("v3");
            jsonWriter.WriteValue(GetV3Source());
            jsonWriter.WriteEndObject();
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
            jsonWriter.WritePropertyName("packages");
            jsonWriter.WriteStartArray();
            foreach (var package in _packages.Values)
            {
                package.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        public void Add(string id, string version, DateTime staged, string nuspecLocation, string packageOwner)
        {
            Package package;
            if (!_packages.TryGetValue(id, out package))
            {
                package = new Package(BaseAddress + "/" + id.ToLowerInvariant(), id);
                _packages.Add(id, package);
            }
            package.Add(version, staged, nuspecLocation, packageOwner);
        }
        public static string MakeRelativeUri(string ownerName, string stageName)
        {
            return string.Format("{0}/{1}", Owner.MakeRelativeUri(ownerName), stageName).ToLowerInvariant();
        }
<<<<<<< HEAD
=======

        private string GetV3Source()
        {
            return string.Format("{0}{1}/index.json", V3SourceBaseAddress, Name);
        }
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
    }
}