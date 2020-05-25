using System;
using System.Collections.Generic;

namespace PackageHelper.Parse
{
    public class ParsedUrlAndResources
    {
        public ParsedUrlAndResources(ParsedUrl parsedUrl, IReadOnlyList<KeyValuePair<string, Uri>> sourceResourceUris)
        {
            ParsedUrl = parsedUrl;
            SourceResourceUris = sourceResourceUris;
        }

        public ParsedUrl ParsedUrl { get; }
        public IReadOnlyList<KeyValuePair<string, Uri>> SourceResourceUris { get; }
    }
}
