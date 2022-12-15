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
using System.Linq;
using FoxHollow.Archiver.Shared.Classes.Disc;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;

namespace FoxHollow.Archiver.Shared.Operations.Disc
{
    public class DiscArchiveSummary
    {
        public delegate void ProgressChangedDelegate(string newLine);

        public event ProgressChangedDelegate OnLineGenerated;

        public IReadOnlyList<string> Lines => (IReadOnlyList<string>)_lines;

        private List<string> _lines;

        public DiscArchiveSummary()
        {
            _lines = new List<string>();

            this.OnLineGenerated += delegate { };
        }

        public void GenerateSummary(List<DiscDetail> allDiscs)
        {
            List<DiscSourceFile> allFiles = allDiscs.SelectMany(x => x.Files).ToList();
            
            this.AppendLine("Current Disc Archive Statistics");
            this.AppendLine("==============================================================");

            IEnumerable<DiscDetail> existingDiscs = allDiscs.Where(x => x.NewDisc == false);
            if (existingDiscs.Count() > 0)
            {

                this.AppendLine($"                Discs: {existingDiscs.Count()}");
                this.AppendLine($"                Files: {existingDiscs.Sum(x => x.Files.Count())}");
                this.AppendLine($"            Data Size: {Formatting.GetFriendlySize(existingDiscs.Sum(x => x.DataSize))}");
                this.AppendLine($"    Last Archive Date: {existingDiscs.Max(x => x.ArchiveDTM).ToShortDateString()}");
                this.AppendLine();
            }
            
            this.AppendLine("File Type Statistics");
            this.AppendLine("==============================================================");
            IEnumerable<FileType> fileCounts = allFiles.GroupBy(x => x.Extension)
                                                        .Select(x => new FileType() { Extension = x.Key, Count = x.Count()})
                                                        .OrderByDescending(x => x.Count);

            int maxCountWidth = fileCounts.Max(x => x.Count.ToString().Length);
            int columnWidth = new int[] { 6, fileCounts.Max(x => x.Extension.Length) }.Max() + 5;

            foreach (FileType type in fileCounts)
            {
                string extension = "<none>";

                if (type.Extension != String.Empty)
                    extension = type.Extension;

                this.AppendLine($"{extension.PadLeft(columnWidth)}: {type.Count.ToString().PadLeft(maxCountWidth+2)}");
            }
        }

        private void AppendLine(string line = null)
        {
            _lines.Add(line);

            this.OnLineGenerated(line);
        }
    }
}