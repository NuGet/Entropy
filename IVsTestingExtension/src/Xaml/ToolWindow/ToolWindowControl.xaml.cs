using IVsTestingExtension.Xaml.ApiSelector;
using System;
using System.Windows.Controls;

namespace IVsTestingExtension.Xaml.ToolWindow
{
    public partial class ToolWindowControl : UserControl
    {
        private ToolWindowControlViewModel _model;

        public ToolWindowControl(ToolWindowControlViewModel model)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            _model = model;
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Frame.Navigate(new ApiSelectorView(_model.Projects, _model.JTF, _model.AsyncServiceProvider));
        }

    }
}
