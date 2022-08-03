using System.Collections;
using System.CommandLine;
using System.Resources;

namespace ResxComment
{
    internal class Program
    {
        static HashSet<String> LangSuffixes = new(){ "chs", "cht", "csy", "deu", "esp", "fra", "ita", "jpn", "kor", "plk", "ptb", "rus", "trk" };

        static void Main(string[] args)
        {
            var optInput = new Option<FileInfo>("--resx", ".resx file to comment");
            optInput.IsRequired = true;

            var optOutput = new Option<FileInfo>("-o", "output file");

            var rootCmd = new RootCommand(".resx commenter");

            rootCmd.Add(optInput);
            rootCmd.Add(optOutput);

            rootCmd.SetHandler(Run, optInput, optOutput);

            rootCmd.Invoke(args);
        }

        static void Run(FileInfo resxFile, FileInfo outputFile)
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
            foreach (DictionaryEntry entry in resxReader)
            {
                if (entry.Key is string entryName)
                {
                    allEntries.Add(entryName);
                    var resxNode = entry.Value as ResXDataNode ?? throw new ArgumentNullException($"null entry {entryName}");

                    if (entryName.Contains('_') && entryName.IndexOf('_') > 0)
                    {
                        string[] parts = entryName.Split('_');

                        if (LangSuffixes.Contains(parts[^1])) // last element
                        {
                            string entryWithoutPrefix = entryName[..entryName.LastIndexOf('_')]; // 0 to last index
                            Console.Error.WriteLine()
                            entriesToLookup.Add(entryWithoutPrefix);
                            resxNode.Comment = "{Locked}";
                        }
                    }
                    else
                    {
                        entriesToLookup.Add(entryName);
                    }

                    resxWriter.AddResource(resxNode);
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
        }
    }
}