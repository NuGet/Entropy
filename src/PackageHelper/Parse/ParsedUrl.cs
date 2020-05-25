using PackageHelper.Replay;

namespace PackageHelper.Parse
{
    public class ParsedUrl
    {
        public ParsedUrl(ParsedUrlType type, StartRequest request)
        {
            Type = type;
            Request = request;
        }

        public ParsedUrlType Type { get; }
        public StartRequest Request { get; }

        public static ParsedUrl Unknown(StartRequest request)
        {
            return new ParsedUrl(ParsedUrlType.Unknown, request);
        }
    }
}
