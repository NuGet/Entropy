using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NupkgValidator
{
    public class PackageFolderDictionary : Dictionary<string, PackageFolder>
    {

    }

    public class PackageFolder
    {
        Dictionary<string, List<PackageInfo>> _packageInfosById = new Dictionary<string, List<PackageInfo>>();

        public Dictionary<string, List<PackageInfo>> PackageInfosById
        {
            get
            {
                return _packageInfosById;
            }
        }

        public void AddPackageInfo(PackageInfo packageInfo)
        {
            List<PackageInfo> packageInfos = null;
            PackageInfosById.TryGetValue(packageInfo.Id, out packageInfos);
            if (packageInfos == null)
            {
                packageInfos = new List<PackageInfo>();
                PackageInfosById.Add(packageInfo.Id, packageInfos);
                PackageIdCount++;
            }

            packageInfos.Add(packageInfo);
            PackageCount++;

            if (packageInfo.Signed)
            {
                SignedPackageCount++;
            }

            if (packageInfo.Verified)
            {
                VerifiedPackageCount++;
            }

            if (packageInfo.Malformed)
            {
                MalformedPackageCount++;
            }

            Megabytes += packageInfo.Megabytes;
        }

        public int PackageCount { get; private set; }
        public int PackageIdCount { get; private set; }
        public int SignedPackageCount { get; private set; }
        public int MalformedPackageCount { get; private set; }
        public int VerifiedPackageCount { get; private set; }
        public double Megabytes { get; private set; }
        public string Path { get; internal set; }
    }
}
