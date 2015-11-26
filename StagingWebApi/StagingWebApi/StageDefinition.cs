<<<<<<< HEAD
﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json.Linq;
using System;
using System.IO;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

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

<<<<<<< HEAD
            StageDefinition definition = new StageDefinition();

            definition.IsValid = false;

            using (StreamReader reader = new StreamReader(stream))
=======
            var definition = new StageDefinition();

            definition.IsValid = false;

            using (var reader = new StreamReader(stream))
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d
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
