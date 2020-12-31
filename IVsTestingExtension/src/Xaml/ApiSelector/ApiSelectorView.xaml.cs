using IVsTestingExtension.Models;
using IVsTestingExtension.Xaml.ApiInvoker;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace IVsTestingExtension.Xaml.ApiSelector
{
    partial class ApiSelectorView : Page
    {
        private ApiSelectorViewModel _viewModel;

        internal ApiSelectorView(ObservableCollection<TargetProject> projects, JoinableTaskFactory jtf, IAsyncServiceProvider asyncServiceProvider)
        {
            InitializeComponent();
            DataContext = _viewModel = new ApiSelectorViewModel(projects, asyncServiceProvider);
            _viewModel.AsyncServiceProvider = asyncServiceProvider;
            _viewModel.JTF = jtf;
        }

        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = _treeView.SelectedItem;
            if (selectedItem is ApiMethod apiMethod)
            {
                e.Handled = true;

                var model = new ApiInvokeModel(apiMethod);
                model.JTF = _viewModel.JTF;
                model.AsyncServiceProvider = _viewModel.AsyncServiceProvider;

                var page = new ApiInvokerView(model, _viewModel.Projects);
                this.NavigationService.Navigate(page, _viewModel.Projects);
            }
        }
    }
}
