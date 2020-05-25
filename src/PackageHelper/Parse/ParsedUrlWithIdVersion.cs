using PackageHelper.Replay;

namespace PackageHelper.Parse
{
    public class ParsedUrlWithIdVersion : ParsedUrlWithId
    {
        public ParsedUrlWithIdVersion(ParsedUrlType type, StartRequest request, string id, string version)
            : base(type, request, id)
        {
            Version = version;
        }

        public string Version { get; }
    }
}
