/**
 *  Archiver - Cross platform, multi-destination backup and archiving utility
 * 
 *  Copyright (c) 2020-2021 Steve Cross <flip@foxhollow.cc>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Diagnostics;
using System.IO;
using System.Linq;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Classes.CSD;
using FoxHollow.Archiver.Shared.Utilities;

namespace FoxHollow.Archiver.CLI.Utilities.CSD
{
    public delegate void Scanner_ProgressChangedDelegate(long newFiles, long existingFiles, long excludedFiles, double filesPerSecond);
    public delegate void Scanner_CompleteDelegate();

    public class FileScanner
    {
        public event Scanner_CompleteDelegate OnComplete;
        public event Scanner_ProgressChangedDelegate OnProgressChanged;

        private const int _sampleDurationMs = 1000;
        private long _totalFileCount = 0;
        private long _lastSampleFileCount = 0;
        private Stopwatch _sw;
        private long _lastSample;
        private CsdScanStats _stats;

        public FileScanner(CsdScanStats stats)
        {
            _stats = stats ?? throw new System.ArgumentNullException(nameof(stats));
            _sw = new Stopwatch();

            this.OnComplete += delegate { };
            this.OnProgressChanged += delegate { };
        }

        public void ScanFiles()
        {
            _sw.Start();

            foreach (string dirtySourcePath in SysInfo.Config.CSD.SourcePaths)
            {
                string sourcePath = PathUtils.CleanPath(dirtySourcePath);

                ScanDirectory(dirtySourcePath);
            }

            _sw.Stop();
            OnComplete();
        }

        private void ScanDirectory(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");

            try {
                foreach (string dir in Directory.GetDirectories(sourcePath))
                {
                    string cleanDir = PathUtils.CleanPath(dir);

                    if (!(SysInfo.Config.CSD.ExcludePaths.Any(x => cleanDir.ToLower().StartsWith(x.ToLower()))))
                        ScanDirectory(dir);
                }
            }
            catch (IOException)
            {
                return;
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                _totalFileCount++;

                string cleanFile = PathUtils.CleanPath(file);
                string fileName = PathUtils.GetFileName(cleanFile);

                if (SysInfo.Config.CSD.ExcludePaths.Any(x => cleanFile.ToLower().StartsWith(x.ToLower())))
                    _stats.ExcludedFileCount++;

                else if (SysInfo.Config.CSD.ExcludeFiles.Any(x => fileName.ToLower().EndsWith(x.ToLower())))
                    _stats.ExcludedFileCount++;

                else
                    new CsdSourceFile(_stats, cleanFile);

                if (_sw.ElapsedMilliseconds - _lastSample > _sampleDurationMs)
                {
                    long elapsedSinceLastSample = _sw.ElapsedMilliseconds - _lastSample;
                    long filesSinceLastSample = _totalFileCount - _lastSampleFileCount;

                    double filesPerSecond = (double)filesSinceLastSample / ((double)elapsedSinceLastSample / 1000.0);

                    OnProgressChanged(_stats.NewFileCount, _stats.ExistingFileCount, _stats.ExcludedFileCount, filesPerSecond);

                    _lastSample = _sw.ElapsedMilliseconds;
                    _lastSampleFileCount = _totalFileCount;
                }
            }
        }
    }
}