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
using FoxHollow.Archiver.Shared.Classes.Tape;
using FoxHollow.Archiver.Shared.Utilities;
using FoxHollow.FHM.Shared.Models;

namespace FoxHollow.Archiver.Shared.Operations.Tape
{
    public class TapeArchiveSummary
    {
        public delegate void ProgressChangedDelegate(string newLine);

        public event ProgressChangedDelegate OnLineGenerated;

        public IReadOnlyList<string> Lines => (IReadOnlyList<string>)_lines;

        private List<string> _lines;

        public TapeArchiveSummary()
        {
            _lines = new List<string>();

            this.OnLineGenerated += delegate { };
        }

        public void GenerateSummary(List<TapeDetail> allTapes)
        {
            List<TapeSourceFile> allFiles = allTapes.SelectMany(x => x.FlattenFiles()).ToList();

            this.AppendLine("Overall Tape Archive Statistics");
            this.AppendLine("==============================================================");

            if (allTapes.Count() > 0)
            {
                this.AppendLine($"          Total Tapes: {allTapes.Count()}");
                this.AppendLine($"          Total Files: {allFiles.Count().ToString("N0")}");
                this.AppendLine($"      Total Data Size: {Formatting.GetFriendlySize(allTapes.Sum(x => x.DataSizeBytes))}");
                this.AppendLine();
                this.AppendLine();
            }

            foreach (TapeDetail tapeDetail in allTapes)
            {
                this.AppendLine("Tape Overview: " + tapeDetail.Name);
                this.AppendLine("==============================================================");
                this.AppendLine("          Tape ID: " + tapeDetail.ID);
                this.AppendLine("        Tape Name: " + tapeDetail.Name);
                this.AppendLine(" Tape Description: " + tapeDetail.Description);
                this.AppendLine("  Tape Write Date: " + tapeDetail.WriteDTM.ToLocalTime().ToString());
                this.AppendLine();
                this.AppendLine("        Data Size: " + Formatting.GetFriendlySize(tapeDetail.DataSizeBytes));
                this.AppendLine("  Directory Count: " + tapeDetail.FlattenDirectories().Count().ToString("N0"));
                this.AppendLine("       File Count: " + tapeDetail.FlattenFiles().Count().ToString("N0"));
                this.AppendLine();
                this.AppendLine("     Archive Size: " + Formatting.GetFriendlySize(tapeDetail.TotalArchiveBytes));
                this.AppendLine("     Size on Tape: " + Formatting.GetFriendlySize(tapeDetail.ArchiveBytesOnTape));
                this.AppendLine("Compression Ratio: " + tapeDetail.CompressionRatio.ToString("0.00") + ":1");

                if (tapeDetail.SourceInfo.SourcePaths.Length > 0)
                {
                    this.AppendLine();
                    this.AppendLine("     Source Paths: " + tapeDetail.SourceInfo.SourcePaths[0]);

                    for (int i = 1; i < tapeDetail.SourceInfo.SourcePaths.Length; i++)
                        this.AppendLine("                   " + tapeDetail.SourceInfo.SourcePaths[i]);
                }

                if (tapeDetail.SourceInfo.ExcludePaths.Length > 0)
                {
                    this.AppendLine();
                    this.AppendLine("   Excluded Paths: " + tapeDetail.SourceInfo.ExcludePaths[0]);

                    for (int i = 1; i < tapeDetail.SourceInfo.ExcludePaths.Length; i++)
                        this.AppendLine("                   " + tapeDetail.SourceInfo.ExcludePaths[i]);
                }

                if (tapeDetail.SourceInfo.ExcludeFiles.Length > 0)
                {
                    this.AppendLine();
                    this.AppendLine("   Excluded Files: " + tapeDetail.SourceInfo.ExcludeFiles[0]);

                    for (int i = 1; i < tapeDetail.SourceInfo.ExcludeFiles.Length; i++)
                        this.AppendLine("                   " + tapeDetail.SourceInfo.ExcludeFiles[i]);
                }
                
                this.AppendLine("==============================================================");
                this.AppendLine();
                this.AppendLine();
                this.AppendLine();
            }
            
            this.AppendLine("Overall File Type Statistics");
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