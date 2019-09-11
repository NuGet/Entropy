using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NupkgValidator
{
    public class PackageInfo
    {
        public string Id { get; internal set; }
        public string Version { get; internal set; }
        public string Path { get; internal set; }
        public bool HasAuthorSignature { get; internal set; }
        public bool HasRepoSignature { get; internal set; }
        public bool Verified { get; internal set; }
        public double Megabytes { get; internal set; }
        public bool Signed
        {
            get
            {
                return HasAuthorSignature || HasRepoSignature;
            }
        }

        public string FolderPath { get; internal set; }
        public bool Malformed { get; internal set; }
    }
}
