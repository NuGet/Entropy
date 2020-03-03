using System;
using System.Diagnostics;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PkgInstallTesterVer2
{
    class Program
    {
        static void Main(string[] args)
        {
            string d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
            List<string> pkgsList = new List<string>();
            List<string> allPkgVersionsList = new List<string>();
            Console.WriteLine("How may packages would you like to evaluate? ");
            int.TryParse(Console.ReadLine(), out int numPkgs);
            if (numPkgs <= 100)
            {
                XmlReader topPkgsReader = XmlReader.Create("https://www.nuget.org/api/v2/Packages()?$orderby=DownloadCount" +
                "%20desc&$top="+ numPkgs.ToString() +"&semVerLevel=2.0.0&$filter=IsLatestVersion&$select=Id,NormalizedVersion");
                XDocument topPkgDoc = XDocument.Load(topPkgsReader);
                var pkgId = XName.Get("Id", d);
                var pkgVerId = XName.Get("NormalizedVersion", d);
                pkgsList = topPkgDoc.Descendants(pkgId).Select(x => x.Value).ToList<string>();
                allPkgVersionsList = topPkgDoc.Descendants(pkgVerId).Select(x => x.Value).ToList<string>();
            }
            else
            {
                int pkgsDone = 0;
                int pkgsLeft = numPkgs;
                int pkgsDoing = 0;
                while (pkgsLeft > 0)
                {
                    if (pkgsLeft < 100)
                    {
                        pkgsDoing = pkgsLeft;
                    }
                    else
                    {
                        pkgsDoing = 100;
                    }
                    XmlReader topPkgsReader = XmlReader.Create("https://www.nuget.org/api/v2/Packages()?$orderby=DownloadCount" +
                    "%20desc&$top="+ pkgsDoing.ToString() +"&semVerLevel=2.0.0&$filter=IsLatestVersion&$select=Id,NormalizedVersion&$skip=" 
                    + pkgsDone.ToString());
                    XDocument topPkgDoc = XDocument.Load(topPkgsReader);
                    var pkgId = XName.Get("Id", d);
                    var pkgVerId = XName.Get("NormalizedVersion", d);
                    pkgsList.AddRange(topPkgDoc.Descendants(pkgId).Select(x => x.Value).ToList<string>());
                    allPkgVersionsList.AddRange(topPkgDoc.Descendants(pkgVerId).Select(x => x.Value).ToList<string>());
                    pkgsDone += pkgsDoing;
                    pkgsLeft -= pkgsDoing;
                }
            }
            string[] pkgs = pkgsList.ToArray();
            string[] allPkgVersions = allPkgVersionsList.ToArray();
            string workingDirectoryRoot = @"C:\Users\" + Environment.UserName + @"\Desktop\pc\";
            string[] tfms = { "net40", "net462", "net48", "uap10.0.16299", "netstandard1.3",
                "netstandard2.0", "netcoreapp1.1", "netcoreapp2.0", "netcoreapp2.2",
                "Xamarin.iOS10.14", "MonoAndroid80", "Unity2018.1"};
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            List<string> uniqueErrors = new List<string>();
            List<string> uniqueWarnings = new List<string>();
            //WebClient versionScraper = new WebClient();
            //Stream versionsStream;
            //StreamReader versionStreamRead;
            string currentVersion;
            bool pkgInstallStatus = false;
            string csprojText;
            //string[] versions;
            //string output;
            Process dotnetInstance = new Process();
            string[] rowResults = new string[tfms.Length + 1];

            DataTable table = new DataTable();

            table.Columns.Add("Package/Framework", typeof(string));
            foreach (string tfm in tfms)
            {
                table.Columns.Add(tfm, typeof(string));
            }

            // Redirect the output stream of the child process.
            dotnetInstance.StartInfo.FileName = "dotnet.Exe";
            dotnetInstance.StartInfo.RedirectStandardInput = true;
            dotnetInstance.StartInfo.RedirectStandardOutput = true;
            dotnetInstance.StartInfo.CreateNoWindow = true;
            dotnetInstance.StartInfo.UseShellExecute = false;

            if (!Directory.Exists(workingDirectoryRoot))
            {
                Directory.CreateDirectory(workingDirectoryRoot);
            }


            for (int k = 0; k < pkgs.Length; k++)
            {
                //try
                //{
                //    versionsStream = versionScraper.OpenRead("https://api.nuget.org/v3-flatcontainer/" + pkgs[k].ToLower() + "/index.json");
                //    versionStreamRead = new StreamReader(versionsStream);
                //    versions = versionStreamRead.ReadToEnd().Split('"');
                //    currentVersion = versions[versions.Length - 2];
                //    versionStreamRead.Close();
                //    versionsStream.Close();
                //}
                //catch (Exception)
                //{

                currentVersion = allPkgVersions[k];
                //}
                
                Console.WriteLine("Now testing package " + pkgs[k] + " version " + currentVersion);
                string previousTFM = null;
                for (int i = 0; i < tfms.Length; i++)
                {
                    rowResults[0] = pkgs[k];
                    string currentFolder = tfms[i];
                    string currentFramework;
                    if (tfms[i] == "Unity2018.1")
                    {
                        currentFramework = "net471";
                    }
                    else
                    {
                        currentFramework = tfms[i];
                    }
                    string currentDirectory = workingDirectoryRoot + pkgs[k] + @"\" + currentFolder;
                    dotnetInstance.StartInfo.WorkingDirectory = currentDirectory;

                    if (!Directory.Exists(currentDirectory))
                    {
                        Directory.CreateDirectory(currentDirectory);
                        if (i == 0)
                        {
                            dotnetInstance.StartInfo.Arguments = "new classlib";
                            dotnetInstance.Start();
                            dotnetInstance.StandardOutput.ReadToEnd();
                        }
                        else
                        {
                            string previousDirectory = workingDirectoryRoot + pkgs[k] + @"\" + tfms[i-1];
                            foreach (string dirPath in Directory.GetDirectories(previousDirectory, "*",
                                SearchOption.AllDirectories))
                                Directory.CreateDirectory(dirPath.Replace(previousDirectory, currentDirectory));
                            foreach (string newPath in Directory.GetFiles(previousDirectory, "*.*",
                                SearchOption.AllDirectories))
                                File.Copy(newPath, newPath.Replace(previousDirectory, currentDirectory), true);
                            File.Delete(currentDirectory + @"\obj\project.assets.json");
                        }

                    }



                    //Console.WriteLine(output);

                    //Changes the target framework in the csproj file and add an empty target framework property
                    using (var csprojFileStream = File.Open(currentDirectory + @"\net40.csproj", FileMode.Open))
                    {
                        StreamReader csprojStream = new StreamReader(csprojFileStream);
                        csprojText = csprojStream.ReadToEnd();
                        if (!csprojText.Contains("<TargetFramework>" + currentFramework + "</TargetFramework\n    <DisableImplicitAssetTargetFallback>true</DisableImplicitAssetTargetFallback>"))
                        {
                            if (previousTFM == null)
                            {
                                csprojText = csprojText.Replace("<TargetFramework>netstandard2.0</TargetFramework>", "<TargetFramework>" + currentFramework + "</TargetFramework>\n    <DisableImplicitAssetTargetFallback>true</DisableImplicitAssetTargetFallback>");
                            }
                            else
                            {
                                if (csprojText.Contains("\n    <DisableImplicitAssetTargetFallback>true</DisableImplicitAssetTargetFallback>"))
                                {
                                    csprojText = csprojText.Replace("<TargetFramework>" + previousTFM + "</TargetFramework>", "<TargetFramework>" + currentFramework + "</TargetFramework>");
                                }
                                else
                                {
                                    csprojText = csprojText.Replace("<TargetFramework>" + previousTFM + "</TargetFramework>", "<TargetFramework>" + currentFramework + "</TargetFramework>\n    <DisableImplicitAssetTargetFallback>true</DisableImplicitAssetTargetFallback>");
                                }

                            }


                        }

                        if (!csprojText.Contains("  <ItemGroup>\n    <PackageReference Include = \"" + pkgs[k] + "\" Version = \"" + currentVersion + "\"/>\n  </ItemGroup>\n</Project>"))
                        {
                            string pkgRef = "  <ItemGroup>\n    <PackageReference Include = \"" + pkgs[k] + "\" Version = \"" + currentVersion + "\"/>\n  </ItemGroup>\n</Project>";
                            csprojText = csprojText.Replace("</Project>", pkgRef);
                        }
                        
                    }

                    File.WriteAllText(currentDirectory + @"\net40.csproj", csprojText);



                    // adds the package to the new proj
                    dotnetInstance.StartInfo.Arguments = "restore";
                    dotnetInstance.Start();
                    dotnetInstance.StandardOutput.ReadToEnd();

                    //determines if the package was successfully installed. If so, the package is removed and a message is shown
                    if (File.Exists(currentDirectory + @"\obj\project.assets.json"))
                    {
                        errors.Clear();
                        warnings.Clear();
                        using (StreamReader assetFile = new StreamReader(currentDirectory + @"\obj\project.assets.json"))
                        {
                            
                            var json = assetFile.ReadToEnd();
                            var root = JToken.Parse(json);
                            var logs = root["logs"];
                            if(logs != null)
                            {
                                JArray logsArray = logs.Value<JArray>();
                                foreach (var token in logsArray)
                                {
                                    var code = token["code"].ToString();
                                    var warningLevel = token["level"].ToString();

                                    if(warningLevel == "Error")
                                    {
                                        errors.Add(code);
                                        if (!uniqueErrors.Contains(code))
                                        {
                                            uniqueErrors.Add(code);
                                        }
                                    }
                                    else if (warningLevel == "Warning")
                                    {
                                        warnings.Add(code);
                                        if (!uniqueWarnings.Contains(code))
                                        {
                                            uniqueWarnings.Add(code);
                                        }
                                    }
                                }
                            }
                        }
                        if (errors.Count == 0)
                        {
                            pkgInstallStatus = true;
                        }
                        else
                        {
                            pkgInstallStatus = false;
                        }
                    }

                    if (pkgInstallStatus)
                    {
                        //Console.WriteLine("Package " + pkgs[k] + " was installed successfully in the " + currentFramework + " framework");
                        dotnetInstance.StartInfo.Arguments = "remove package " + pkgs[k];
                        dotnetInstance.Start();
                        dotnetInstance.WaitForExit();
                        
                        //WORKING AROUND NUGET BUG: HELP TO FILE AND/OR FIX
                        
                        string result = "Success!";
                        if(warnings.Count != 0)
                        {
                            result = result + " Warnings: ";
                            foreach (var warning in warnings)
                            {
                                result = result + warning + "/";
                            }
                        }
                        //output = dotnetInstance.StandardOutput.ReadToEnd();
                        //Console.WriteLine(output);
                        rowResults[i + 1] = result.Remove(result.Length - 1);
                    }
                    else
                    {
                        //Console.WriteLine("Package " + pkgs[k] + " was NOT installed successfully in the " + currentFramework + " framework");
                        string result = "Failure... Errors: ";
                        foreach (var error in errors)
                        {
                            result = result + error + "/";
                        }
                        if (warnings.Count != 0)
                        {
                            result = result + " Warnings: ";
                            foreach (var warning in warnings)
                            {
                                result = result + warning + "/";
                            }
                        }
                        rowResults[i + 1] = result.Remove(result.Length - 1);
                    }

                    previousTFM = currentFramework;
                }

                table.Rows.Add(rowResults);
            }
            dotnetInstance.WaitForExit();
            dotnetInstance.Close();
            createSpreadsheet(table, pkgs, allPkgVersions);
            createErrorReport(uniqueErrors, uniqueWarnings);
            //Directory.Delete(workingDirectoryRoot, true);

        }

        private static void createErrorReport(List<string> uniqueErrors, List<string> uniqueWarnings)
        {
            string txt = "Errors:\n";
            foreach(string code in uniqueErrors)
            {
                txt = txt + code + "\n";
            }
            txt += "Warnings:";
            foreach(string code in uniqueWarnings)
            {
                txt += "\n" + code;
            }
            File.WriteAllText(@"C:\Users\" + Environment.UserName + @"\Desktop\errWarnReport.txt", txt);
        }

        public static void createSpreadsheet(DataTable table, string[] pkgs, string[] allPkgVersions)
        {
            string csvText = null;
            foreach (DataColumn col in table.Columns)
            {
                if (csvText == null)
                {
                    csvText = col.ColumnName;
                }
                else
                {
                    csvText = csvText + ", " + col.ColumnName;
                }

            }
            csvText = csvText.Trim();
            foreach (DataRow row in table.Rows)
            {
                string textRow = null;
                for (int i = 0; i < row.ItemArray.Length; i++)
                {
                    if (i == 0)
                    {
                        textRow = (string)row.ItemArray[i] + "/ver. "+ allPkgVersions[Array.IndexOf(pkgs, row.ItemArray[i])];
                    }
                    else
                    {
                        textRow = textRow + "," + row.ItemArray[i];
                    }
                }

                csvText = csvText + "\n" + textRow;
            }
            File.WriteAllText(@"C:\Users\" + Environment.UserName + @"\Desktop\tfmVSpkgs.csv", csvText);
        }
    }
}
