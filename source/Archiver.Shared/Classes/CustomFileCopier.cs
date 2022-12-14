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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxHollow.Archiver.Shared.Interfaces;
using FoxHollow.Archiver.Shared.Structures;
using FoxHollow.Archiver.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Classes
{
    /// <summary>
    ///     Asynchronous file copier that provides progress information and 
    ///     generates a MD5 hash while copying the file
    /// </summary>
    public class CustomFileCopier
    {
        public delegate void ProgressChangedDelegate(FileCopyProgress progress);

        /// <summary>
        ///     Object that describes the source file being copied
        /// </summary>
        public ISourceFile SourceFile { get; set; }


        /// <summary>
        ///     The directory that is considered the root of the destination. Since the source relative path
        ///     is preserved when the file is copied to the destination, this is the root that gets prepended
        ///     to the <see cref="ISourceFile.RelativeDirectory" /> of the <see cref="ISourceFile" /> object
        /// </summary>
        public string DestinationRoot { get; set; }

        /// <summary>
        ///     Full directory path of the destination file
        /// </summary>
        public string DestinationDirectory { get; set; }

        /// <summary>
        ///     Full path to the new file
        /// </summary>
        public string DestinationFilePath { get; set; }

        /// <summary>
        ///     If true, the destination file will be overwritten if it already exists
        /// </summary>
        public bool OverwriteDestination { get; set; } = false;

        /// <summary>
        ///     If true, preserve the file and directory timestamps on the destination
        /// </summary>
        public bool Preserve { get; set; } = true;

        /// <summary>
        ///     How frequently the copier shall emit progress updates
        /// 
        ///     Default: 200
        /// </summary>
        public int SampleDurationMs { get; set; } = 200;

        /// <summary>
        ///     MD5 hash of the copied file. Note: this is only available after the copy has completed
        /// </summary>
        public string MD5_Hash { get; private set; } = null;

        /// <summary>
        ///     Event that is fired when the file copy is finished
        /// </summary>
        public event ProgressChangedDelegate OnComplete;

        /// <summary>
        ///     Event that is fireed periodically during the copy process to provide 
        ///     progress updates
        /// </summary>
        public event ProgressChangedDelegate OnProgressChanged;

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="sourceFile">Source file that is being copied</param>
        /// <param name="destinationRoot">
        ///     The directory that is considered the root of the destination. Since the source relative path
        ///     is preserved when the file is copied to the destination, this is the root that gets prepended
        ///     to the <see cref="ISourceFile.RelativeDirectory" /> of the <see cref="ISourceFile" /> object
        /// </param>
        /// <param name="newFileName">
        ///     If provided, this overwrites the file name name of the destination file
        /// </param>
        public CustomFileCopier(ISourceFile sourceFile, string destinationRoot, string newFileName = null)
        {
            this.SourceFile = sourceFile;
            this.DestinationRoot = PathUtils.CleanPath(destinationRoot);
            this.DestinationDirectory = PathUtils.CleanPath($"{this.DestinationRoot}/{this.SourceFile.RelativeDirectory}");

            if (!File.Exists(this.SourceFile.FullPath))
                throw new FileNotFoundException($"Source file doesn't exist: {this.SourceFile.FullPath}");

            if (!Directory.Exists(this.DestinationDirectory))
                Directory.CreateDirectory(this.DestinationDirectory);

            if (newFileName != null)
                this.DestinationFilePath = Path.Join(this.DestinationDirectory, newFileName);
            else
            {
                FileInfo fi = new FileInfo(this.SourceFile.FullPath);
                this.DestinationFilePath = Path.Join(this.DestinationDirectory, fi.Name);
            }

            this.DestinationFilePath = PathUtils.CleanPath(this.DestinationFilePath);

            this.OnComplete += delegate { };
            this.OnProgressChanged += delegate { };
        }

        /// <summary>
        ///     Asynchronously copy the file to the destination
        /// </summary>
        /// <param name="cToken">Token used to allow for cancellation of the process</param>
        /// <returns>Task</returns>
        public Task CopyAsync(CancellationToken cToken)
            => Task.Run(() => Copy(cToken));

        /// <summary>
        ///     Copy the file to the destination
        /// </summary>
        /// <param name="cToken">Token used to allow for cancellation of the process</param>
        public void Copy(CancellationToken cToken = default)
        {
            FileCopyProgress progress = new FileCopyProgress()
            {
                TotalCopiedBytes = 0,
                BytesCopiedSinceLastupdate = 0,
                TotalBytes = 0,
                Complete = false
            };

            byte[] buffer = new byte[1024 * 1024 * 2]; // 2MB buffer

            bool isAbort = false;
            // bool cancelFlag = false;

            using (FileStream source = new FileStream(this.SourceFile.FullPath, FileMode.Open, FileAccess.Read))
            {
                long fileLength = source.Length;

                progress.TotalBytes = fileLength;

                if (this.OverwriteDestination && File.Exists(this.DestinationFilePath))
                    File.Delete(this.DestinationFilePath);

                // TODO: what is this for?
                File.Create(this.DestinationFilePath).Dispose();

                FileInfo sourceFileInfo = new FileInfo(this.SourceFile.FullPath);

                using (var md5 = MD5.Create())
                using (FileStream dest = new FileStream(this.DestinationFilePath, FileMode.Truncate, FileAccess.Write))
                {
                    int currentBlockSize = 0;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    long lastSample = sw.ElapsedMilliseconds;
                    long lastSampleCopyTotal = 0;
                    int md5Offset = 0;

                    // TODO: refactor this so that the lastBlock functionality runs outside of the loop
                    while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (cToken.IsCancellationRequested)
                        {
                            isAbort = true;
                            break;
                        }

                        bool lastBlock = currentBlockSize < buffer.Length;
                        progress.TotalCopiedBytes += currentBlockSize;

                        dest.Write(buffer, 0, currentBlockSize);
                        md5Offset += md5.TransformBlock(buffer, 0, currentBlockSize, buffer, 0);
                        
                        if (lastBlock || sw.ElapsedMilliseconds - lastSample > this.SampleDurationMs)
                        {
                            progress.BytesCopiedSinceLastupdate = progress.TotalCopiedBytes - lastSampleCopyTotal;
                            lastSampleCopyTotal = progress.TotalCopiedBytes;
                            
                            if (lastBlock)
                            {
                                progress.Complete = true;
                                sw.Stop();
                                md5.TransformFinalBlock(new byte[] { }, 0, 0);

                                this.MD5_Hash = BitConverter.ToString(md5.Hash).Replace("-","").ToLower();
                            }

                            OnProgressChanged(progress);
                            lastSample = sw.ElapsedMilliseconds;
                        }
                    }
                }

                if (this.Preserve && !isAbort)
                {
                    FileInfo destinationFileInfo = new FileInfo(this.DestinationFilePath);

                    destinationFileInfo.LastAccessTimeUtc = sourceFileInfo.LastAccessTimeUtc;
                    destinationFileInfo.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
                    destinationFileInfo.CreationTimeUtc = sourceFileInfo.CreationTimeUtc;

                    PreserveDirectoryTimes();
                }
            }
            
            // here I can optionally clean up the partially copied file 
            // if (isAbort)
            //     File.Delete(this.DestinationFilePath);

            if (!isAbort)
                OnComplete(progress);
        }

        /// <summary>
        ///     Used to preserve the directory times after a file has been written
        /// </summary>
        private void PreserveDirectoryTimes()
        {
            // TODO: likely only need to update the modify dtm on the parent dir when a file is added, that is all
            string[] dirParts = this.SourceFile.RelativeDirectory.Trim('/').Split("/");

            StringBuilder dirRecursion = new StringBuilder();

            foreach (string part in dirParts)
            {
                dirRecursion.Append($"/{part}");

                string sourceDirPart = $"{this.SourceFile.SourceRootPath}{dirRecursion.ToString()}";
                string destDirPart = $"{this.DestinationRoot}{dirRecursion.ToString()}";

                if (Directory.Exists(destDirPart) && Directory.Exists(destDirPart))
                {
                    // copy the relevant bits
                    DirectoryInfo destinationDirInfo = new DirectoryInfo(destDirPart);
                    DirectoryInfo sourceDirInfo = new DirectoryInfo(sourceDirPart);

                    destinationDirInfo.LastAccessTimeUtc = sourceDirInfo.LastAccessTimeUtc;
                    destinationDirInfo.LastWriteTimeUtc = sourceDirInfo.LastWriteTimeUtc;
                    destinationDirInfo.CreationTimeUtc = sourceDirInfo.CreationTimeUtc;
                }
            }
        }
    }
}