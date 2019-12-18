
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace IVsTestingExtension.ToolWindows
{
    public class ProjectCommandTestingModel : INotifyPropertyChanged
    {
        private string _arguments;
        private HashSet<string> _projects;
        private string _projectName;
        private ThreadAffinity _threadAffinity;

        private readonly DTE dte;
        private SolutionEvents solutionEvents; // We need a reference to SolutionEvents to avoid getting GC'ed
        // We don't really handle the no solution case so well :) 

        private readonly Func<Project, Dictionary<string, string>, System.Threading.Tasks.Task> testMethodAsync;

        public ProjectCommandTestingModel(DTE _dte, Func<Project, Dictionary<string, string>, System.Threading.Tasks.Task> _testMethodAsync)
        {
            testMethodAsync = _testMethodAsync ?? throw new ArgumentNullException(nameof(_testMethodAsync));
            dte = _dte ?? throw new ArgumentNullException(nameof(_dte));
        }

        internal async System.Threading.Tasks.Task InitializeAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            solutionEvents = dte.Events.SolutionEvents;
            solutionEvents.Opened += OnLoad;
            solutionEvents.BeforeClosing += OnSolutionClosing;
            solutionEvents.ProjectAdded += OnEnvDTEProjectAdded;
            solutionEvents.ProjectRemoved += OnEnvDTEProjectRemoved;
            solutionEvents.ProjectRenamed += OnEnvDTEProjectRenamed;
            OnLoad();
        }

        private Dictionary<string, string> GetArgumentDictionary()
        {
            var dictionary = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Arguments))
            {
                var allArgs = Arguments.Split(';');
                foreach (var arg in allArgs)
                {
                    var kvp = arg.Trim().Split('=');
                    if (kvp.Length == 2) // We ignore bad arguments...tough luck. No need for extra validation. :)
                    {
                        dictionary.Add(kvp[0].Trim(), kvp[1].Trim());
                    }
                }
            }
            return dictionary;
        }

        public void Clicked()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project projectSelected = GetSelectedProject();

            switch (ThreadAffinity)
            {
                case ThreadAffinity.SYNC_JTF_RUN: // deadlock
                    {
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await testMethodAsync(projectSelected, GetArgumentDictionary());
                        });
                        break;
                    }
                case ThreadAffinity.SYNC_TASKRUN_UNAWAITED: // no deadlock usually.
                    {
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await testMethodAsync(projectSelected, GetArgumentDictionary());
                        });
                        break;
                    }
                case ThreadAffinity.SYNC_TASKRUN_BLOCKING: // deadlock
                    {
                        var task = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await testMethodAsync(projectSelected, GetArgumentDictionary());
                        });

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - Done on purpose. This should deadlock in methods that are not free threaded. 
                        task.Wait(CancellationToken.None);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        break;
                    }
                case ThreadAffinity.SYNC_JTF_RUNASYNC_FIRE_FORGET:
                    {
                        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                        {
                            await testMethodAsync(projectSelected, GetArgumentDictionary());
                        });
                        break;
                    }
                default:
                    break;
            }
        }

        public async System.Threading.Tasks.Task ClickedAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Project projectSelected = GetSelectedProject();
            switch (ThreadAffinity)
            {
                case ThreadAffinity.ASYNC_FROM_UI:
                    {
                        await testMethodAsync(projectSelected, GetArgumentDictionary());
                        break;
                    }
                case ThreadAffinity.ASYNC_FROM_BACKGROUND:
                    {
                        await TaskScheduler.Default;

                        await testMethodAsync(projectSelected, GetArgumentDictionary());
                        break;
                    }
                case ThreadAffinity.ASYNC_FREETHREADED_CHECK:
                    {
                        // this test only catches issues when it blocks the UI thread.
                        ThreadHelper.JoinableTaskFactory.Run(async delegate
                        {
                            using (ThreadHelper.JoinableTaskFactory.Context.SuppressRelevance())
                            {
                                await System.Threading.Tasks.Task.Run(async delegate
                                {
                                    await testMethodAsync(projectSelected, GetArgumentDictionary());
                                });
                            }
                        });
                        break;
                    }
                default:
                    break;
            }
        }

        private Project GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solution = (SolutionClass)dte.Solution;
            Project projectSelected = null;

            foreach (Project project in solution.Projects)
            {
                if (project.Name.Equals(ProjectName))
                {
                    projectSelected = project;
                    break;
                }
            }
            return projectSelected;
        }

        public IEnumerable<string> Projects
        {
            get => _projects;
            set
            {
                if (value != null)
                {
                    _projects = new HashSet<string>(value);
                }
                else
                {
                    _projects = new HashSet<string>();
                }

                OnPropertyChanged("Projects");
            }
        }

        public ThreadAffinity ThreadAffinity
        {
            get => _threadAffinity;
            set
            {
                _threadAffinity = value;
                OnPropertyChanged("ThreadAffinity");
            }
        }

        public string Arguments
        {
            get => _arguments;
            set
            {
                _arguments = value;
                OnPropertyChanged("Arguments");
            }
        }

        public string ProjectName
        {
            get => _projectName;
            set
            {
                _projectName = value;
                OnPropertyChanged("ProjectName");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnSolutionClosing()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _projects?.Clear();
            Projects = _projects;
            UpdateProjectName();
        }

        private void OnLoad()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projects = new HashSet<string>();
            foreach (Project project in dte.Solution.Projects)
            {
                projects.Add(project.Name);
            }
            _projects = projects;
            Projects = _projects;
            UpdateProjectName();
        }

        private void OnEnvDTEProjectRenamed(Project Project, string OldName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _projects.Remove(OldName);
            _projects.Add(Project.Name);
            Projects = _projects;
            UpdateProjectName();
        }

        private void OnEnvDTEProjectRemoved(Project Project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _projects.Remove(Project.Name);
            Projects = _projects;
            UpdateProjectName();
        }

        private void OnEnvDTEProjectAdded(Project Project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _projects.Add(Project.Name);
            Projects = _projects;
            UpdateProjectName();
        }

        private void UpdateProjectName()
        {
            if (string.IsNullOrEmpty(_projectName))
            {
                if (_projects?.Count > 0)
                {
                    ProjectName = _projects.First();
                }
            }
            else
            {
                if (!_projects.Contains(_projectName))
                {
                    ProjectName = _projects.First();
                }
            }
        }
    }
}
