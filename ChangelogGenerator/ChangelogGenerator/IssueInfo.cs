using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    public class IssueInfo
    {
        public Issue Issue { get; set; } 

        public bool HideFromReleaseNotes { get; set; }
        public bool IsFix { get; set; }
        public bool FilterFromIssueList { get; set; }
        public IssueType IssueType { get; set; }
    }
}
