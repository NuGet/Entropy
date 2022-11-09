using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace IVsTestingExtension.Xaml.ApiInvoker
{
    public class ApiInvokeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = (FrameworkElement)container;

            var parameter = (ApiInvokeModel.Parameter)item;
            if (parameter.Type == typeof(Guid) || parameter.Type == typeof(EnvDTE.Project))
            {
                var template = (DataTemplate)element.FindResource("ProjectSelection");
                return template;
            }
            else if (parameter.Type == typeof(string))
            {
                var template = (DataTemplate)element.FindResource("String");
                return template;
            }
            else if (parameter.Type == typeof(CancellationToken))
            {
                var template = (DataTemplate)element.FindResource("CancellationToken");
                return template;
            }
            else if (parameter.Type == typeof(FrameworkName))
            {
                var template = (DataTemplate)element.FindResource("FrameworkName");
                return template;
            }
            else if (parameter.Type == typeof(Boolean))
            {
                var template = (DataTemplate)element.FindResource("Boolean");
                return template;
            }
            else
            {
                var template = (DataTemplate)element.FindResource("NotImplemented");
                return template;
            }
        }
    }
}
