using Octokit;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace GithubIssueTagger.Reports
{
    internal class AreaOwnerReport : IReport
    {
        private GitHubClient _client;
        private QueryCache _queryCache;

        public AreaOwnerReport(GitHubClient client, QueryCache queryCache)
        {
            _client = client;
            _queryCache = queryCache;
        }

        public async Task RunAsync()
        {
            if (_queryCache.AllHomeIssues is null)
            {
                _queryCache.AllHomeIssues = await IssueUtilities.GetAllIssuesAsync(_client, "nuget", "home");
            }

            if (_queryCache.AllHomeLabels is null)
            {
                _queryCache.AllHomeLabels = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }

            List<Label> ignoreLabels = new List<Label>()
            {
                //2671458320      Type:Tracking
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 2671458320),

                //801160517       Type: Spec
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 801160517),

                //1593926950      Type: DeveloperDocs
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 1593926950),

                //249737088       Type: Docs
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 249737088),

                //180116592       Type: Feature
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 180116592),

                //2185215650      Type: Learning
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 2185215650),
            };

            List<Label> validTypeLabels = new List<Label>()
            {
                //180116450       Type:Bug
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 180116450),

                //386656158       Type: DataAnalysis
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 386656158),

                //180970997       Type: DCR
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 180970997),

                //979424473       Type: Test
                _queryCache.AllHomeLabels.SingleOrDefault(label => label.Id == 979424473),
            };

            IEnumerable<Issue> includedIssues = _queryCache.AllHomeIssues.Where(issue => !issue.Labels.Any(label => ignoreLabels.Any(ignoreLabel => ignoreLabel.Id == label.Id)));

            //263262236       Functionality:VisualStudioUI = PM UI
            Label pmuiLabel = await GetLabelById(263262236);
            IEnumerable<Issue> pmuiIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == pmuiLabel.Id));
            Console.WriteLine("PM UI\t" + pmuiIssues.Count() + GetUntypedIssueCountString(pmuiIssues, validTypeLabels));

            //430506461       Product: VS.PMConsole = PMC
            Label pmcLabel = await GetLabelById(430506461);
            IEnumerable<Issue> pmcIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == pmcLabel.Id));
            Console.WriteLine("PMC\t" + pmcIssues.Count() + GetUntypedIssueCountString(pmcIssues, validTypeLabels));

            //171462728       Functionality: SDK = SDK
            Label sdkLabel = await GetLabelById(171462728);
            IEnumerable<Issue> sdkIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == sdkLabel.Id));
            Console.WriteLine("SDK\t" + sdkIssues.Count() + GetUntypedIssueCountString(sdkIssues, validTypeLabels));

            //CLI(nuget, dotnet, msbuild)
            //{
            //    722811433       Product: dotnet.exe
            //    182924875       Product: NuGet.exe
            //    1048477918      Product: MSBuildSDKResolver
            //}
            Label cliLabel1 = await GetLabelById(722811433);
            Label cliLabel2 = await GetLabelById(182924875);
            Label cliLabel3 = await GetLabelById(1048477918);
            var cliLabels = new long[] { cliLabel1.Id, cliLabel2.Id, cliLabel3.Id };
            IEnumerable<Issue> cliIssues = includedIssues.Where(issue => issue.Labels.Any(label => cliLabels.Contains(label.Id)));
            Console.WriteLine("CLI\t" + cliIssues.Count() + GetUntypedIssueCountString(cliIssues, validTypeLabels));

            //330852328       Functionality: Pack = Pack
            Label packLabel = await GetLabelById(330852328);
            IEnumerable<Issue> packIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == packLabel.Id));
            Console.WriteLine("Pack\t" + packIssues.Count() + GetUntypedIssueCountString(packIssues, validTypeLabels));

            //332553843       Functionality: Push = Push
            Label pushLabel = await GetLabelById(332553843);
            IEnumerable<Issue> pushIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == pushLabel.Id));
            Console.WriteLine("Push\t" + pushIssues.Count() + GetUntypedIssueCountString(pushIssues, validTypeLabels));

            //Restore
            //{
            //    345983287       Functionality: Restore
            //    1950335805      Area: RestoreCPVM
            //    630044219       Area: RestoreNoOp
            //    1243121573      Area: RestoreRepeatableBuild
            //    1790102601      Area: RestoreStaticGraph
            //    664611674       Area: RestoreTool
            //}
            Label restoreLabel1 = await GetLabelById(345983287);
            Label restoreLabel2 = await GetLabelById(1950335805);
            Label restoreLabel3 = await GetLabelById(630044219);
            Label restoreLabel4 = await GetLabelById(1243121573);
            Label restoreLabel5 = await GetLabelById(1790102601);
            Label restoreLabel6 = await GetLabelById(664611674);
            var restoreLabels = new long[] { restoreLabel1.Id, restoreLabel2.Id, restoreLabel3.Id, restoreLabel4.Id, restoreLabel5.Id, restoreLabel6.Id };
            IEnumerable<Issue> restoreIssues = includedIssues.Where(issue => issue.Labels.Any(label => restoreLabels.Contains(label.Id)));
            Console.WriteLine("Restore\t" + restoreIssues.Count() + GetUntypedIssueCountString(restoreIssues, validTypeLabels));

            // 702649610       Functionality: Signing = Signing / verification
            Label signingLabel = await GetLabelById(702649610);
            IEnumerable<Issue> signingIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == signingLabel.Id));
            Console.WriteLine("Signing\t" + signingIssues.Count() + GetUntypedIssueCountString(signingIssues, validTypeLabels));

            //Package Management(install/ uninstall / update)
            //{
            //    430514155       Functionality: Install
            //    336427001       Functionality: Update
            //}
            Label managementLabel1 = await GetLabelById(430514155);
            Label managementLabel2 = await GetLabelById(336427001);
            var managementLabels = new long[] { managementLabel1.Id, managementLabel2.Id };
            IEnumerable<Issue> managementIssues = includedIssues.Where(issue => issue.Labels.Any(label => managementLabels.Contains(label.Id)));
            Console.WriteLine("Package Management\t" + managementIssues.Count() + GetUntypedIssueCountString(managementIssues, validTypeLabels));

            //Search + List
            //{
            //    1498027322      Functionality: List(Search)
            //    723045456       Functionality: Search
            //}
            Label searchListLabel1 = await GetLabelById(1498027322);
            Label searchListLabel2 = await GetLabelById(723045456);
            var searchListLabels = new long[] { searchListLabel1.Id, searchListLabel2.Id };
            IEnumerable<Issue> searchListIssues = includedIssues.Where(issue => issue.Labels.Any(label => searchListLabels.Contains(label.Id)));
            Console.WriteLine("Search & List\t" + searchListIssues.Count() + GetUntypedIssueCountString(searchListIssues, validTypeLabels));

            //    723058565       Area: NewFrameworks = Core
            Label coreLabel = await GetLabelById(723058565);
            IEnumerable<Issue> coreIssues = includedIssues.Where(issue => issue.Labels.Any(label => label.Id == coreLabel.Id));
            Console.WriteLine("Core\t" + coreIssues.Count() + GetUntypedIssueCountString(coreIssues, validTypeLabels));
        }


        private async Task<Label> GetLabelById(long id)
        {
            if (_queryCache.AllHomeLabels is null)
            {
                _queryCache.AllHomeLabels = await LabelUtilities.GetLabelsForRepository(_client, "nuget", "home");
            }

            Label foundLabel = _queryCache.AllHomeLabels?.SingleOrDefault(l => l.Id == id);
            return foundLabel;
        }

        private static string GetUntypedIssueCountString(IEnumerable<Issue> issues, List<Label> validTypeLabels)
        {
            IEnumerable<long> validTypeLabelIds = validTypeLabels.Select(label => label.Id);
            IEnumerable<Issue> unlabeledIssues = issues.Where(issue => !issue.Labels.Any(label => validTypeLabelIds.Contains(label.Id)));
            var unlabeledCount = unlabeledIssues.Count();
            if (unlabeledCount > 0)
            {
                return " (Missing Types: " + unlabeledCount + ")";
            }
            return string.Empty;
        }
    }
}
