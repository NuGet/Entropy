using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    class IssueLabels
    {
        public static string ClosedPrefix = "Resolution:";
        public static string RegressionDuringThisVersion = "RegressionDuringThisVersion";
        public static string EngImprovement = "Category:Engineering";
        public static string Epic = "Epic";
        public static string Feature = "Type:Feature";
        public static string DCR = "Type:DCR";
        public static string Bug = "Type:Bug";
        public static string Spec = "Type:Spec";
        public static string Test = "Type:Test";
        public static string Docs = "Type:Docs";

    }
}
