using IVsTestingExtension.Models;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio;
using NuGet.VisualStudio.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace IVsTestingExtension.Xaml.ApiSelector
{
    internal class ApiSelectorViewModel
    {
        public ApiSelectorViewModel(ObservableCollection<TargetProject> projects, IAsyncServiceProvider asyncServiceProvider)
        {
            Interfaces = new Builder()
                .WithService<INuGetProjectService>()
                    .FromBrokeredService(asyncServiceProvider, NuGetServices.NuGetProjectServiceV1)
                    .WithAllMethods()
                    .BuildService()

                // COM interfaces and EmbedInterop breaks reflection, so we can't use WithAllMethods()
                .WithService<IVsPackageInstallerServices>()
                    .FromMEF(asyncServiceProvider)
                    .WithMethod(s => s.GetInstalledPackages())
                    .WithMethod(s => s.GetInstalledPackages(It.IsAny<EnvDTE.Project>()))
                    .WithMethod(s => s.IsPackageInstalled(It.IsAny<EnvDTE.Project>(), It.IsAny<string>()))
                    .WithMethod(s => s.IsPackageInstalledEx(It.IsAny<EnvDTE.Project>(), It.IsAny<string>(), It.IsAny<string>()))
                    .BuildService()
                .WithService<IVsFrameworkCompatibility>()
                    .FromMEF(asyncServiceProvider)
                    .WithMethod(s => s.GetFrameworksSupportingNetStandard(It.IsAny<FrameworkName>()))
                    .WithMethod(s => s.GetNearest(It.IsAny<FrameworkName>(), It.IsAny<IEnumerable<FrameworkName>>()))
                    .WithMethod(s => s.GetNetStandardFrameworks())
                    .BuildService()
                .WithService<IVsFrameworkCompatibility2>()
                    .FromMEF(asyncServiceProvider)
                    .WithMethod(s => s.GetNearest(It.IsAny<FrameworkName>(), It.IsAny<IEnumerable<FrameworkName>>(), It.IsAny<IEnumerable<FrameworkName>>()))
                    .BuildService()
                .WithService<IVsFrameworkCompatibility3>()
                    .FromMEF(asyncServiceProvider)
                    .WithMethod(s => s.GetNearest(It.IsAny<IVsNuGetFramework>(), It.IsAny<IEnumerable<IVsNuGetFramework>>()))
                    .WithMethod(s => s.GetNearest(It.IsAny<IVsNuGetFramework>(), It.IsAny<IEnumerable<IVsNuGetFramework>>(), It.IsAny<IEnumerable<IVsNuGetFramework>>()))
                    .BuildService()
                .WithService<IVsFrameworkParser>()
                    .FromMEF(asyncServiceProvider)
                    .WithMethod(s => s.GetShortFrameworkName(It.IsAny<FrameworkName>()))
                    .WithMethod(s => s.ParseFrameworkName(It.IsAny<string>()))
                    .BuildService()
                .Build();

            Projects = projects;
        }

        public IReadOnlyList<ApiService> Interfaces { get; }

        public ObservableCollection<TargetProject> Projects { get; }

        public JoinableTaskFactory JTF { get; set; }
        public IAsyncServiceProvider AsyncServiceProvider { get; set; }

        private class Builder
        {
            private List<ApiService> _services = new List<ApiService>();

            public ServiceBuilder<T> WithService<T>()
                where T : class
            {
                return new ServiceBuilder<T>(this);
            }

            public Builder AddService(ApiService service)
            {
                int index = 0;
                while (index < _services.Count && StringComparer.Ordinal.Compare(service.Name, _services[index].Name) > 0)
                {
                    index++;
                }

                if (index < _services.Count)
                {
                    _services.Insert(index, service);
                }
                else
                {
                    _services.Add(service);
                }

                return this;
            }

            public IReadOnlyList<ApiService> Build()
            {
                return _services;
            }
        }

        private class ServiceBuilder<T>
            where T : class
        {
            private Builder _builder;
#pragma warning disable ISB001 // Dispose of proxies
            private Func<Task<object>> _factory;
#pragma warning restore ISB001 // Dispose of proxies
            private List<MethodInfo> _methods = new List<MethodInfo>();

            public ServiceBuilder(Builder builder)
            {
                _builder = builder;
            }

            public Builder BuildService()
            {
                if (_factory == null)
                {
                    throw new InvalidOperationException("Factory way not provided");
                }

                if (_methods.Count == 0)
                {
                    throw new InvalidOperationException("No methods were added");
                }

                var apiService = new ApiService(typeof(T).FullName, _methods, _factory);
                _builder.AddService(apiService);

                return _builder;
            }

            public ServiceBuilder<T> WithAllMethods()
            {
                var type = typeof(T);
                var methods = type.GetMethods();

                foreach (var methodInfo in methods)
                {
                    AddMethodInfo(methodInfo);
                }

                return this;
            }

            public ServiceBuilder<T> WithMethod(Expression<Action<T>> expression)
            {
                var methodCallExpression = (MethodCallExpression)expression.Body;
                var methodInfo = methodCallExpression.Method;

                AddMethodInfo(methodInfo);

                var declaringType = methodInfo.DeclaringType;
                var typeMethods = declaringType.GetMethods();

                return this;
            }

            private void AddMethodInfo(MethodInfo methodInfo)
            {
                int index = 0;
                var name = methodInfo.ToString();
                while (index < _methods.Count && StringComparer.OrdinalIgnoreCase.Compare(name, _methods[index].Name) > 0)
                {
                    index++;
                }

                if (index < _methods.Count)
                {
                    _methods.Insert(index, methodInfo);
                }
                else
                {
                    _methods.Add(methodInfo);
                }
            }

            public ServiceBuilder<T> FromMEF(IAsyncServiceProvider asyncServiceProvider)
            {
                _factory = async () =>
                {
                    var componentModel = await asyncServiceProvider.GetServiceAsync<SComponentModel, IComponentModel>();
                    return componentModel.GetService<T>();
                };

                return this;
            }

            public ServiceBuilder<T> FromBrokeredService(IAsyncServiceProvider asyncServiceProvider, ServiceRpcDescriptor serviceRpcDescriptor)
            {
#pragma warning disable ISB001 // Dispose of proxies
                _factory = async () =>
#pragma warning restore ISB001 // Dispose of proxies
                {
                    var brokeredServiceContainer = await asyncServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                    var serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();

                    return await serviceBroker.GetProxyAsync<INuGetProjectService>(serviceRpcDescriptor);
                };

                return this;
            }
        }

        private static class It
        {
            public static T IsAny<T>()
            {
                throw new NotImplementedException();
            }
        }
    }
}
