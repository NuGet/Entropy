using IVsTestingExtension.Models;
using IVsTestingExtension.Xaml.ApiSelector;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace IVsTestingExtension.Xaml.ApiInvoker
{
    public class ApiInvokeModel
    {
        internal ApiInvokeModel(ApiMethod apiMethod)
        {
            var parameters = new ObservableCollection<Parameter>();
            foreach (var parameter in apiMethod.MethodInfo.GetParameters())
            {
                var p = new Parameter()
                {
                    Name = parameter.Name,
                    Type = parameter.ParameterType
                };
                parameters.Add(p);
            }

            Parameters = parameters;
            ApiMethod = apiMethod;
        }

        public ObservableCollection<Parameter> Parameters { get; set; }
        public IAsyncServiceProvider AsyncServiceProvider { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public JoinableTaskFactory JTF { get; set; }
        public Func<Task<object>> ServiceFactory { get; set; }
        internal ApiMethod ApiMethod { get; set; }

        public class Parameter : INotifyPropertyChanged
        {
            public string _name;
            public string Name
            {
                get => _name;
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    }
                }
            }

            private Type _type;
            public Type Type
            {
                get => _type;
                set
                {
                    if (_type != value)
                    {
                        _type = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Type)));
                    }
                }
            }

            private object _value;
            public object Value
            {
                get => _value; set
                {
                    if (_value != value)
                    {
                        _value = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
