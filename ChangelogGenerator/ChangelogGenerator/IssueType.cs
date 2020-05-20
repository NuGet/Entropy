using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    public enum IssueType
    {
        None = 0,
        Feature,
        Bug,
        DCR,
        Spec,
        StillOpen,
    }
}
