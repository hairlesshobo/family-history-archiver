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
using FoxHollow.Archiver.Shared.Classes.CSD;
using FoxHollow.Archiver.Shared.Models;
using FoxHollow.Archiver.Shared.Utilities;

namespace FoxHollow.Archiver.Shared.Operations.CSD
{
    public class CsdArchiveSummary
    {
        public delegate void ProgressChangedDelegate(string newLine);

        public event ProgressChangedDelegate OnLineGenerated;

        public IReadOnlyList<string> Lines => (IReadOnlyList<string>)_lines;

        private List<string> _lines;

        public CsdArchiveSummary()
        {
            _lines = new List<string>();

            this.OnLineGenerated += delegate { };
        }

        public void GenerateSummary(List<CsdDetail> allCsdDrives)
        {
            // IEnumerable<CsdSourceFile> allFiles = existingTapes.SelectMany(x => x.FlattenFiles());

            this.AppendLine("                Overall CSD Archive Statistics");
            this.AppendLine("==============================================================");

            long driveCount = allCsdDrives.Count();
            long totalFileCount = allCsdDrives.Sum(x => x.TotalFiles);
            long totalDriveCapacity = allCsdDrives.Sum(x => x.TotalSpace);
            long totalDataSize = allCsdDrives.Sum(x => x.DataSize);
            long totalDataSizeOnDisk = allCsdDrives.Sum(x => x.DataSizeOnDisc);
            long totalFreeSpace = totalDriveCapacity - totalDataSize - (driveCount * AppInfo.Config.CSD.ReservedCapacityBytes);
            double capacityUsed = Math.Round(((double)totalDataSize / (double)totalDriveCapacity)*100.0, 1);

            if (allCsdDrives.Count() > 0)
            {
                this.AppendLine($"    Registered CSD Drives: {driveCount}");
                this.AppendLine();
                this.AppendLine($" Total CSD Drive Capacity: {Formatting.GetFriendlySize(totalDriveCapacity)} ({totalDriveCapacity.ToString("N0")} Bytes)");
                this.AppendLine($"        Usable Free Space: {Formatting.GetFriendlySize(totalFreeSpace)} ({totalFreeSpace.ToString("N0")} Bytes)");
                this.AppendLine($"            Used Capacity: {capacityUsed.ToString()}%");
                this.AppendLine();
                this.AppendLine($"              Total Files: {totalFileCount.ToString("N0")}");
                this.AppendLine($"          Total Data Size: {Formatting.GetFriendlySize(totalDataSize)} ({totalDataSize.ToString("N0")} Bytes)");
                this.AppendLine($"  Total Data Size on Disk: {Formatting.GetFriendlySize(totalDataSizeOnDisk)} ({totalDataSizeOnDisk.ToString("N0")} Bytes)");
                this.AppendLine();
                this.AppendLine();
                this.AppendLine($"                      CSD Drive Overview");
                this.AppendLine("==============================================================");
                this.AppendLine("CSD".PadLeft(6) + "    " + "Free Space".PadLeft(11) + "    " + "Capacity".PadLeft(11) + "    " + "Used %".PadLeft(6) + "    " + "File Count".PadLeft(10));
                this.AppendLine("--------------------------------------------------------------");


                foreach (CsdDetail csd in allCsdDrives)
                {
                    long usableFreeSpace = csd.FreeSpace - AppInfo.Config.CSD.ReservedCapacityBytes;
                    
                    if (usableFreeSpace < 0 || usableFreeSpace == csd.BlockSize)
                        usableFreeSpace = 0;

                    double csdPctUsed = Math.Round(((double)csd.DataSizeOnDisc / (double)(csd.TotalSpace-AppInfo.Config.CSD.ReservedCapacityBytes))*100.0, 1);

                    this.AppendLine(csd.CsdName + "    " + Formatting.GetFriendlySize(usableFreeSpace).PadLeft(11) + "    " + Formatting.GetFriendlySize(csd.TotalSpace).PadLeft(11) + "    " + $"{csdPctUsed.ToString("N1")}%".PadLeft(6) + "    " + csd.TotalFiles.ToString("N0").PadLeft(10));
                }
            }

            // foreach (CsdDetail csdDetail in existinCsdDrives)
            // {
            //     this.AppendLine("Tape Overview: " + csdDetail.Name);
            //     this.AppendLine("==============================================================");
            //     this.AppendLine("          Tape ID: " + csdDetail.ID);
            //     this.AppendLine("        Tape Name: " + csdDetail.Name);
            //     this.AppendLine(" Tape Description: " + csdDetail.Description);
            //     this.AppendLine("  Tape Write Date: " + csdDetail.WriteDTM.ToLocalTime().ToString());
            //     this.AppendLine();
            //     this.AppendLine("        Data Size: " + Formatting.GetFriendlySize(csdDetail.DataSizeBytes));
            //     this.AppendLine("  Directory Count: " + csdDetail.FlattenDirectories().Count().ToString("N0"));
            //     this.AppendLine("       File Count: " + csdDetail.FlattenFiles().Count().ToString("N0"));
            //     this.AppendLine();
            //     this.AppendLine("     Archive Size: " + Formatting.GetFriendlySize(csdDetail.TotalArchiveBytes));
            //     this.AppendLine("     Size on Tape: " + Formatting.GetFriendlySize(csdDetail.ArchiveBytesOnTape));
            //     this.AppendLine("Compression Ratio: " + csdDetail.CompressionRatio.ToString("0.00") + ":1");
            //     this.AppendLine();
            //     this.AppendLine();
            // }
            
            // this.AppendLine("Overall File Type Statistics");
            // this.AppendLine("==============================================================");
            // IEnumerable<FileType> fileCounts = allFiles.GroupBy(x => x.Extension)
            //                                         .Select(x => new FileType() { Extension = x.Key, Count = x.Count()})
            //                                         .OrderByDescending(x => x.Count);

            // int maxCountWidth = fileCounts.Max(x => x.Count.ToString().Length);
            // int columnWidth = new int[] { 6, fileCounts.Max(x => x.Extension.Length) }.Max() + 5;

            // foreach (FileType type in fileCounts)
            // {
            //     string extension = "<none>";

            //     if (type.Extension != String.Empty)
            //         extension = type.Extension;

            //     this.AppendLine($"{extension.PadLeft(columnWidth)}: {type.Count.ToString().PadLeft(maxCountWidth+2)}");
            // }
        }

        private void AppendLine(string line = null)
        {
            _lines.Add(line);

            this.OnLineGenerated(line);
        }
    }
}