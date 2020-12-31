using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;

namespace IVsTestingExtension
{
    public static class ServiceProviderExtensions
    {
        public static Task<IComponentModel> GetComponentModelAsync(
            this Microsoft.VisualStudio.Shell.IAsyncServiceProvider site)
        {
            return site.GetServiceAsync<SComponentModel, IComponentModel>();
        }

        public static async Task<TInterface> GetServiceAsync<TService, TInterface>(
            this Microsoft.VisualStudio.Shell.IAsyncServiceProvider site)
            where TInterface : class
        {
            var service = await site.GetServiceAsync(typeof(TService));

            if (service != null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return service as TInterface;
            }

            return null;
        }
    }
}
