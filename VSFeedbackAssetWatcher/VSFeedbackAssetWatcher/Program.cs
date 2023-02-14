using System;
using System.IO;
public class FileWatcher
{
    static string path;

    public static void Main(string[] args)
    {
        path = Path.Combine(Environment.GetFolderPath(
    Environment.SpecialFolder.LocalApplicationData), "Temp");
        path = Path.Combine(path, "NuGetScratch");
        // If a directory is not specified, exit program.
        if (args.Length != 1)
        {
            // Display the proper way to call the program.
            Console.WriteLine("Usage: VSFeedbackAssetWatcher.exe <directory>");
            Console.WriteLine($"Defaulting to NuGetScratch folder {path}");

        }
        else
        {
            path = args[0];
        }

        try
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = path;
            // Watch both files and subdirectories.
            watcher.IncludeSubdirectories = true;
            // Watch for all changes specified in the NotifyFilters
            //enumeration.
            watcher.NotifyFilter = NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.DirectoryName |
            NotifyFilters.FileName |
            NotifyFilters.LastAccess |
            NotifyFilters.LastWrite |
            NotifyFilters.Security |
            NotifyFilters.Size;
            // Watch all files.
            watcher.Filter = "*.zip";
            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            //Start monitoring.
            watcher.EnableRaisingEvents = true;
            Console.WriteLine("Press \'q\' to quit the sample.");
            Console.WriteLine();
            //Make an infinite loop till 'q' is pressed.
            while (Console.Read() != 'q') ;
        }
        catch (IOException e)
        {
            Console.WriteLine("A Exception Occurred :" + e);
            Console.ReadKey();
        }
        catch (Exception oe)
        {
            Console.WriteLine("An Exception Occurred :" + oe);
            Console.ReadKey();
        }
    }
    // Define the event handlers.
    public static void OnChanged(object source, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(".zip"))
        {
            try
            {
                Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);
                var backupFolder = Path.Combine(path, "Backup");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                var dest = Path.Combine(backupFolder, e.Name);
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

                File.Copy(e.FullPath, dest);
            }
            catch (Exception)
            {

            }
        }
    }

    public static void OnCreated(object source, FileSystemEventArgs e)
    {
        // Specify what is done when a file is changed.
        if (e.FullPath.Contains(".zip"))
        {
            try
            {
                Console.WriteLine("{0}, with path {1} has been {2}", e.Name, e.FullPath, e.ChangeType);
                var backupFolder = Path.Combine(path, "Backup");
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                File.Copy(e.FullPath, Path.Combine(backupFolder, e.Name));
            }
            catch (Exception)
            {

            }
        }
    }

    public static void OnRenamed(object source, RenamedEventArgs e)
    {
        // Specify what is done when a file is renamed.
        //Console.WriteLine(" {0} renamed to {1}", e.OldFullPath, e.FullPath);
    }
}