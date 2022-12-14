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
using FoxHollow.Archiver.Shared.Interfaces;
using FoxHollow.Archiver.Shared.Utilities;
using Newtonsoft.Json;

namespace FoxHollow.Archiver.Shared.Classes.Tape
{
    public class TapeSourceFile : ISourceFile
    {
        private string _FullPath;

        public string Name { get; set; }
        public string Extension { 
            get
            {
                if (this.Name.Contains(".") && this.Name.LastIndexOf('.') != this.Name.Length-1)
                    return this.Name.Substring(this.Name.LastIndexOf('.')+1).ToLower();
                else
                    return String.Empty;
            }
        }
        public string FullPath { 
            get
            {
                return _FullPath;
            } 
            set
            {
                _FullPath = PathUtils.CleanPath(value);

                this.Name = PathUtils.GetFileName(_FullPath);
                this.RelativePath = PathUtils.GetRelativePath(value);
                this.RelativeDirectory = this.RelativePath.Substring(0, this.RelativePath.LastIndexOf('/'));
            }
        }
        public string RelativePath { get; set; }
        public string RelativeDirectory { get; set; }
        public long Size { get; set; } = 0;
        public string Hash { get; set; }
        public bool Copied { get; set; }
        public DateTime ArchiveTimeUtc { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public FileAttributes Attributes { get; set; }
        [JsonIgnore]
        public TapeDetail Tape { get; set; } = null;
        [JsonIgnore]
        public string SourceRootPath
        {
            get
            {
                return this.FullPath.Substring(0, this.FullPath.Length - this.Name.Length - this.RelativeDirectory.Length - 1);
            }
        }

        public TapeSourceFile()
        {
        }

        public TapeSourceFile(string sourcePath)
        {
            this.FullPath = sourcePath;

            if (!File.Exists(this.FullPath))
                throw new DirectoryNotFoundException($"Source file does not exist: {sourcePath}");
        }

        public TapeSourceFile(string sourcePath, TapeDetail tapeDetail) : this(sourcePath)
        {
            this.Tape = tapeDetail;
        }

        public void ReadSizeAndAttribs(TapeDetail tape)
        {
            FileInfo fileInfo = new FileInfo(this.FullPath);

            this.Size = fileInfo.Length;
            this.LastAccessTimeUtc = fileInfo.LastAccessTimeUtc;
            this.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
            this.CreationTimeUtc = fileInfo.CreationTimeUtc;
            this.Attributes = fileInfo.Attributes;
        }

        // public CustomFileCopier ActivateCopy()
        // {
        //     string destinationDir = $"{this.DestinationDisc.RootStagingPath}/data" + this.RelativeDirectory;

        //     CustomFileCopier copier = new CustomFileCopier(this.FullPath, destinationDir);
        //     copier.OverwriteDestination = true;
        //     copier.Preserve = true;

        //     return copier;
        // }
    }
}
