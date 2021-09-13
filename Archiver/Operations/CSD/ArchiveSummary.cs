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
using System.Threading.Tasks;
using Archiver.Shared;
using Archiver.Shared.Classes.CSD;
using Archiver.Shared.Utilities;
using Archiver.Utilities.Shared;
using TerminalUI;
using TerminalUI.Elements;

namespace Archiver.Operations.CSD
{
    public static class ArchiveSummary
    {
        public static async Task StartOperationAsync()
        {
            List<CsdDetail> existingCsdDrives = await Helpers.ReadCsdIndexAsync();
            Terminal.Clear();
            Terminal.Header.UpdateLeft("CSD Archive Summary");

            // IEnumerable<CsdSourceFile> allFiles = existingTapes.SelectMany(x => x.FlattenFiles());

            using (Pager pager = new Pager())
            {
                pager.AppendLine("                Overall CSD Archive Statistics");
                pager.AppendLine("==============================================================");

                long driveCount = existingCsdDrives.Count();
                long totalFileCount = existingCsdDrives.Sum(x => x.TotalFiles);
                long totalDriveCapacity = existingCsdDrives.Sum(x => x.TotalSpace);
                long totalDataSize = existingCsdDrives.Sum(x => x.DataSize);
                long totalDataSizeOnDisk = existingCsdDrives.Sum(x => x.DataSizeOnDisc);
                long totalFreeSpace = totalDriveCapacity - totalDataSize - (driveCount * SysInfo.Config.CSD.ReservedCapacityBytes);
                double capacityUsed = Math.Round(((double)totalDataSize / (double)totalDriveCapacity)*100.0, 1);

                if (existingCsdDrives.Count() > 0)
                {
                    pager.AppendLine($"    Registered CSD Drives: {driveCount}");
                    pager.AppendLine();
                    pager.AppendLine($" Total CSD Drive Capacity: {Formatting.GetFriendlySize(totalDriveCapacity)} ({totalDriveCapacity.ToString("N0")} Bytes)");
                    pager.AppendLine($"        Usable Free Space: {Formatting.GetFriendlySize(totalFreeSpace)} ({totalFreeSpace.ToString("N0")} Bytes)");
                    pager.AppendLine($"            Used Capacity: {capacityUsed.ToString()}%");
                    pager.AppendLine();
                    pager.AppendLine($"              Total Files: {totalFileCount.ToString("N0")}");
                    pager.AppendLine($"          Total Data Size: {Formatting.GetFriendlySize(totalDataSize)} ({totalDataSize.ToString("N0")} Bytes)");
                    pager.AppendLine($"  Total Data Size on Disk: {Formatting.GetFriendlySize(totalDataSizeOnDisk)} ({totalDataSizeOnDisk.ToString("N0")} Bytes)");
                    pager.AppendLine();
                    pager.AppendLine();
                    pager.AppendLine($"                      CSD Drive Overview");
                    pager.AppendLine("==============================================================");
                    pager.AppendLine("CSD".PadLeft(6) + "    " + "Free Space".PadLeft(11) + "    " + "Capacity".PadLeft(11) + "    " + "Used %".PadLeft(6) + "    " + "File Count".PadLeft(10));
                    pager.AppendLine("--------------------------------------------------------------");


                    foreach (CsdDetail csd in existingCsdDrives)
                    {
                        long usableFreeSpace = csd.FreeSpace - SysInfo.Config.CSD.ReservedCapacityBytes;
                        
                        if (usableFreeSpace < 0 || usableFreeSpace == csd.BlockSize)
                            usableFreeSpace = 0;

                        double csdPctUsed = Math.Round(((double)csd.DataSizeOnDisc / (double)(csd.TotalSpace-SysInfo.Config.CSD.ReservedCapacityBytes))*100.0, 1);

                        pager.AppendLine(csd.CsdName + "    " + Formatting.GetFriendlySize(usableFreeSpace).PadLeft(11) + "    " + Formatting.GetFriendlySize(csd.TotalSpace).PadLeft(11) + "    " + $"{csdPctUsed.ToString("N1")}%".PadLeft(6) + "    " + csd.TotalFiles.ToString("N0").PadLeft(10));
                    }
                }

                // foreach (CsdDetail csdDetail in existinCsdDrives)
                // {
                //     pager.AppendLine("Tape Overview: " + csdDetail.Name);
                //     pager.AppendLine("==============================================================");
                //     pager.AppendLine("          Tape ID: " + csdDetail.ID);
                //     pager.AppendLine("        Tape Name: " + csdDetail.Name);
                //     pager.AppendLine(" Tape Description: " + csdDetail.Description);
                //     pager.AppendLine("  Tape Write Date: " + csdDetail.WriteDTM.ToLocalTime().ToString());
                //     pager.AppendLine();
                //     pager.AppendLine("        Data Size: " + Formatting.GetFriendlySize(csdDetail.DataSizeBytes));
                //     pager.AppendLine("  Directory Count: " + csdDetail.FlattenDirectories().Count().ToString("N0"));
                //     pager.AppendLine("       File Count: " + csdDetail.FlattenFiles().Count().ToString("N0"));
                //     pager.AppendLine();
                //     pager.AppendLine("     Archive Size: " + Formatting.GetFriendlySize(csdDetail.TotalArchiveBytes));
                //     pager.AppendLine("     Size on Tape: " + Formatting.GetFriendlySize(csdDetail.ArchiveBytesOnTape));
                //     pager.AppendLine("Compression Ratio: " + csdDetail.CompressionRatio.ToString("0.00") + ":1");
                //     pager.AppendLine();
                //     pager.AppendLine();
                // }
                
                // pager.AppendLine("Overall File Type Statistics");
                // pager.AppendLine("==============================================================");
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

                //     pager.AppendLine($"{extension.PadLeft(columnWidth)}: {type.Count.ToString().PadLeft(maxCountWidth+2)}");
                // }

                await pager.RunAsync();
            }

            // allFiles = null;
        }
    }
}