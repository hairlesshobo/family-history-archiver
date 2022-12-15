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
using System.IO;
using System.Linq;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Classes;
using FoxHollow.FHM.Shared.Exceptions;
using FoxHollow.FHM.Shared.Interfaces;
using FoxHollow.FHM.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Classes.CSD
{
    public class CsdSourceFile : CsdSourceFilePathDetail, ISourceFile
    {
        private bool _copied = false;

        public CsdSourceFilePathDetail OriginalFile { get; set; } = null;
        public long Size { get; set; } = -1;
        public bool? FileDeleted { get; set; } = null;
        public string Hash { get; set; }
        public bool Copied 
        { 
            get => _copied; 
            set
            {
                bool isBeingMarkedAsCopied = _copied == false && value == true;

                _copied = value;

                if (isBeingMarkedAsCopied && this.DestinationCsd != null)
                    this.DestinationCsd.MarkFileCopied(this);
            }
        }
        public DateTime ArchiveTimeUtc { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public FileAttributes Attributes { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public CsdDetail DestinationCsd { get; set; } = null;

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourceRootPath => this.FullPath.Substring(0, this.FullPath.Length - this.Name.Length - this.RelativeDirectory.Length - 1);

        public CsdSourceFile() { }

        public CsdSourceFile(CsdScanStats stats, string sourcePath, bool scanForRenames = false)
        {
            this.FullPath = sourcePath;

            if (!File.Exists(this.FullPath))
                throw new DirectoryNotFoundException($"Source file does not exist: {sourcePath}");

            // we found a file on the filesystem, but it is already in the archive
            if (stats.SourceFileDict.ContainsKey(this.RelativePath))
                stats.ExistingFileCount += 1;

            // we only add the file to the index if it hasn't yet been archived
            else
            {
                bool isNewFile = true;
                
                // scan for renames, only if we are told to do so
                if (scanForRenames == true)
                {
                    // we scan for renames by checking for files with the same extension and size
                    this.ReadSizeAndAttribs(stats);
                }

                if (isNewFile)
                {
                    stats.NewFileCount += 1;
                    stats.SourceFileDict.Add(this.RelativePath, this);
                }
            }
        }

        public void ReadSizeAndAttribs(CsdScanStats stats)
        {
            // only read attribs if we haven't already done so
            if (this.LastWriteTimeUtc.Equals(default(DateTime)))
            {
                FileInfo fileInfo = new FileInfo(this.FullPath);

                this.Size = fileInfo.Length;
                this.LastAccessTimeUtc = fileInfo.LastAccessTimeUtc;
                this.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                this.CreationTimeUtc = fileInfo.CreationTimeUtc;
                this.Attributes = fileInfo.Attributes;

                stats.TotalSizePending += this.Size;
            }
        }

        public void AssignCsd(CsdScanStats stats)
        {
            if (this.Size >= 0)
            {
                this.DestinationCsd = GetDestinationCsd(stats, this.Size);
                this.DestinationCsd.AddFile(this);
            }
        }

        public CustomFileCopier ActivateCopy(string driveLetter)
        {
            string destinationRoot = $"{driveLetter}/data";

            CustomFileCopier copier = new CustomFileCopier(this, destinationRoot);
            copier.OverwriteDestination = true;
            copier.Preserve = true;

            return copier;
        }

        private static CsdDetail GetDestinationCsd(CsdScanStats stats, long FileSize)
        {
            CsdDetail matchingCsd = stats.DestinationCsds
                                         .FirstOrDefault(x => x.DiskFull == false &&
                                                              x.UsableFreeSpace > HelpersNew.RoundToNextMultiple(FileSize, x.BlockSize));

            if (matchingCsd == null)
                throw new CsdInsufficientCapacityException($"No CSD Drive with sufficient capacity to store a {FileSize} byte ({Formatting.GetFriendlySize(FileSize)}) file");
            else
                return matchingCsd;
        }
    }
}
