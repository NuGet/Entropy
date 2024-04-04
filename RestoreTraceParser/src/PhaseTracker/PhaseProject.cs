using Microsoft.Diagnostics.Tracing;

namespace RestoreTraceParser
{
    internal class PhaseProject
    {
        private double _restoreProjectStartTimeStamp;
        private double _restoreProjectStopTimeStamp;

        private double _calculateAndWriteDependencySpecStartTimeStamp;
        private double _calculateAndWriteDependencySpecStopTimeStamp;

        private double _createRestoreGraphStartTimeStamp;
        private double _createRestoreGraphStopTimeStamp;

        private double _buildAssetsFileStartTimeStamp;
        private double _buildAssetsFileStopTimeStamp;

        private double _commitAsyncStartTimeStamp;
        private double _commitAsyncStopTimeStamp;

        private double _writeCacheFileStartTimeStamp;
        private double _writeCacheFileStopTimeStamp;

        private double _writeDgSpecFileStartTimeStamp;
        private double _writeDgSpecFileStopTimeStamp;

        private double _writeLockFileStartTimeStamp;
        private double _writeLockFileStopTimeStamp;

        private double _writePackagesLockFileStartTimeStamp;
        private double _writePackagesLockFileStopTimeStamp;

        public double RestoreTime
        {
            get { return _restoreProjectStopTimeStamp - _restoreProjectStartTimeStamp; }
        }

        public double CalculateAndWriteDependencySpecTime
        {
            get { return _calculateAndWriteDependencySpecStopTimeStamp - _calculateAndWriteDependencySpecStartTimeStamp; }
        }

        public double CreateRestoreGraphTime
        {
            get { return _createRestoreGraphStopTimeStamp - _createRestoreGraphStartTimeStamp; }
        }

        public double BuildAssetsFileTime
        {
            get { return _buildAssetsFileStopTimeStamp - _buildAssetsFileStartTimeStamp; }
        }

        public double CommitAsyncTime
        {
            get { return _commitAsyncStopTimeStamp - _commitAsyncStartTimeStamp; }
        }

        public double WriteCacheFileTime
        {
            get { return _writeCacheFileStopTimeStamp - _writeCacheFileStartTimeStamp; }
        }

        public double WriteDgSpecFileTime
        {
            get { return _writeDgSpecFileStopTimeStamp - _writeDgSpecFileStartTimeStamp; }
        }

        public double WriteLockFileTime
        {
            get { return _writeLockFileStopTimeStamp - _writeLockFileStartTimeStamp; }
        }

        public double WritePackagesLockFileTime
        {
            get { return _writePackagesLockFileStopTimeStamp - _writePackagesLockFileStartTimeStamp; }
        }

        public void OnRestoreProjectStart(TraceEvent data)
        {
            _restoreProjectStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnRestoreProjectStop(TraceEvent data)
        {
            _restoreProjectStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCalculateAndWriteDependencySpecStart(TraceEvent data)
        {
            _calculateAndWriteDependencySpecStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCalculateAndWriteDependencySpecStop(TraceEvent data)
        {
            _calculateAndWriteDependencySpecStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCreateRestoreGraphStart(TraceEvent data)
        {
            _createRestoreGraphStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCreateRestoreGraphStop(TraceEvent data)
        {
            _createRestoreGraphStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnBuildAssetsFileStart(TraceEvent data)
        {
            _buildAssetsFileStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnBuildAssetsFileStop(TraceEvent data)
        {
            _buildAssetsFileStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCommitAsyncStart(TraceEvent data)
        {
            _commitAsyncStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnCommitAsyncStop(TraceEvent data)
        {
            _commitAsyncStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteCacheFileStart(TraceEvent data)
        {
            _writeCacheFileStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteCacheFileStop(TraceEvent data)
        {
            _writeCacheFileStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteDgSpecFileStart(TraceEvent data)
        {
            _writeDgSpecFileStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteDgSpecFileStop(TraceEvent data)
        {
            _writeDgSpecFileStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteLockFileStart(TraceEvent data)
        {
            _writeLockFileStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWriteLockFileStop(TraceEvent data)
        {
            _writeLockFileStopTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWritePackagesLockFileStart(TraceEvent data)
        {
            _writePackagesLockFileStartTimeStamp = data.TimeStampRelativeMSec;
        }

        public void OnWritePackagesLockFileStop(TraceEvent data)
        {
            _writePackagesLockFileStopTimeStamp = data.TimeStampRelativeMSec;
        }
    }
}
