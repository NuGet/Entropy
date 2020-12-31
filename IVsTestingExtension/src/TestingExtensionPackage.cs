using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IVsTestingExtension.Xaml.ToolWindow;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace IVsTestingExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("IVs Testing Extension", "Helps test the IVs APIs in Visual Studio by invoking on different threading contexts.", "1.0")]
    [ProvideToolWindow(typeof(CommandInvokingWindow), Style = VsDockStyle.Tabbed, DockedWidth = 300, Window = "DocumentWell", Orientation = ToolWindowOrientation.Left)]
    [Guid("5A86D30C-B4EC-4537-8B81-A422058075CE")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class TestingExtensionPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            var componentModel = await this.GetComponentModelAsync();
            componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowToolWindow.InitializeAsync(this);
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            return toolWindowType.Equals(Guid.Parse(CommandInvokingWindow.WindowGuidString)) ? this : null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            return toolWindowType == typeof(CommandInvokingWindow) ? CommandInvokingWindow.Title : base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var vsSolution = (IVsSolution)await GetServiceAsync(typeof(SVsSolution));
            var model = new ToolWindowControlViewModel(vsSolution, this, JoinableTaskFactory);
            return model;
        }
    }
}
