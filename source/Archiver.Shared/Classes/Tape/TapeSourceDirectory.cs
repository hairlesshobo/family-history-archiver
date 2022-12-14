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
using System.Collections.Generic;
using System.IO;
using FoxHollow.Archiver.Shared.Utilities;
using Newtonsoft.Json;

namespace FoxHollow.Archiver.Shared.Classes.Tape
{
    public class TapeSourceDirectory
    {
        private string _FullPath;

        public string Name { get; set; }

        public string FullPath { 
            get
            {
                return _FullPath;
            } 
            set
            {
                _FullPath = PathUtils.CleanPath(value);

                this.Name = PathUtils.GetFileName(_FullPath);
                this.RelativePath = PathUtils.GetRelativePath(value).TrimEnd('/');
            }
        }
        public string RelativePath { get; set; }
        public List<TapeSourceDirectory> Directories { get; set; }
        public List<TapeSourceFile> Files { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        [JsonIgnore]
        public TapeDetail Tape { get; set; } = null;

        public TapeSourceDirectory(string sourcePath)
        {
            this.Files = new List<TapeSourceFile>();
            this.Directories = new List<TapeSourceDirectory>();

            this.FullPath = PathUtils.CleanPath(sourcePath);

            if (!Directory.Exists(this.FullPath))
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");

            // we go ahead and pull the last write time when we add the directory, no
            // need for seperate scanning for this info
            FileInfo dirInfo = new FileInfo(this.FullPath);

            this.LastWriteTimeUtc = dirInfo.LastWriteTimeUtc;
        }

        public TapeSourceDirectory()
        {
            this.Files = new List<TapeSourceFile>();
        }
    }
}