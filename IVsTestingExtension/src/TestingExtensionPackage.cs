using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IVsTestingExtension.Tests;
using IVsTestingExtension.ToolWindows;
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
        [Import]
        ITestMethodProvider TestMethodFactory { get; set; }

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
            var dte = await this.GetDTEAsync();
            var model = new ProjectCommandTestingModel(dte, TestMethodFactory.GetMethod());
            await model.InitializeAsync();
            return model;
        }
    }
}
