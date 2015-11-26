<<<<<<< HEAD
﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
=======
﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Newtonsoft.Json;
using System.IO;
>>>>>>> 15898dffd7c655c67c3d2a9a02c8142b328fef7d

namespace StagingWebApi
{
    public abstract class LinkedDataDocument
    {
        string _id;
        string _type;

        protected LinkedDataDocument(string id, string type)
        {
            _id = id;
            _type = type;
        }

        public string BaseAddress { get { return _id; } }

        public string ToJson()
        {
            using (var textWriter = new StringWriter())
            {
                using (var jsonWriter = new JsonTextWriter(textWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    WriteJson(jsonWriter);
                    jsonWriter.Flush();
                    textWriter.Flush();
                    return textWriter.ToString();
                }
            }
        }
        public abstract void WriteJson(JsonWriter jsonWriter);

        protected void WriteResource(JsonWriter jsonWriter)
        {
            jsonWriter.WritePropertyName("@id");
            jsonWriter.WriteValue(_id);
            jsonWriter.WritePropertyName("@type");
            jsonWriter.WriteValue(_type);
        }
    }
}