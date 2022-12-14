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
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.Archiver.Shared;
using FoxHollow.Archiver.Shared.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace FoxHollow.Archiver.Shared.Utilities.Disc
{
    public class FileScanner
    {
        public delegate void ProgressChangedDelegate(DiscScanStats stats);

        public event ProgressChangedDelegate OnProgressChanged;

        private const int _sampleDurationMs = 250;
        private Stopwatch _sw;
        private long _lastSample;
        private DiscScanStats _stats;

        public FileScanner(DiscScanStats stats)
        {
            _stats = stats ?? throw new System.ArgumentNullException(nameof(stats));
            _sw = new Stopwatch();

            this.OnProgressChanged += delegate { };
        }

        public Task ScanFilesAsync(CancellationToken cToken)
            => Task.Run(() => ScanFiles(cToken));

        public void ScanFiles(CancellationToken cToken = default)
        {
            _sw.Start();

            foreach (string dirtySourcePath in SysInfo.Config.Disc.SourcePaths)
            {
                string sourcePath = PathUtils.CleanPath(dirtySourcePath);

                ScanDirectory(dirtySourcePath, cToken);

                if (cToken.IsCancellationRequested)
                    break;
            }

            this.OnProgressChanged(_stats);

            _sw.Stop();
        }

        private void ScanDirectory(string sourcePath, CancellationToken cToken = default)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");

            foreach (string dir in Directory.GetDirectories(sourcePath))
            {
                if (cToken.IsCancellationRequested)
                    return;

                string cleanDir = PathUtils.CleanPath(dir);

                if (!(SysInfo.Config.Disc.ExcludePaths.Any(x => cleanDir.ToLower().StartsWith(x.ToLower()))))
                    ScanDirectory(dir, cToken);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                if (cToken.IsCancellationRequested)
                    return;

                string cleanFile = PathUtils.CleanPath(file);

                if (SysInfo.Config.Disc.ExcludePaths.Any(x => cleanFile.ToLower().StartsWith(x.ToLower())))
                    _stats.ExcludedFileCount++;

                else if (SysInfo.Config.Disc.ExcludeFiles.Any(x => PathUtils.GetFileName(cleanFile).ToLower().EndsWith(x.ToLower())))
                    _stats.ExcludedFileCount++;

                else
                    new DiscSourceFile(_stats, cleanFile);

                if (_sw.ElapsedMilliseconds - _lastSample > _sampleDurationMs)
                {
                    this.OnProgressChanged(_stats);
                    _lastSample = _sw.ElapsedMilliseconds;
                }
            }
        }
    }
}