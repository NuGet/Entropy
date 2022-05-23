using System.Threading.Tasks;

namespace GithubIssueTagger.Reports
{
    internal interface IReport
    {
        Task RunAsync();
    }
}
