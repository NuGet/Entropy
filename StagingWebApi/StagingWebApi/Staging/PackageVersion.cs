// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;

namespace StagingWebApi
{
    class PackageVersion : LinkedDataDocument
    {
        string _version;
        DateTime _staged;
        string _nuspecLocation;

        public PackageVersion(string address, string version, DateTime staged, string nuspecLocation) : base(address, "PackageVersion")
        {
            _version = version;
            _staged = staged;
            _nuspecLocation = nuspecLocation;
        }

        public override void WriteJson(JsonWriter jsonWriter)
        {
            jsonWriter.WriteStartObject();
            WriteResource(jsonWriter);
            jsonWriter.WritePropertyName("version");
            jsonWriter.WriteValue(_version);
            jsonWriter.WritePropertyName("staged");
            jsonWriter.WriteValue(_staged.ToString());
            jsonWriter.WritePropertyName("nuspecLocation");
            jsonWriter.WriteValue(_nuspecLocation);
            jsonWriter.WriteEndObject();
        }

        public static string MakeRelativeUri(string ownerName, string stageName, string packageId, string packageVersion)
        {
            return string.Format("{0}/{1}", Package.MakeRelativeUri(ownerName, stageName, packageId), packageVersion).ToLowerInvariant();
        }

        public static HttpResponseMessage HttpCreateResponse(Uri baseAddress, string ownerName, string stageName, string packageId, string packageVersion, DateTime staged, Uri nuspecLocation)
        {
            var createdPackageVersion = new PackageVersion(
                new Uri(baseAddress, PackageVersion.MakeRelativeUri(ownerName, stageName, packageId, packageVersion)).AbsoluteUri,
                packageVersion,
                staged,
                nuspecLocation.AbsoluteUri);

            return Utils.CreateJsonResponse(HttpStatusCode.Created, createdPackageVersion.ToJson());
        }
    }
}
