using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangelogGenerator
{
    class IssueLabels
    {
        public static string ClosedPrefix = "ClosedAs:";
        public static string RegressionDuringThisVersion = "RegressionDuringThisVersion";
        public static string EngImprovement = "Category:Engineering";
        public static string Epic = "Epic";
        public static string Feature = "Type:Feature";
        public static string DCR = "Type:DCR";
        public static string Bug = "Type:Bug";
        public static string Spec = "Type:Spec";

    }
}
