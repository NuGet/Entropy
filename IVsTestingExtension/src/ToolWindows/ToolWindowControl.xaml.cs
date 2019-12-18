using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IVsTestingExtension.ToolWindows
{
    public partial class ToolWindowControl : UserControl
    {

        private ProjectCommandTestingModel _model;

        public ToolWindowControl(ProjectCommandTestingModel model)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            _model = model;
            InitializeComponent();
            Arguments.DataContext = _model;
            ProjectName.DataContext = _model;
            Affinity.DataContext = _model;
            Affinity.ItemsSource = Enum.GetValues(typeof(ThreadAffinity)).Cast<ThreadAffinity>();
            Affinity.SelectedItem = ThreadAffinity.SYNC_JTF_RUN;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                _model.Clicked();
            }
            catch
            {
                // do nothing
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods - UI events need to be void.
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await _model.ClickedAsync();
            }
            catch
            {
                // do nothing
            }
        }
    }
}
