// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace StagingWebApi
{
    public class StageDefinition
    {
        public bool IsValid { get; private set; }
        public string Reason { get; private set; }
        public string OwnerName { get; private set; }
        public string StageName { get; private set; }
        public string BaseService { get; private set; }

        StageDefinition()
        {
            IsValid = true;
            Reason = string.Empty;
        }

        public static StageDefinition ReadFromStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var definition = new StageDefinition();

            definition.IsValid = false;

            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();

                JObject obj;

                try
                {
                    obj = JObject.Parse(json);
                }
                catch (Exception)
                {
                    definition.Reason = "unable to parse content as JSON";
                    return definition;
                }

                definition.OwnerName = (string)obj["ownerName"];

                if (string.IsNullOrEmpty(definition.OwnerName))
                {
                    definition.Reason = "unable to read a ownerName";
                    return definition;
                }

                definition.StageName = (string)obj["stageName"];

                if (string.IsNullOrEmpty(definition.StageName))
                {
                    definition.Reason = "unable to read a stageName";
                    return definition;
                }

                definition.BaseService = (string)obj["baseService"];

                if (string.IsNullOrEmpty(definition.BaseService))
                {
                    definition.Reason = "unable to read a stageName";
                    return definition;
                }
            }

            definition.IsValid = true;

            return definition;
        }
    }
}
