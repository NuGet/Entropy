using IVsTestingExtension.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace IVsTestingExtension.Xaml.ApiInvoker
{
    internal class ApiInvokeViewModel : INotifyPropertyChanged
    {
        public ApiInvokeViewModel(ApiInvokeModel model, ObservableCollection<TargetProject> projects)
        {
            Model = model;
            Projects = projects;
        }

        public ApiInvokeModel Model { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private TargetProject _selectedProject;
        public TargetProject SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (value != _selectedProject)
                {
                    _selectedProject = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedProject)));
                }
            }
        }

        private ObservableCollection<TargetProject> _projects;
        public ObservableCollection<TargetProject> Projects
        {
            get => _projects;
            set
            {
                if (value != _projects)
                {
                    _projects = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Projects)));
                }
            }
        }

        private bool _runOnUiThread;
        public bool RunOnUiThread
        {
            get => _runOnUiThread;
            set
            {
                if (value != _runOnUiThread)
                {
                    _runOnUiThread = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RunOnUiThread)));
                }
            }
        }

        private bool _freeThreadedCheck;
        public bool FreeThreadedCheck
        {
            get => _freeThreadedCheck;
            set
            {
                if (_freeThreadedCheck != value)
                {
                    _freeThreadedCheck = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FreeThreadedCheck)));
                }
            }
        }
    }
}
