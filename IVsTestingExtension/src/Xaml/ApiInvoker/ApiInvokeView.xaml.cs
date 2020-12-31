using IVsTestingExtension.Models;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IVsTestingExtension.Xaml.ApiInvoker
{
    public partial class ApiInvokerView : Page
    {
        internal ApiInvokeViewModel ViewModel { get; set; }

        internal ApiInvokerView(ApiInvokeModel model, ObservableCollection<TargetProject> projects)
        {
            InitializeComponent();

            DataContext = ViewModel = new ApiInvokeViewModel(model, projects);

            this.Resources.Add("Projects", projects);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            _results.Text = "Calling method";
            ViewModel.Model.JTF.RunAsync(async delegate
            {
                string resultText;
                try
                {
                    var parameters = new object[ViewModel.Model.Parameters.Count];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var paramModel = ViewModel.Model.Parameters[i];
                        if (paramModel.Type == typeof(Guid))
                        {
                            var project = (TargetProject)paramModel.Value;
                            parameters[i] = project?.Guid;
                        }
                        else if (paramModel.Type == typeof(EnvDTE.Project))
                        {
                            var project = (TargetProject)paramModel.Value;
                            parameters[i] = project?.DteProject;
                        }
                        else if (paramModel.Type == typeof(CancellationToken))
                        {
                            parameters[i] = CancellationToken.None;
                        }
                        else if (paramModel.Type == paramModel.Value.GetType())
                        {
                            parameters[i] = paramModel.Value;
                        }
                        else
                        {
                            throw new Exception(nameof(RunButton_Click) + " doesn't support parameter type " + paramModel.Type.FullName);
                        }
                    }

                    var service = await ViewModel.Model.ApiMethod.Service.Factory();
                    using (service as IDisposable)
                    {
                        object result;
                        if (ViewModel.FreeThreadedCheck)
                        {
                            await ViewModel.Model.JTF.SwitchToMainThreadAsync();
                            result = ViewModel.Model.JTF.Run(async delegate
                            {
                                using (ViewModel.Model.JTF.Context.SuppressRelevance())
                                {
                                    return await Task.Run(async () =>
                                    {
                                        var value = ViewModel.Model.ApiMethod.MethodInfo.Invoke(service, parameters);
                                        if (value is Task t)
                                        {
                                            await t;
                                        }
                                        return value;
                                    });
                                }
                            });
                        }
                        else
                        {
                            if (ViewModel.RunOnUiThread)
                            {
                                await ViewModel.Model.JTF.SwitchToMainThreadAsync();
                            }
                            else
                            {
                                await TaskScheduler.Default;
                            }
                            result = ViewModel.Model.ApiMethod.MethodInfo.Invoke(service, parameters);
                        }

                        if (result is Task task)
                        {
                            await task;

                            var resultProperty = result.GetType().GetProperty("Result");
                            if (resultProperty != null)
                            {
                                result = resultProperty.GetValue(result);
                            }
                        }

                        resultText = JsonConvert.SerializeObject(result, Formatting.Indented);
                    }
                }
                catch (Exception exception)
                {
                    resultText = exception.ToString();
                }

                await ViewModel.Model.JTF.SwitchToMainThreadAsync();

                _results.Text = resultText;
            });
        }
    }
}
