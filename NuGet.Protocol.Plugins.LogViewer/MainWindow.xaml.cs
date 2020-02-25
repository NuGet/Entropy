// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace NuGet.Protocol.Plugins.LogViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AppendLog(string messages)
        {
            if (!string.IsNullOrWhiteSpace(messages))
            {
                logTextBlob.Text += messages + Environment.NewLine;
            }
        }

        private void ApplyDataBindings(IReadOnlyList<LogFileReadResult> results, IReadOnlyList<DataBinding> dataBindings)
        {
            var allJObjects = results.SelectMany(result => result.JObjects).ToArray();

            foreach (var dataBinding in dataBindings)
            {
                var dataTable = dataBinding.Processor.Process(allJObjects);

                if (dataTable.Columns.Count == 0)
                {
                    continue;
                }

                var view = dataTable.DefaultView;

                try
                {
                    view.Sort = dataBinding.SortColumns;
                }
                catch (IndexOutOfRangeException)
                {
                    AppendLog($"Grid {dataBinding.Grid.Name} does not have all sort columns:  {dataBinding.SortColumns}.");
                }

                dataBinding.Grid.DataContext = view;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();

            var dialog = new OpenFileDialog();

            dialog.Filter = "log files (*.log)|*.log|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.Multiselect = true;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    logFilesList.Items.Add(fileName);
                }

                if (logFilesList.Items.Count > 0)
                {
                    try
                    {
                        DisplayLogs();
                    }
                    catch (Exception ex)
                    {
                        AppendLog(ex.ToString());
                    }
                }
            }
        }

        private void DisplayLogs()
        {
            logTextBlob.Text = null;

            var logFiles = GetLogFiles();

            if (!logFiles.Any())
            {
                return;
            }

            var results = logFiles
                .Select(logFile => LogFileReader.Read(logFile))
                .ToArray();

            var dataBindings = new List<DataBinding>()
                {
                    new DataBinding(new MachineLogMessageProcessor(), machineGrid, sortColumns: "Now"),
                    new DataBinding(new ThreadPoolLogMessageProcessor(), threadPoolGrid, sortColumns: "Now"),
                    new DataBinding(new AssemblyLogMessageProcessor(), assemblyGrid, sortColumns: "Now"),
                    new DataBinding(new CommunicationLogMessageProcessor(), communicationGrid, sortColumns: "Sending"),
                    new DataBinding(new TaskLogMessageProcessor(), taskGrid, sortColumns: "Now"),
                    new DataBinding(new ProcessLogMessageProcessor(), processGrid, sortColumns: "Now"),
                    new DataBinding(new EnvironmentVariablesLogMessageProcessor(), environmentVariablesGrid, sortColumns: "Now"),
                    new DataBinding(new PluginInstanceLogMessageProcessor(), pluginInstanceGrid, sortColumns: "Now"),
                };

            ApplyDataBindings(results, dataBindings);

            foreach (var result in results)
            {
                AppendLog(result.Messages);
            }
        }

        private IReadOnlyList<FileInfo> GetLogFiles()
        {
            return logFilesList.Items
                .Cast<string>()
                .Select(filePath => new FileInfo(filePath))
                .ToArray();
        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Color":
                case "Relative Time In Ticks":
                    e.Cancel = true;
                    break;
            }
        }

        private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var context = (sender as FrameworkElement).DataContext;

            logFilesList.Items.Remove(context);
        }

        private void Reset()
        {
            logFilesList.Items.Clear();
        }

        private struct DataBinding
        {
            internal ILogMessageProcessor Processor { get; }
            internal DataGrid Grid { get; }
            internal string SortColumns { get; }

            internal DataBinding(ILogMessageProcessor processor, DataGrid grid, string sortColumns)
            {
                Processor = processor;
                Grid = grid;
                SortColumns = sortColumns;
            }
        }
    }
}