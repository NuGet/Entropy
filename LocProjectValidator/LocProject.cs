using System;

namespace LocProjectValidator
{
    public class LocProject
    {
        public DateTime DateTime { get; set; }
        public string BuildSystem { get; set; }
        public SourceControl SourceControl { get; set; }
        public object[] LanguageSets { get; set; }
        public Project[] Projects { get; set; }
        public LanguageFolderMappings LanguageFolderMappings { get; set; }
    }

    public class SourceControl
    {
        public string Type { get; set; }
        public string Url { get; set; }
        public string Branch { get; set; }
        public string Repository { get; set; }
    }

    public class LanguageFolderMappings
    {
        public string CHS { get; set; }
        public string CHT { get; set; }
        public string CSY { get; set; }
        public string DEU { get; set; }
        public string ESN { get; set; }
        public string FRA { get; set; }
        public string ITA { get; set; }
        public string JPN { get; set; }
        public string KOR { get; set; }
        public string PLK { get; set; }
        public string PTB { get; set; }
        public string RUS { get; set; }
        public string TRK { get; set; }
    }

    public class Project
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public LocItem[] LocItems { get; set; }
        public object[] LssFiles { get; set; }
    }

    public class LocItem
    {
        public string SourceFile { get; set; }
        public string LclFile { get; set; }
        public string LcgFile { get; set; }
        public string LciFile { get; set; }
        public string Languages { get; set; }
        public string CopyOption { get; set; }
        public string[] LssFiles { get; set; }
    }
}
