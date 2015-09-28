// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace StagingWebApi
{
    class Package : LinkedDataDocument
    {
        HashSet<string> _owners;
        IDictionary<string, PackageVersion> _versions;

        public Package(string address, string id) : base(address, "Package")
        {
            Id = id;
            _owners = new HashSet<string>();
            _versions = new Dictionary<string, PackageVersion>();
        }

        public string Id { get; set; }

        public void Add(string version, DateTime staged, string nuspecLocation, string owner)
        {
            _versions[version] = new PackageVersion(
                BaseAddress + "/" + version.ToLowerInvariant(),
                version,
                staged,
                nuspecLocation);

            _owners.Add(owner);
        }

        public override void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteResource(jsonWriter);
            jsonWriter.WritePropertyName("id");
            jsonWriter.WriteValue(Id);
            jsonWriter.WritePropertyName("owners");
            jsonWriter.WriteStartArray();
            foreach (var owner in _owners)
            {
                jsonWriter.WriteValue(owner);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WritePropertyName("versions");
            jsonWriter.WriteStartArray();
            foreach (var version in _versions.Values)
            {
                version.WriteJson(jsonWriter);
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
        }

        public static string MakeRelativeUri(string ownerName, string stageName, string packageId)
        {
            return string.Format("{0}/{1}", Stage.MakeRelativeUri(ownerName, stageName), packageId).ToLowerInvariant();
        }
    }
}
