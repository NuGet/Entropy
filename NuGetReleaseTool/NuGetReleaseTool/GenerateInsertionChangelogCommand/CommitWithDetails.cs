namespace NuGetReleaseTool.GenerateInsertionChangelogCommand
{
    public class CommitWithDetails
    {
        public string Sha { get; }
        public string Author { get; }
        public string Link { get; }
        public string Message { get; }

        public ISet<Tuple<int, string>> Issues { get; }

        public Tuple<int, string> PR { get; set; }

        public CommitWithDetails(string sha, string author, string link, string message)
        {
            Sha = sha;
            Author = author;
            Link = link;
            Message = message?
                        .Replace("\r", " ")
                        .Replace("\n", " ");
            Issues = new HashSet<Tuple<int, string>>();
        }
    }
}
