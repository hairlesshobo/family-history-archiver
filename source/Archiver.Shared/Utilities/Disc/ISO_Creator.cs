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

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FoxHollow.Archiver.Shared.Utilities.Disc
{

    public class ISO_Creator
    {
        public delegate void ProgressChangedDelegate(double currentPercent);

        public event ProgressChangedDelegate OnProgressChanged;

        private string _discName;
        private string _sourceDirectory;
        private string _destinationPath;

        public ISO_Creator(string discName, string sourceDirectory, string destinationPath)
        {
            _discName = discName;
            _sourceDirectory = sourceDirectory;
            _destinationPath = destinationPath;

            this.OnProgressChanged += delegate { };
        }

        public Task CreateIsoAsync(CancellationToken cToken)
            => Task.Run(() => CreateISO(cToken));

        public void CreateISO(CancellationToken cToken = default)
        {
            string procArgs = $" --burn-data -folder[\\]:\"{PathUtils.DirtyPath(_sourceDirectory)}\" -name:\"{_discName}\" -udf:2.5 -iso:\"{PathUtils.DirtyPath(_destinationPath)}\"";
            string processPath = PathUtils.ResolveRelativePath(SysInfo.Config.CdbxpPath);

            Process process = new Process();
            process.StartInfo.FileName = processPath;
            process.StartInfo.Arguments = procArgs;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            while (process.HasExited == false)
            {
                if (cToken.IsCancellationRequested)
                {
                    process.Kill();
                    break;
                }

                string output = process.StandardOutput.ReadLine();

                if (output != null && output.EndsWith("%"))
                {
                    output = output.TrimEnd('%');

                    this.OnProgressChanged((double)Int32.Parse(output) / 100.0);
                }
            }

            if (cToken.IsCancellationRequested)
            {
                // TODO: Cleanup partially generated iso file
            }

            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
    }
}