using System;
using System.ComponentModel;

namespace IVsTestingExtension.Models
{
    internal class TargetProject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public TargetProject(string name, Guid guid, EnvDTE.Project dteProject)
        {
            Name = name;
            Guid = guid;
            DteProject = dteProject;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        public Guid Guid { get; }

        public EnvDTE.Project DteProject { get; }
    }
}
