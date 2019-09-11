using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NupkgValidator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        static Dictionary<string, List<PackageInfo>> PackagesById;
        static PackageFolderDictionary PackagesByFolder;


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var nuget162 = @"\\rr-vs2019latest\nupkg\verification-log.txt";
            var startOfCommand = @"C:\>c:\nupkg\nuget.exe verify -signatures """;
            var lenOfCommand = startOfCommand.Length;
            var lines = File.ReadAllLines(nuget162);
            PackagesById = new Dictionary<string, List<PackageInfo>>();
            PackagesByFolder = new PackageFolderDictionary();
            PackageInfo packageInfo = null;

            foreach (var line in lines)
            {
                var tLine = line.Trim();
                string id = null;
                string version = null;
                string fullName = null;

                if (tLine.StartsWith(@"C:\>c:\nupkg\nuget.exe verify -signatures "))
                {
                    if (packageInfo != null)
                    {
                        StorePackageInfo(packageInfo);
                    }

                    var nupkgPath = tLine.Substring(lenOfCommand, tLine.Length - lenOfCommand - 1);
                    FileInfo nupkgFileInfo = new FileInfo(nupkgPath);

                    var packageFileNameWithExtension = nupkgFileInfo.Name;
                    var possibleVersion = nupkgFileInfo.Directory;
                    var possibleId = possibleVersion.Parent;
                    string folderPath = null;
                    var predictedFileName = (possibleId.Name.ToLower() + "."
                        + possibleVersion.Name.ToLower() + ".nupkg");

                    packageInfo = new PackageInfo();

                    if (predictedFileName == packageFileNameWithExtension.ToLower())
                    {
                        id = possibleId.Name;
                        version = possibleVersion.Name;
                        folderPath = possibleId.Parent.FullName;
                    }
                    else
                    {
                        fullName = packageFileNameWithExtension;
                        folderPath = nupkgFileInfo.Directory.FullName;
                        id = fullName; // TODO: this includes id.version.nupkg
                    }

                    packageInfo.Id = id;
                    packageInfo.Version = version;
                    packageInfo.Path = nupkgPath;
                    packageInfo.FolderPath = folderPath;
                    if (nupkgFileInfo.Exists)
                    {
                        packageInfo.Megabytes = nupkgFileInfo.Length / 1024000.0;
                    }
                }
                else if (tLine == "Signature type: Author")
                {
                    packageInfo.HasAuthorSignature = true;
                }
                else if (tLine == "Signature type: Repository")
                {
                    packageInfo.HasRepoSignature = true;
                }
                else if (tLine.StartsWith("NU3005"))
                {
                    packageInfo.Malformed = true;
                }
                else if (tLine.StartsWith("Successfully verified package"))
                {
                    packageInfo.Verified = true;
                }
            }

            OutputResults();
        }

        private void OutputResults()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("**Ids********************************************");

            //foreach (var packageId in PackagesById.Keys)
            //{
            //    sb.AppendLine(packageId);
            //    foreach (var packageInfo in PackagesById[packageId])
            //    {
            //        sb.AppendLine("  " + packageInfo.Version + " "
            //            + (packageInfo.Signed ? "Signed" : "NOTSIGNED") + " "
            //            + (packageInfo.Verified ? "Verified" : "NOTVERIFIED") + " "
            //            + packageInfo.Path);
            //    }
            //}

            sb.AppendLine("**SummaryOfFolders********************************************");
            foreach (var packageFolderPath in PackagesByFolder.Keys)
            {
                var packageFolder = PackagesByFolder[packageFolderPath];
                sb.AppendLine($"signed: {packageFolder.SignedPackageCount} / {packageFolder.PackageCount} {(packageFolder.MalformedPackageCount > 0 ? "malformed: " + packageFolder.MalformedPackageCount : "")} - {string.Format("{0:0.00}", packageFolder.Megabytes)}MB - {packageFolder.Path}");
            }

            sb.AppendLine("**DetailsOfFolders********************************************");
            foreach (var packageFolderPath in PackagesByFolder.Keys)
            {
                var packageFolder = PackagesByFolder[packageFolderPath];
                sb.AppendLine($"signed: {packageFolder.SignedPackageCount} / {packageFolder.PackageCount} {(packageFolder.MalformedPackageCount > 0 ? "malformed: " + packageFolder.MalformedPackageCount : "")} - {string.Format("{0:0.00}", packageFolder.Megabytes)}MB - {packageFolder.Path}");
                foreach (var packageId in packageFolder.PackageInfosById.Keys)
                {
                    sb.AppendLine("  " + packageId);
                    foreach (var packageInfo in packageFolder.PackageInfosById[packageId])
                    {
                        sb.AppendLine("    " + packageInfo.Version + " "
                            + (packageInfo.Signed ? "Signed" : (!packageInfo.Malformed ? "ErrorNOTSIGNED" : "ErrorMALFORMED")) + " "
                            + (packageInfo.Verified ? "Verified" : "NOTVERIFIED") + " "
                            + string.Format("{0:0.00}", packageInfo.Megabytes) + "MB "
                            + packageInfo.Path);
                    }
                }
            }

            var tb = new TextBox();
            this.Content = tb;
            tb.Text = sb.ToString();
        }

        private PackageInfo StorePackageInfo(PackageInfo packageInfo)
        {
            List<PackageInfo> versions = null;
            if (packageInfo.Id != null)
            {
                // Store by PackageId
                var found = PackagesById.TryGetValue(packageInfo.Id, out versions);
                if (!found)
                {
                    versions = new List<PackageInfo>();
                    PackagesById.Add(packageInfo.Id, versions);
                }

                versions.Add(packageInfo);

                // Store by PackageFolder
                PackageFolder packageFolder;
                var folderFound = PackagesByFolder.TryGetValue(packageInfo.FolderPath, out packageFolder);
                if (!folderFound)
                {
                    packageFolder = new PackageFolder()
                    {
                        Path = packageInfo.FolderPath
                    };

                    PackagesByFolder.Add(packageInfo.FolderPath, packageFolder);
                }

                packageFolder.AddPackageInfo(packageInfo);

                return packageInfo;
            }
            else
            {
                throw new ArgumentNullException(nameof(packageInfo.Id));
            }
        }
    }
}
