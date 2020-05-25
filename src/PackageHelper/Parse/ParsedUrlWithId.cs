using PackageHelper.Replay;

namespace PackageHelper.Parse
{
    public class ParsedUrlWithId : ParsedUrl
    {
        public ParsedUrlWithId(ParsedUrlType type, StartRequest request, string id)
            : base(type, request)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
