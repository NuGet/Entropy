namespace MyGetMirror
{
    public class VsixPackage
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return $"{Id}-{Version}";
        }
    }
}
