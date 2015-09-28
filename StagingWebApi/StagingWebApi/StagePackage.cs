// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using NuGet.Versioning;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace StagingWebApi
{
    public class StagePackage
    {
        public string Id { get; private set; }
        public string Version { get; private set; }
        public Stream NuspecStream { get; }
        public bool IsValid { get; private set; }
        public string Reason { get; private set; }

        public StagePackage()
        {
            NuspecStream = new MemoryStream();
            IsValid = true;
            Reason = string.Empty;
        }

        public StagePackage(string id, string version)
        {
            NuspecStream = new MemoryStream();
            IsValid = true;
            Reason = string.Empty;

            if (!ValidateAndSetId(id))
            {
                return;
            }

            if (!ValidateAndSetVersion(version))
            {
                return;
            }
        }

        public string GetNupkgName(string path)
        {
            return string.Format("{0}{1}.{2}.nupkg", path, Id, Version).ToLowerInvariant();
        }
        public string GetNuspecName(string path)
        {
            return string.Format("{0}{1}.nuspec", path, Id).ToLowerInvariant();
        }

        public bool ValidateAndSetId(string id)
        {
            int indexOf = id.IndexOfAny(new char[] { '/' });
            if (indexOf != -1)
            {
                IsValid = false;
                Reason = "id must not contain '/' character";
                return false;
            }
            else
            {
                Id = id;
                return true;
            }
        }

        public bool ValidateAndSetVersion(string version)
        {
            NuGetVersion nugetVersion;
            if (!NuGetVersion.TryParse(version, out nugetVersion))
            {
                IsValid = false;
                Reason = "version format was not acceptable";
                return false;
            }
            else
            {
                Version = nugetVersion.ToNormalizedString();
                return true;
            }
        }

        public void Load(string id, string version)
        {
            // skips validation
            Id = id;
            Version = version;
        }

        public static StagePackage ReadFromStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            StagePackage stagePackage = new StagePackage();

            stagePackage.IsValid = false;

            ZipArchive archive = null;

            try
            {
                try
                {
                    archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                }
                catch (InvalidDataException)
                {
                    stagePackage.Reason = "unable to open stream as a zip archive";
                }

                if (archive != null)
                {
                    stagePackage.Reason = "unable to find nuspec file in nupkg";

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.LastIndexOf('/') >= 0)
                        {
                            continue;
                        }
                        if (entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Open().CopyTo(stagePackage.NuspecStream);
                            stagePackage.NuspecStream.Seek(0, SeekOrigin.Begin);
                            ReadNuspec(stagePackage);
                            break;
                        }
                    }
                }
            }
            finally
            {
                if (archive != null)
                {
                    archive.Dispose();
                }
            }

            return stagePackage;
        }

        static void ReadNuspec(StagePackage package)
        {
            XDocument document = XDocument.Load(package.NuspecStream);

            XElement idElement = document.Root.DescendantsAndSelf().Elements().Where(d => d.Name.LocalName == "id").FirstOrDefault();
            if (idElement == null)
            {
                package.IsValid = false;
                package.Reason = "unable to find the id element in the nuspec";
                return;
            }

            if (!package.ValidateAndSetId(idElement.Value))
            {
                return;
            }

            XElement versionElement = document.Root.DescendantsAndSelf().Elements().Where(d => d.Name.LocalName == "version").FirstOrDefault();
            if (versionElement == null)
            {
                package.IsValid = false;
                package.Reason = "unable to find the version element in the nuspec";
                return;
            }

            if (!package.ValidateAndSetVersion(versionElement.Value))
            {
                return;
            }

            //  extraction of other fields and validation goes here

            package.NuspecStream.Seek(0, SeekOrigin.Begin);

            package.IsValid = true;
            package.Reason = string.Empty;
        }
    }
}