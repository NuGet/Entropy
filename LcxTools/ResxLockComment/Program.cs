using System.Collections;
using System.CommandLine;
using System.Resources;

namespace ResxComment
{
    internal enum RunningMode
    {
        Report,
        Remove,
        Comment,
    }

    internal class Program
    {
        static HashSet<String> LangSuffixes = new(){ "chs", "cht", "csy", "deu", "esp", "fra", "ita", "jpn", "kor", "plk", "ptb", "rus", "trk" };

        static void Main(string[] args)
        {
            var optInput = new Option<FileInfo>("--resx", ".resx file to comment");
            optInput.IsRequired = true;

            var optOutput = new Option<FileInfo>("-o", "output file");

            var optMode = new Option<RunningMode>(name: "-m", description: "Running Mode", getDefaultValue: () => RunningMode.Report);

            var rootCmd = new RootCommand(".resx commenter");

            rootCmd.Add(optInput);
            rootCmd.Add(optOutput);
            rootCmd.Add(optMode);

            rootCmd.SetHandler(Run, optInput, optOutput, optMode);

            rootCmd.Invoke(args);
        }




        static void Run(FileInfo resxFile, FileInfo outputFile, RunningMode mode)
        {
            HashSet<string> entriesToLookup = new();
            HashSet<string> allEntries = new();

            using ResXResourceReader resxReader = new(resxFile.FullName);

            TextWriter writerOut;
            if (outputFile == null)
            {
                writerOut = Console.Out;
            }
            else
            {
                writerOut = File.CreateText(outputFile.FullName);
            }

            // Create the file
            using ResXResourceWriter resxWriter = new(writerOut);
            resxReader.UseResXDataNodes = true;
            int localizedEntries = 0;
            bool isThreeLetterLocalizedResource = false;
            foreach (DictionaryEntry entry in resxReader)
            {
                if (entry.Key is string entryName)
                {
                    allEntries.Add(entryName);
                    var resxNode = entry.Value as ResXDataNode ?? throw new ArgumentNullException($"null entry {entryName}");
                    isThreeLetterLocalizedResource = false;
                    string entryWithoutPrefix = string.Empty;
                    if (entryName.Contains('_') && entryName.IndexOf('_') > 0)
                    {
                        string[] parts = entryName.Split('_');

                        if (LangSuffixes.Contains(parts[^1])) // last element
                        {
                            localizedEntries++;
                            isThreeLetterLocalizedResource = true;
                            entryWithoutPrefix = entryName[..entryName.LastIndexOf('_')]; // 0 to last index
                            entriesToLookup.Add(entryWithoutPrefix);

                            switch (mode)
                            {
                                case RunningMode.Comment:
                                    resxNode.Comment = "{Locked}";
                                    break;
                            }

                        }
                        else // it's a not three-letter suffixed string, add to collection
                        {
                            resxWriter.AddResource(resxNode);
                            entriesToLookup.Add(entryName);
                        }
                    }
                    else
                    {
                        resxWriter.AddResource(resxNode);
                        entriesToLookup.Add(entryName);
                    }

                    switch (mode)
                    {
                        case RunningMode.Report:
                        case RunningMode.Comment:
                            resxWriter.AddResource(resxNode);
                            break;

                        case RunningMode.Remove:
                            if (isThreeLetterLocalizedResource)
                            {
                                if (!allEntries.Contains(entryWithoutPrefix))
                                {
                                    Console.Error.WriteLine($"Warning: String resource not found: {entryWithoutPrefix}. Localized three letter string will not be removed: {entryName}");
                                    resxWriter.AddResource(resxNode);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Ignoring entry: {entry.Key}");
                }
            }

            foreach (string look in entriesToLookup)
            {
                if (!allEntries.Contains(look))
                {
                    Console.Error.WriteLine($"Entry not found in .resx file: {look}");
                }
            }

            foreach (DictionaryEntry entry in resxReader)
            {
                if (entry.Key is string entryName)
                {
                    var newName = entryName + "_enu";
                    if (entriesToLookup.Contains(newName))
                    {
                        Console.Error.WriteLine($"Found key: {newName}");
                    }
                }
            }

            Console.Error.WriteLine($"All resources: {allEntries.Count()}; Unique resources: {entriesToLookup.Count()}; Three letter localized resources: {localizedEntries}; Three-letter-Localized ratio: {Convert.ToDouble(localizedEntries) / allEntries.Count()}");
        }
    }
}