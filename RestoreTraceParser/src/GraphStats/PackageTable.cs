using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RestoreTraceParser
{
    public sealed class PackageTableEntry
    {
        private List<int> _nodeCountByRun;

        public string LibraryName { get; }
        public List<int> NodeCountByRun { get { return _nodeCountByRun; } }

        public PackageTableEntry(string libraryName)
        {
            LibraryName = libraryName;
            _nodeCountByRun = [0];
        }

        public void SetRunIndex(int index)
        {
            if (index >= _nodeCountByRun.Count)
            {
                for(int i = _nodeCountByRun.Count; i <= index; i++)
                {
                    _nodeCountByRun.Add(0);
                }
            }
        }

        public void Increment(int index, int count)
        {
            if(index >= _nodeCountByRun.Count)
            {
                SetRunIndex(index);
            }
            _nodeCountByRun[index] += count;
        }

        public int GetNodeCount(int index)
        {
            return _nodeCountByRun[index];
        }
    }

    public sealed class PackageTable
    {
        private Dictionary<string, PackageTableEntry> _nodeCountByPackage = new Dictionary<string, PackageTableEntry>(StringComparer.OrdinalIgnoreCase);
        private int _runIndex = 0;

        public void IncrementBy(string libraryName, int count)
        {
            if (!_nodeCountByPackage.TryGetValue(libraryName, out PackageTableEntry entry))
            {
                entry = new PackageTableEntry(libraryName);
                _nodeCountByPackage.Add(libraryName, entry);
            }

            entry.Increment(_runIndex, count);
        }

        public int IncrementRunIndex()
        {
            int prevIndex = _runIndex++;
            for(int i=0; i<_nodeCountByPackage.Count; i++)
            {
                _nodeCountByPackage.Values
                    .ElementAt(i)
                    .SetRunIndex(_runIndex);
            }

            return prevIndex;
        }

        public int GetNodeCount(string libraryName, int index)
        {
            if (_nodeCountByPackage.TryGetValue(libraryName, out PackageTableEntry entry))
            {
                return entry.GetNodeCount(index);
            }
            else
            {
                return 0;
            }
        }

        public bool AllRunsIdentical()
        {
            bool ret = true;
            foreach (var library in _nodeCountByPackage)
            {
                int firstCount = library.Value.NodeCountByRun[0];
                foreach (var nodeCount in library.Value.NodeCountByRun)
                {
                    if (nodeCount != firstCount)
                    {
                        ret = false;
                        Console.WriteLine($"Package with different node counts: {library.Key}");
                    }
                }
            }

            return ret;
        }

        public void PrintTable(TextWriter writer)
        {
            writer.WriteCsvCell("Library");
            for (int i = 0; i <= _runIndex; i++)
            {
                writer.WriteCsvCell($"Run {i + 1}");
            }
            writer.WriteLine();

            foreach (var library in _nodeCountByPackage)
            {
                writer.WriteCsvCell(library.Key);
                for (int i = 0; i <= _runIndex; i++)
                {
                    writer.WriteCsvCell(library.Value.GetNodeCount(i));
                }
                writer.WriteLine();
            }
        }
    }
}
