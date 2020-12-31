using System;
using System.Collections.ObjectModel;
using IVsTestingExtension.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;

namespace IVsTestingExtension.Xaml.ToolWindow
{
    public class ToolWindowControlViewModel
    {
        private SolutionEventHandler _solutionEventHandler;

        public ToolWindowControlViewModel(IVsSolution vsSolution, IAsyncServiceProvider asyncServiceProvider, JoinableTaskFactory jtf)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Projects = new ObservableCollection<TargetProject>();
            _solutionEventHandler = new SolutionEventHandler(Projects, vsSolution);

            ErrorHandler.ThrowOnFailure(vsSolution.AdviseSolutionEvents(_solutionEventHandler, out _));

            AsyncServiceProvider = asyncServiceProvider;
            JTF = jtf;
        }

        internal ObservableCollection<TargetProject> Projects { get; }

        internal IAsyncServiceProvider AsyncServiceProvider { get; }
        internal JoinableTaskFactory JTF { get; }

        private class SolutionEventHandler : IVsSolutionEvents, IVsSolutionEvents2, IVsSolutionEvents3, IVsSolutionEvents4
        {
            private ObservableCollection<TargetProject> _projects;
            private IVsSolution _vsSolution;
            private bool solutionBulkOperation = false;

            public SolutionEventHandler(ObservableCollection<TargetProject> projects, IVsSolution vsSolution)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _projects = projects;
                _vsSolution = vsSolution;

                Guid ignored = Guid.Empty;
                ErrorHandler.ThrowOnFailure(_vsSolution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref ignored, out IEnumHierarchies enumHierarchies));

                uint projectCount;
                var vsProjects = new IVsHierarchy[1];
                ErrorHandler.ThrowOnFailure(enumHierarchies.Next(1, vsProjects, out projectCount));

                while (projectCount > 0)
                {
                    ErrorHandler.ThrowOnFailure(AddProject(vsProjects[0]));
                    ErrorHandler.ThrowOnFailure(enumHierarchies.Next(1, vsProjects, out projectCount));
                }
            }

            private int AddProject(IVsHierarchy pHierarchy)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Guid guid;
                var hr = _vsSolution.GetGuidOfProject(pHierarchy, out guid);

                if (hr != VSConstants.S_OK)
                {
                    return hr;
                }

                var asdfs = pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out object pvar);
                if (asdfs == VSConstants.S_OK)
                {
                    var dteProj = pvar as EnvDTE.Project;
                    if (dteProj != null)
                    {
                        ErrorHandler.ThrowOnFailure(pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out object oName));
                        var projectName = (string)oName;

                        var project = new TargetProject(projectName, guid, dteProj);
                        _projects.Add(project);
                        return VSConstants.S_OK;
                    }
                }

                return VSConstants.E_UNEXPECTED;
            }

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return AddProject(pHierarchy);
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                if (!solutionBulkOperation)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    Guid guid;
                    var hr = _vsSolution.GetGuidOfProject(pHierarchy, out guid);
                    if (hr != VSConstants.S_OK)
                    {
                        return hr;
                    }

                    for (int i = 0; i < _projects.Count; i++)
                    {
                        if (_projects[i].Guid == guid)
                        {
                            _projects.RemoveAt(i);
                            break;
                        }
                    }
                }

                return VSConstants.S_OK;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                solutionBulkOperation = true;
                _projects.Clear();

                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                solutionBulkOperation = false;
                return VSConstants.S_OK;
            }

            public int OnAfterMergeSolution(object pUnkReserved)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterRenameProject(IVsHierarchy pHierarchy)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Guid guid;
                var hr = _vsSolution.GetGuidOfProject(pHierarchy, out guid);
                if (hr != VSConstants.S_OK)
                {
                    return hr;
                }

                for (int i = 0; i < _projects.Count; i++)
                {
                    var proj = _projects[i];
                    if (proj.Guid == guid)
                    {
                        hr = pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out object pvar);
                        if (hr != VSConstants.S_OK)
                        {
                            return hr;
                        }
                        proj.Name = (string)pvar;
                        break;
                    }
                }

                return VSConstants.S_OK;
            }

            public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.S_OK;
            }
        }
    }
}
