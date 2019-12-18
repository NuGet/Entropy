using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Threading.Tasks;

namespace IVsTestingExtension
{
    public static class ServiceProviderExtensions
    {
        public static TService GetService<TService>(
            this IServiceProvider serviceProvider)
            where TService : class
        {
            return serviceProvider.GetService(typeof(TService)) as TService;
        }

        public static TInterface GetService<TService, TInterface>(
            this IServiceProvider serviceProvider)
            where TInterface : class
        {
            return serviceProvider.GetService(typeof(TService)) as TInterface;
        }

        public static Task<EnvDTE.DTE> GetDTEAsync(
            this Microsoft.VisualStudio.Shell.IAsyncServiceProvider site)
        {
            return site.GetServiceAsync<SDTE, EnvDTE.DTE>();
        }

        public static Task<IComponentModel> GetComponentModelAsync(
            this Microsoft.VisualStudio.Shell.IAsyncServiceProvider site)
        {
            return site.GetServiceAsync<SComponentModel, IComponentModel>();
        }

        public static async Task<TService> GetServiceAsync<TService>(
            this Microsoft.VisualStudio.Shell.IAsyncServiceProvider site)
            where TService : class
        {
            return await site.GetServiceAsync(typeof(TService)) as TService;
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
